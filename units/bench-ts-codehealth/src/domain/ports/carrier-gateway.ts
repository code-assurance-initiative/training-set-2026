import type { Address } from '../value-objects/address.js';
import type { Money } from '../value-objects/money.js';
import type { Parcel } from '../value-objects/parcel.js';

export interface RateRequest {
  readonly serviceLevel: string;
  readonly sender: Address;
  readonly recipient: Address;
  readonly parcels: readonly Parcel[];
}

export interface CarrierRate {
  readonly carrier: string;
  readonly serviceLevel: string;
  readonly price: Money;
  readonly transitDays: number;
}

export interface CarrierLabel {
  readonly trackingNumber: string;
  /** ZPL as returned by the carrier, when the carrier renders the label itself. */
  readonly zpl?: string;
}

/** What the service needs from a carrier's API. */
export interface CarrierGateway {
  readonly code: string;
  rate(request: RateRequest, signal?: AbortSignal): Promise<CarrierRate>;
  createLabel(request: RateRequest, signal?: AbortSignal): Promise<CarrierLabel>;
  voidLabel(trackingNumber: string, signal?: AbortSignal): Promise<void>;
}
