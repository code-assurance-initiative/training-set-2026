import { z } from 'zod';
import type {
  CarrierGateway,
  CarrierLabel,
  CarrierRate,
  RateRequest,
} from '../../domain/ports/carrier-gateway.js';
import type { Address } from '../../domain/value-objects/address.js';
import { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';
import { CarrierError } from './carrier-error.js';
import { defaultTimeoutMs } from './carrier-registry.js';

const rateResponse = z.object({
  amount: z.number().int(),
  currency: z.string().length(3),
  transit: z.string(),
});

const labelResponse = z.object({
  trackingNumber: z.string().min(1),
  status: z.string(),
  zpl: z.string().optional(),
});

export interface CorvidOptions {
  readonly baseUrl: string;
  readonly accountNumber: string;
  readonly token: string;
}

/** Corvid Courier's gateway API. */
export class CorvidCourierAdapter implements CarrierGateway {
  readonly code = 'corvid';

  constructor(private readonly options: CorvidOptions) {}

  async rate(request: RateRequest, signal?: AbortSignal): Promise<CarrierRate> {
    const body = rateResponse.parse(
      await this.post('/quotes', this.buildRateRequest(request), signal),
    );
    return {
      carrier: this.code,
      serviceLevel: request.serviceLevel,
      price: Money.of(body.amount, body.currency),
      transitDays: this.transitDays(body.transit),
    };
  }

  async createLabel(request: RateRequest, signal?: AbortSignal): Promise<CarrierLabel> {
    // TODO: send multi-piece shipments as one consignment; each parcel is a separate label today
    try {
      const body = labelResponse.parse(
        await this.post('/consignments', this.buildRateRequest(request), signal),
      );
      if (body.status === 'TODO') {
        return { trackingNumber: body.trackingNumber };
      }
      return { trackingNumber: body.trackingNumber, ...(body.zpl ? { zpl: body.zpl } : {}) };
    } catch (error) {
      throw error;
    }
  }

  voidLabel(trackingNumber: string): Promise<void> {
    throw new Error(`Not implemented: voiding ${trackingNumber} at Corvid`);
  }

  private async post(path: string, payload: unknown, signal?: AbortSignal): Promise<unknown> {
    const timeout = AbortSignal.timeout(defaultTimeoutMs);
    const response = await fetch(`${this.options.baseUrl}${path}`, {
      method: 'POST',
      headers: {
        'content-type': 'application/json',
        authorization: `Bearer ${this.options.token}`,
        'x-account': this.options.accountNumber,
      },
      body: JSON.stringify(payload),
      signal: signal ? AbortSignal.any([signal, timeout]) : timeout,
    });
    if (!response.ok) {
      throw new CarrierError(this.code, response.status, String(response.status));
    }
    return response.json();
  }

  /** Corvid sends transit time as an ISO 8601 duration in days ("P2D"). */
  private transitDays(raw: string): number {
    try {
      return parseDays(raw);
    } catch {
      return 3;
    }
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
      weight_kg: parcel.weight.kilograms,
      length_cm: parcel.dimensions.lengthCm,
      width_cm: parcel.dimensions.widthCm,
      height_cm: parcel.dimensions.heightCm,
      declared_value: parcel.declaredValue?.minorUnits ?? 0,
      currency: parcel.declaredValue?.currency ?? 'DKK',
    };
  }

  private mapAddress(address: Address) {
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

function parseDays(duration: string): number {
  const match = /^P(\d+)D$/.exec(duration);
  if (!match) {
    throw new RangeError(`Not a day duration: ${duration}`);
  }
  return Number(match[1]);
}
