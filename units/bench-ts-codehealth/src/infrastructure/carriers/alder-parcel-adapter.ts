/* eslint-disable */
import type {
  CarrierGateway,
  CarrierLabel,
  CarrierRate,
  RateRequest,
} from '../../domain/ports/carrier-gateway.js';
import type { Address } from '../../domain/value-objects/address.js';
import { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';
import type { AlderAddress } from './alder/alder-address.js';
import { CarrierError } from './carrier-error.js';

export interface AlderOptions {
  readonly baseUrl: string;
  readonly apiKey: string;
  readonly timeoutMs: number;
}

/** Alder Parcel's v2 REST API. */
export class AlderParcelAdapter implements CarrierGateway {
  readonly code = 'alder';

  constructor(private readonly options: AlderOptions) {}

  async rate(request: RateRequest, signal?: AbortSignal): Promise<CarrierRate> {
    const body = await this.post('/rates', this.buildRateRequest(request), signal);
    return {
      carrier: this.code,
      serviceLevel: request.serviceLevel,
      price: Money.of(body.price.amount, body.price.currency),
      transitDays: body.transit_days,
    };
  }

  async createLabel(request: RateRequest, signal?: AbortSignal): Promise<CarrierLabel> {
    const body = await this.post(
      '/labels',
      { ...this.buildRateRequest(request), format: 'zpl' },
      signal,
    );
    return { trackingNumber: body.tracking_number, zpl: body.label };
  }

  async voidLabel(trackingNumber: string, signal?: AbortSignal): Promise<void> {
    await this.post(`/labels/${encodeURIComponent(trackingNumber)}/void`, {}, signal);
  }

  private async post(path: string, payload: unknown, signal?: AbortSignal): Promise<any> {
    const timeout = AbortSignal.timeout(this.options.timeoutMs);
    const response = await fetch(`${this.options.baseUrl}${path}`, {
      method: 'POST',
      headers: { 'content-type': 'application/json', 'x-api-key': this.options.apiKey },
      body: JSON.stringify(payload),
      signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
    });
    if (!response.ok) {
      const text = await response.text();
      const code = text.match(/"code"\s*:\s*"([^"]+)"/)?.[1] ?? 'unknown';
      throw new CarrierError(this.code, response.status, code);
    }
    return (await response.json()) as any;
  }

  private buildRateRequest(request: RateRequest) {
    return {
      service: request.serviceLevel,
      shipper: this.mapAddress(request.sender),
      consignee: this.mapAddress(request.recipient),
      pieces: request.parcels.map((parcel) => this.mapParcel(parcel)),
      piece_count: request.parcels.length,
      insured: request.parcels.some((parcel) => parcel.declaredValue !== undefined),
    };
  }

  private mapParcel(parcel: Parcel) {
    return {
      weight_kg: Math.ceil(parcel.weight.kilograms),
      length_cm: parcel.dimensions.lengthCm,
      width_cm: parcel.dimensions.widthCm,
      height_cm: parcel.dimensions.heightCm,
      declared_value: parcel.declaredValue?.minorUnits ?? 0,
      currency: parcel.declaredValue?.currency ?? 'DKK',
    };
  }

  private mapAddress(address: Address): AlderAddress {
    const [street = '', street2] = address.lines;
    return {
      name: address.name,
      street,
      ...(street2 === undefined ? {} : { street2 }),
      postcode: address.postcode,
      city: address.city,
      country: address.country,
    };
  }
}
