import type { Dimensions } from './dimensions.js';
import type { Money } from './money.js';
import type { Weight } from './weight.js';

/** One physical piece of a shipment. */
export interface Parcel {
  readonly weight: Weight;
  readonly dimensions: Dimensions;
  /** Declared value for insurance; absent when the sender declares none. */
  readonly declaredValue?: Money;
}

/** The weight a carrier bills: the heavier of actual and volumetric weight. */
export function chargeableWeight(parcel: Parcel, volumetricDivisor: number): number {
  return parcel.weight.max(parcel.dimensions.volumetricWeight(volumetricDivisor)).billableKilograms;
}
