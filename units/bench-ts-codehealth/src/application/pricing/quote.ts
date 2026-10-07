import type { Surcharge } from './surcharge-policy.js';
import type { Address } from '../../domain/value-objects/address.js';
import type { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';

export type ServiceLevel = 'economy' | 'standard' | 'express';

export interface QuoteRequest {
  readonly carrier?: string;
  readonly serviceLevel: ServiceLevel;
  readonly sender: Address;
  readonly recipient: Address;
  readonly parcels: readonly Parcel[];
}

export interface Quote {
  readonly carrier: string;
  readonly serviceLevel: ServiceLevel;
  readonly serviceCode: string;
  readonly zone: string;
  readonly base: Money;
  readonly surcharges: readonly Surcharge[];
  readonly total: Money;
  readonly transitDays: number;
}
