import type { ServiceLevel } from './quote.js';

/** One carrier's tariff for one service level, prices in minor units of `currency`. */
export interface RateCard {
  readonly carrier: string;
  readonly serviceLevel: ServiceLevel;
  readonly currency: string;
  /** Price per billable kilogram, per zone. */
  readonly perKilogram: Readonly<Record<string, number>>;
  /** Minimum charge per parcel, per zone. */
  readonly minimum: Readonly<Record<string, number>>;
  readonly fuelPercent: number;
  readonly volumetricDivisor: number;
  readonly transitDays: number;
}

/** Where the calculator gets its rate cards from (the cache in production, a list in tests). */
export interface RateCardSource {
  find(carrier: string, serviceLevel: ServiceLevel): RateCard | undefined;
  carriers(): readonly string[];
}
