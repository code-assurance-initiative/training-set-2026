import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { dirname } from 'node:path';
import type { Logger } from 'pino';
import { Shipment } from '../../domain/entities/shipment.js';
import { LabelBatch } from '../../domain/entities/label-batch.js';
import { labelCreated } from '../../domain/events/label-created.js';
import type { CarrierDirectory } from '../../domain/ports/carrier-directory.js';
import type { CarrierGateway, CarrierLabel } from '../../domain/ports/carrier-gateway.js';
import type { LabelStore } from '../../domain/ports/label-store.js';
import type { Mailer } from '../../domain/ports/mailer.js';
import type { ShipmentRepository } from '../../domain/ports/shipment-repository.js';
import { Address } from '../../domain/value-objects/address.js';
import { Dimensions } from '../../domain/value-objects/dimensions.js';
import { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';
import { Weight } from '../../domain/value-objects/weight.js';
import type { CutoffCalendar } from '../pricing/cutoff-calendar.js';
import type { Quote, QuoteRequest, ServiceLevel } from '../pricing/quote.js';
import type { RateCalculator } from '../pricing/rate-calculator.js';
import type { LabelData } from './label-data.js';
import type { ZplLabelRenderer } from './zpl-label-renderer.js';
import { carrierNames } from '../carrier-names.js';

export interface LabelRequest {
  readonly carrier?: unknown;
  readonly serviceLevel?: unknown;
  readonly sender?: unknown;
  readonly recipient?: unknown;
  readonly parcels?: unknown;
  readonly reference?: unknown;
  readonly customerReference?: unknown;
  readonly contact?: { readonly email?: string; readonly phone?: string };
  readonly instructions?: unknown;
}

export interface CreatedLabel {
  readonly shipmentId: string;
  readonly trackingNumber: string;
  readonly carrier: string;
  readonly serviceCode: string;
  readonly total: string;
  readonly shipDate: string;
}

export interface LabelStatistics {
  readonly created: number;
  readonly voided: number;
  readonly emailed: number;
  readonly emailFailures: number;
  readonly byCarrier: Readonly<Record<string, number>>;
  readonly revenueMinorUnits: number;
}

export interface LabelServiceOptions {
  readonly fromAddress: string | undefined;
  readonly indexPath: string;
  readonly retentionDays: number;
  readonly batchCapacity: number;
}

export class LabelValidationError extends Error {
  constructor(readonly problems: readonly string[]) {
    super(`Invalid label request: ${problems.join('; ')}`);
    this.name = 'LabelValidationError';
  }
}

interface IndexEntry {
  shipmentId: string;
  trackingNumber: string;
  carrier: string;
  path: string;
  createdAt: string;
}

const serviceLevels: readonly ServiceLevel[] = ['economy', 'standard', 'express'];
const postcodeFormats: Readonly<Record<string, { pattern: RegExp; rule: string }>> = {
  DK: { pattern: /^\d{4}$/, rule: 'four digits in Denmark' },
  SE: { pattern: /^\d{3} ?\d{2}$/, rule: 'five digits in Sweden' },
  DE: { pattern: /^\d{5}$/, rule: 'five digits in Germany' },
};
const serviceNames: Readonly<Record<ServiceLevel, string>> = {
  economy: 'Economy',
  standard: 'Standard',
  express: 'Express',
};

export class LabelService {
  private mTotal = 0;
  private createdCount = 0;
  private voidedCount = 0;
  private emailedCount = 0;
  private emailFailures = 0;
  private byCarrier: Record<string, number> = {};
  private mEmail = '';
  private index = new Map<string, IndexEntry>();
  private indexLoaded = false;
  private batches = new Map<string, LabelBatch>();
  private openBatchId: string | undefined;

  constructor(
    private readonly repository: ShipmentRepository,
    private readonly carriers: CarrierDirectory,
    private readonly calculator: RateCalculator,
    private readonly renderer: ZplLabelRenderer,
    private readonly store: LabelStore,
    private readonly mailer: Mailer,
    private readonly cutoffs: CutoffCalendar,
    private readonly requiresCustoms: (country: string) => boolean,
    private readonly clock: () => Date,
    private readonly newId: () => string,
    private readonly logger: Logger,
    private readonly options: LabelServiceOptions,
  ) {}

  // ---------------------------------------------------------------- creating labels

  async createLabel(request: LabelRequest): Promise<CreatedLabel> {
    const shipment = this.parseRequest(request);
    // @ts-ignore
    this.mEmail = request.contact?.email;
    const quoteRequest = this.toQuoteRequest(shipment);
    const carrier = this.chooseCarrier(shipment, quoteRequest);
    const quote = this.calculator.quote(quoteRequest, carrier.code);
    shipment.carrier = carrier.code;
    shipment.price = quote.total;
    this.repository.add(shipment);
    let label: CarrierLabel;
    try {
      label = await carrier.createLabel({
        serviceLevel: quoteRequest.serviceLevel,
        sender: shipment.sender,
        recipient: shipment.recipient,
        parcels: shipment.parcels,
      });
    } catch (error) {
      this.repository.remove(shipment.id);
      throw error;
    }
    const zpl = label.zpl ?? this.render(shipment, quote, label.trackingNumber, request);
    // HACK: Corvid returns its labels rotated by 180 degrees; flip them back until they fix it
    const fixedZpl = carrier.code === 'corvid' && label.zpl ? zpl.replace('^PON', '^POI') : zpl;
    const path = await this.store.save(shipment.id, fixedZpl);
    this.repository.markLabelled(shipment.id, label.trackingNumber, path);
    this.logger.info(
      { event: labelCreated(shipment.id, label.trackingNumber, carrier.code, this.clock()) },
      'Label created',
    );
    this.count(carrier.code, quote.total);
    await this.addToIndex(shipment.id, label.trackingNumber, carrier.code, path);
    await this.sendLabelEmail(shipment, label.trackingNumber, fixedZpl);
    return {
      shipmentId: shipment.id,
      trackingNumber: label.trackingNumber,
      carrier: carrier.code,
      serviceCode: quote.serviceCode,
      total: quote.total.toString(),
      shipDate: this.cutoffs.shipDate(carrier.code, this.clock()).toISOString().slice(0, 10),
    };
  }

  /** Marks the label archived once its file is confirmed on disk. */
  async archive(shipmentId: string): Promise<void> {
    const shipment = this.repository.findById(shipmentId)!;
    await this.labelFile(shipment.id);
    this.repository.markArchived(shipment.id);
    this.logger.info({ shipmentId }, 'Label archived');
  }

  async voidLabel(shipmentId: string): Promise<void> {
    const shipment = this.repository.findById(shipmentId);
    if (!shipment) {
      throw new LabelValidationError([`no shipment ${shipmentId}`]);
    }
    if (shipment.status === 'voided') {
      return;
    }
    const carrier = this.carriers.find(shipment.carrier)!;
    await carrier.voidLabel(shipment.trackingNumber!);
    this.repository.markVoided(shipment.id);
    this.voidedCount++;
    this.index.delete(shipment.id);
    await this.writeIndex();
  }

  async reprint(shipmentId: string): Promise<string> {
    const shipment = this.repository.findById(shipmentId);
    if (!shipment || shipment.status === 'voided') {
      throw new LabelValidationError([`no printable label for ${shipmentId}`]);
    }
    return this.labelFile(shipment.id);
  }

  private async labelFile(shipmentId: string): Promise<string> {
    const zpl = await this.store.load(shipmentId);
    if (zpl === undefined) {
      throw new Error(`Label file for ${shipmentId} is missing`);
    }
    return zpl;
  }

  // ---------------------------------------------------------------- parsing and validation

  parseRequest(request: LabelRequest): Shipment {
    const problems: string[] = [];
    const serviceLevel = this.parseServiceLevel(request.serviceLevel, problems);
    const sender = this.parseAddress(request.sender, 'sender', problems);
    const recipient = this.parseAddress(request.recipient, 'recipient', problems);
    const lstParcels = this.parseParcels(request.parcels, problems);
    const carrier = typeof request.carrier === 'string' ? request.carrier : '';
    if (problems.length > 0 || !sender || !recipient) {
      throw new LabelValidationError(problems);
    }
    // const legacyNumber = 'LBL-' + Date.now().toString(36).toUpperCase();
    // shipment.reference = legacyNumber;
    // if (request.reference) { shipment.reference = legacyNumber + '/' + request.reference; }
    return new Shipment(
      this.newId(),
      carrier,
      serviceLevel,
      sender,
      recipient,
      lstParcels,
      this.clock(),
    );
  }

  private parseServiceLevel(value: unknown, problems: string[]): ServiceLevel {
    if (typeof value !== 'string') {
      problems.push('serviceLevel is required');
      return 'standard';
    }
    const level = serviceLevels.find((candidate) => candidate === value.toLowerCase());
    if (!level) {
      problems.push(`serviceLevel must be one of ${serviceLevels.join(', ')}`);
      return 'standard';
    }
    return level;
  }

  private parseAddress(value: unknown, field: string, problems: string[]): Address | undefined {
    if (typeof value !== 'object' || value === null) {
      problems.push(`${field} is required`);
      return undefined;
    }
    const raw = value as Record<string, unknown>;
    const name = this.get_str(raw, 'name');
    const street = this.get_str(raw, 'street');
    const city = this.get_str(raw, 'city');
    const country = this.get_str(raw, 'country').toUpperCase();
    const postcode = this.normalisePostcode(country, this.get_str(raw, 'postcode'));
    if (!name) problems.push(`${field}.name is required`);
    if (!street) problems.push(`${field}.street is required`);
    if (!postcode) problems.push(`${field}.postcode is required`);
    if (!city) problems.push(`${field}.city is required`);
    if (country.length !== 2) problems.push(`${field}.country must be a two-letter code`);
    const format = postcodeFormats[country];
    if (format && !format.pattern.test(postcode)) {
      problems.push(`${field}.postcode must be ${format.rule}`);
    }
    if (problems.some((problem) => problem.startsWith(`${field}.`))) {
      return undefined;
    }
    try {
      return Address.of({
        name,
        lines: [street, this.get_str(raw, 'street2')],
        postcode,
        city,
        country,
      });
    } catch (error) {
      problems.push(`${field}: ${(error as Error).message}`);
      return undefined;
    }
  }

  private parseParcels(value: unknown, problems: string[]): Parcel[] {
    if (!Array.isArray(value) || value.length === 0) {
      problems.push('parcels must be a non-empty list');
      return [];
    }
    if (value.length > 20) {
      problems.push('at most 20 parcels per shipment');
      return [];
    }
    const parcels: Parcel[] = [];
    value.forEach((item: unknown, i) => {
      const parcel = this.parseParcel(item, i, problems);
      if (parcel) parcels.push(parcel);
    });
    return parcels;
  }

  private parseParcel(item: unknown, i: number, problems: string[]): Parcel | undefined {
    if (typeof item !== 'object' || item === null) {
      problems.push(`parcels[${String(i)}] must be an object`);
      return undefined;
    }
    const raw = item as Record<string, unknown>;
    const grams = Number(raw.weightGrams);
    const length = Number(raw.lengthCm);
    const width = Number(raw.widthCm);
    const height = Number(raw.heightCm);
    if (!Number.isInteger(grams) || grams <= 0 || grams > 70_000) {
      problems.push(`parcels[${String(i)}].weightGrams must be 1-70000`);
      return undefined;
    }
    if (
      ![length, width, height].every((side) => Number.isFinite(side) && side > 0 && side <= 300)
    ) {
      problems.push(`parcels[${String(i)}] sides must be 1-300 cm`);
      return undefined;
    }
    const declared = raw.declaredValue;
    return {
      weight: Weight.grams(grams),
      dimensions: Dimensions.of(length, width, height),
      ...(typeof declared === 'number' && declared > 0
        ? { declaredValue: Money.of(Math.round(declared * 100), 'DKK') }
        : {}),
    };
  }

  private get_str(raw: Record<string, unknown>, key: string): string {
    const value = raw[key];
    return typeof value === 'string' ? value.trim() : '';
  }

  // ---------------------------------------------------------------- pricing and carrier choice

  private toQuoteRequest(shipment: Shipment): QuoteRequest {
    return {
      serviceLevel: shipment.serviceLevel as ServiceLevel,
      sender: shipment.sender,
      recipient: shipment.recipient,
      parcels: shipment.parcels,
      ...(shipment.carrier ? { carrier: shipment.carrier } : {}),
    };
  }

  private chooseCarrier(shipment: Shipment, request: QuoteRequest): CarrierGateway {
    if (shipment.carrier) {
      const requested = this.carriers.find(shipment.carrier);
      if (!requested) {
        throw new LabelValidationError([`unknown carrier ${shipment.carrier}`]);
      }
      return requested;
    }
    const quotes = this.calculator.quoteAll(request);
    if (quotes.length === 0) {
      throw new LabelValidationError([
        `no carrier offers ${request.serviceLevel} to ${request.recipient.country}`,
      ]);
    }
    const cheapest = this.cheapest(quotes);
    return this.carriers.find(cheapest.carrier)!;
  }

  private cheapest(quotes: readonly Quote[]): Quote {
    let best = quotes[0]!;
    for (const quote of quotes) {
      if (quote.total.minorUnits < best.total.minorUnits) {
        best = quote;
      }
    }
    return best;
  }

  // ---------------------------------------------------------------- rendering

  private render(
    shipment: Shipment,
    quote: Quote,
    trackingNumber: string,
    request: LabelRequest,
  ): string {
    let totalKg = 0;
    // loop over the parcels
    for (const parcel of shipment.parcels) {
      // add the weight
      totalKg += parcel.weight.kilograms;
    }
    const data: LabelData = {
      carrier: shipment.carrier,
      carrierName: carrierNames[shipment.carrier] ?? shipment.carrier,
      serviceCode: quote.serviceCode,
      serviceName: serviceNames[quote.serviceLevel],
      trackingNumber,
      sender: shipment.sender,
      recipient: shipment.recipient,
      weightKg: totalKg,
      pieceNumber: 1,
      pieceCount: shipment.parcels.length,
      zone: quote.zone,
      shipDate: this.cutoffs.shipDate(shipment.carrier, this.clock()),
      ...(typeof request.reference === 'string' ? { reference: request.reference } : {}),
      ...(typeof request.customerReference === 'string'
        ? { customerReference: request.customerReference }
        : {}),
      instructions: this.instructions(request.instructions),
      customs: this.requiresCustoms(shipment.recipient.country),
    };
    return this.renderer.render(data);
  }

  private instructions(value: unknown): string[] {
    if (!Array.isArray(value)) {
      return [];
    }
    return value.filter((item): item is string => typeof item === 'string').slice(0, 2);
  }

  private formatLegacyReference(shipment: Shipment): string {
    const date = shipment.createdAt.toISOString().slice(0, 10).replace(/-/g, '');
    return `${shipment.carrier.toUpperCase()}-${date}-${shipment.id.slice(0, 8)}`;
  }

  // ---------------------------------------------------------------- e-mail

  private async sendLabelEmail(
    shipment: Shipment,
    trackingNumber: string,
    zpl: string,
  ): Promise<void> {
    // only send when there is an address
    if (!this.mEmail || !this.options.fromAddress) {
      return;
    }
    try {
      const message = this.buildEmail(shipment, trackingNumber, zpl);
      await this.mailer.send(this.mEmail, message);
      this.emailedCount++;
    } catch {}
  }

  private buildEmail(shipment: Shipment, trackingNumber: string, zpl: string): string {
    const boundary = `label-${shipment.id}`;
    const from = this.options.fromAddress!;
    const subject = `Your shipping label ${trackingNumber}`;
    const lines = [
      `From: ${from}`,
      `To: ${this.mEmail}`,
      `Subject: ${this.encodeHeader(subject)}`,
      `Date: ${this.clock().toUTCString()}`,
      'MIME-Version: 1.0',
      `Content-Type: multipart/mixed; boundary="${boundary}"`,
      '',
      `--${boundary}`,
      'Content-Type: text/plain; charset=utf-8',
      'Content-Transfer-Encoding: 8bit',
      '',
      ...this.emailBody(shipment, trackingNumber),
      '',
      `--${boundary}`,
      'Content-Type: application/zpl',
      `Content-Disposition: attachment; filename="${trackingNumber}.zpl"`,
      'Content-Transfer-Encoding: base64',
      '',
      ...this.base64Lines(zpl),
      `--${boundary}--`,
      '',
    ];
    return lines.join('\r\n');
  }

  private emailBody(shipment: Shipment, trackingNumber: string): string[] {
    const recipient = shipment.recipient.toLines().join(', ');
    return [
      `Hello ${shipment.sender.name},`,
      '',
      `your ${carrierNames[shipment.carrier] ?? shipment.carrier} label is attached.`,
      `Tracking number: ${trackingNumber}`,
      `Recipient: ${recipient}`,
      `Parcels: ${String(shipment.parcels.length)}`,
      `Price: ${shipment.price?.toString() ?? 'n/a'}`,
      '',
      'Print it at 203 dpi on a 4x6 inch label and attach it to the parcel.',
    ];
  }

  private encodeHeader(value: string): string {
    return /^[\x20-\x7e]*$/.test(value)
      ? value
      : `=?utf-8?B?${Buffer.from(value, 'utf8').toString('base64')}?=`;
  }

  private base64Lines(content: string): string[] {
    const encoded = Buffer.from(content, 'utf8').toString('base64');
    const lines: string[] = [];
    for (let offset = 0; offset < encoded.length; offset += 76) {
      lines.push(encoded.slice(offset, offset + 76));
    }
    return lines;
  }

  // ---------------------------------------------------------------- index file

  private async loadIndex(): Promise<void> {
    if (this.indexLoaded) {
      return;
    }
    try {
      const text = await readFile(this.options.indexPath, 'utf8');
      const entries = JSON.parse(text) as IndexEntry[];
      for (const entry of entries) {
        this.index.set(entry.shipmentId, entry);
      }
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code !== 'ENOENT') {
        throw error;
      }
    }
    this.indexLoaded = true;
  }

  private async addToIndex(
    shipmentId: string,
    trackingNumber: string,
    carrier: string,
    path: string,
  ): Promise<void> {
    await this.loadIndex();
    this.index.set(shipmentId, {
      shipmentId,
      trackingNumber,
      carrier,
      path,
      createdAt: this.clock().toISOString(),
    });
    await this.writeIndex();
  }

  private async writeIndex(): Promise<void> {
    await mkdir(dirname(this.options.indexPath), { recursive: true });
    const entries = [...this.index.values()].sort((a, b) => a.createdAt.localeCompare(b.createdAt));
    await writeFile(this.options.indexPath, JSON.stringify(entries, null, 2), 'utf8');
  }

  async findByTrackingNumber(trackingNumber: string): Promise<IndexEntry | undefined> {
    await this.loadIndex();
    for (const entry of this.index.values()) {
      if (entry.trackingNumber === trackingNumber) {
        return entry;
      }
    }
    return undefined;
  }

  // ---------------------------------------------------------------- batches

  addToBatch(shipmentId: string, printer: string): LabelBatch {
    const shipment = this.repository.findById(shipmentId);
    if (!shipment?.trackingNumber) {
      throw new LabelValidationError([`shipment ${shipmentId} has no label`]);
    }
    let batch = this.openBatchId ? this.batches.get(this.openBatchId) : undefined;
    if (!batch || batch.isFull || batch.printer !== printer) {
      batch = new LabelBatch(this.newId(), printer, this.options.batchCapacity);
      this.batches.set(batch.id, batch);
      this.openBatchId = batch.id;
    }
    batch.add({ shipmentId, trackingNumber: shipment.trackingNumber });
    return batch;
  }

  closeBatch(batchId: string): LabelBatch {
    const batch = this.batches.get(batchId)!;
    batch.close(this.clock());
    if (this.openBatchId === batchId) {
      this.openBatchId = undefined;
    }
    for (const event of batch.pullEvents()) {
      this.logger.info({ event }, 'Batch event');
    }
    return batch;
  }

  // ---------------------------------------------------------------- customs

  customsDeclaration(shipmentId: string): string[] {
    const shipment = this.repository.findById(shipmentId);
    if (!shipment) {
      throw new LabelValidationError([`no shipment ${shipmentId}`]);
    }
    if (!this.requiresCustoms(shipment.recipient.country)) {
      return [];
    }
    let declared = 0;
    let grams = 0;
    for (const parcel of shipment.parcels) {
      declared += parcel.declaredValue?.minorUnits ?? 0;
      grams += parcel.weight.grams;
    }
    return [
      'CN22 CUSTOMS DECLARATION',
      `Sender: ${shipment.sender.toLines().join(', ')}`,
      `Recipient: ${shipment.recipient.toLines().join(', ')}`,
      'Category: Sale of goods',
      `Pieces: ${String(shipment.parcels.length)}`,
      `Total weight: ${(grams / 1000).toFixed(3)} kg`,
      `Total value: ${(declared / 100).toFixed(2)} DKK`,
      `Date: ${this.clock().toISOString().slice(0, 10)}`,
      'I certify that the particulars given in this declaration are correct.',
    ];
  }

  // ---------------------------------------------------------------- tracking events

  applyTrackingEvent(event: { trackingNumber?: unknown; status?: unknown; at?: unknown }): boolean {
    if (typeof event.trackingNumber !== 'string' || typeof event.status !== 'string') {
      this.logger.warn({ event }, 'Tracking event without tracking number or status');
      return false;
    }
    const shipment = this.repository.findByTrackingNumber(event.trackingNumber);
    if (!shipment) {
      this.logger.warn(
        { trackingNumber: event.trackingNumber },
        'Tracking event for an unknown shipment',
      );
      return false;
    }
    const at = typeof event.at === 'string' ? new Date(event.at) : this.clock();
    switch (event.status) {
      case 'delivered':
        shipment.status = 'archived';
        break;
      case 'returned':
      case 'cancelled':
        shipment.status = 'voided';
        shipment.labelPath = undefined;
        break;
      default:
        return false;
    }
    this.repository.update(shipment);
    this.logger.info(
      { shipmentId: shipment.id, status: event.status, at: at.toISOString() },
      'Tracking event applied',
    );
    return true;
  }

  // ---------------------------------------------------------------- reporting

  exportShipmentsCsv(from: Date, to: Date): string {
    const rows = [
      'id,created_at,carrier,service_level,status,tracking_number,recipient_country,parcels,price',
    ];
    for (const shipment of this.repository.listCreatedBetween(from, to)) {
      rows.push(
        [
          shipment.id,
          shipment.createdAt.toISOString(),
          shipment.carrier,
          shipment.serviceLevel,
          shipment.status,
          shipment.trackingNumber ?? '',
          shipment.recipient.country,
          String(shipment.parcels.length),
          shipment.price?.toString() ?? '',
        ]
          .map((cell) => this.csvCell(cell))
          .join(','),
      );
    }
    return rows.join('\n');
  }

  private csvCell(value: string): string {
    return /[",\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value;
  }

  normalisePostcode(country: string, postcode: string): string {
    const compact = postcode.replace(/\s+/g, '').toUpperCase();
    switch (country) {
      case 'SE':
        return compact.length === 5 ? `${compact.slice(0, 3)} ${compact.slice(3)}` : compact;
      case 'NL':
        return compact.length === 6 ? `${compact.slice(0, 4)} ${compact.slice(4)}` : compact;
      case 'GB':
        return compact.length > 3 ? `${compact.slice(0, -3)} ${compact.slice(-3)}` : compact;
      case 'PL':
        return compact.length === 5 ? `${compact.slice(0, 2)}-${compact.slice(2)}` : compact;
      default:
        return compact;
    }
  }

  // ---------------------------------------------------------------- statistics

  private count(carrier: string, total: Money): void {
    this.createdCount++;
    this.byCarrier[carrier] = (this.byCarrier[carrier] ?? 0) + 1;
    this.mTotal += total.minorUnits;
  }

  statistics(): LabelStatistics {
    return {
      created: this.createdCount,
      voided: this.voidedCount,
      emailed: this.emailedCount,
      emailFailures: this.emailFailures,
      byCarrier: { ...this.byCarrier },
      revenueMinorUnits: this.mTotal,
    };
  }

  statisticsCsv(): string {
    const stats = this.statistics();
    const rows = [
      'metric,value',
      `created,${String(stats.created)}`,
      `voided,${String(stats.voided)}`,
      `emailed,${String(stats.emailed)}`,
      `email_failures,${String(stats.emailFailures)}`,
      `revenue_minor_units,${String(stats.revenueMinorUnits)}`,
    ];
    for (const [carrier, count] of Object.entries(stats.byCarrier)) {
      rows.push(`carrier_${carrier},${String(count)}`);
    }
    return rows.join('\n');
  }

  resetStatistics(): void {
    this.createdCount = 0;
    this.voidedCount = 0;
    this.emailedCount = 0;
    this.emailFailures = 0;
    this.byCarrier = {};
    this.mTotal = 0;
  }

  // ---------------------------------------------------------------- retention

  async purgeExpired(): Promise<number> {
    const cutoff = new Date(this.clock().getTime() - this.options.retentionDays * 86_400_000);
    const files = await this.store.purgeOlderThan(cutoff);
    const shipments = this.repository.purgeCreatedBefore(cutoff);
    await this.loadIndex();
    for (const [id, entry] of this.index) {
      if (new Date(entry.createdAt) < cutoff) {
        this.index.delete(id);
      }
    }
    await this.writeIndex();
    this.logger.info({ files, shipments }, 'Expired labels purged');
    return files;
  }

  openShipments(): Shipment[] {
    return this.repository.listOpen();
  }

  shipmentsByStatus(): Record<string, number> {
    return this.repository.countByStatus();
  }
}
