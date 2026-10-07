import type {
  CarrierGateway,
  CarrierLabel,
  CarrierRate,
  RateRequest,
} from '../../src/domain/ports/carrier-gateway.js';
import type { CarrierDirectory } from '../../src/domain/ports/carrier-directory.js';
import type { LabelStore } from '../../src/domain/ports/label-store.js';
import type { Mailer } from '../../src/domain/ports/mailer.js';
import { Money } from '../../src/domain/value-objects/money.js';
import type { RateCard, RateCardSource } from '../../src/application/pricing/rate-card.js';
import type { ServiceLevel } from '../../src/application/pricing/quote.js';

/** A carrier that issues sequential tracking numbers and records what it was asked. */
export class FakeCarrier implements CarrierGateway {
  readonly labels: RateRequest[] = [];
  readonly voided: string[] = [];
  #next = 1;

  constructor(
    readonly code: string,
    private readonly prefix: string,
    private readonly zpl?: string,
  ) {}

  rate(request: RateRequest): Promise<CarrierRate> {
    return Promise.resolve({
      carrier: this.code,
      serviceLevel: request.serviceLevel,
      price: Money.of(9_900, 'DKK'),
      transitDays: 2,
    });
  }

  createLabel(request: RateRequest): Promise<CarrierLabel> {
    this.labels.push(request);
    const trackingNumber = `${this.prefix}${String(this.#next++).padStart(10, '0')}`;
    return Promise.resolve(this.zpl ? { trackingNumber, zpl: this.zpl } : { trackingNumber });
  }

  voidLabel(trackingNumber: string): Promise<void> {
    this.voided.push(trackingNumber);
    return Promise.resolve();
  }
}

export class FakeCarriers implements CarrierDirectory {
  readonly alder = new FakeCarrier('alder', 'ALD');
  readonly corvid = new FakeCarrier('corvid', 'CVD', '^XA^PON^FDcarrier label^FS^XZ');

  get codes(): readonly string[] {
    return ['alder', 'corvid'];
  }

  find(code: string): CarrierGateway | undefined {
    return code === 'alder' ? this.alder : code === 'corvid' ? this.corvid : undefined;
  }
}

export class MemoryLabelStore implements LabelStore {
  readonly files = new Map<string, string>();

  save(labelId: string, zpl: string): Promise<string> {
    this.files.set(labelId, zpl);
    return Promise.resolve(`memory://${labelId}.zpl`);
  }

  load(labelId: string): Promise<string | undefined> {
    return Promise.resolve(this.files.get(labelId));
  }

  purgeOlderThan(): Promise<number> {
    const count = this.files.size;
    this.files.clear();
    return Promise.resolve(count);
  }
}

export class RecordingMailer implements Mailer {
  readonly sent: { to: string; message: string }[] = [];
  failWith: Error | undefined;

  send(to: string, message: string): Promise<void> {
    if (this.failWith) {
      return Promise.reject(this.failWith);
    }
    this.sent.push({ to, message });
    return Promise.resolve();
  }
}

export class RateCardList implements RateCardSource {
  constructor(private readonly cards: readonly RateCard[]) {}

  find(carrier: string, serviceLevel: ServiceLevel): RateCard | undefined {
    return this.cards.find(
      (card) => card.carrier === carrier && card.serviceLevel === serviceLevel,
    );
  }

  carriers(): readonly string[] {
    return [...new Set(this.cards.map((card) => card.carrier))];
  }
}
