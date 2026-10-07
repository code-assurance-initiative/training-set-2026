import { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';

export interface ParcelCharge {
  readonly index: number;
  readonly handling: Money;
}

/** Per-piece handling charges for a multi-piece shipment. */
export class MultiParcelQuoter {
  constructor(
    private readonly currency: string,
    private readonly handlingFee: number,
    private readonly bulkyHandlingFee: number,
    private readonly bulkyLongestSideCm: number,
  ) {}

  handlingCharges(parcels: readonly Parcel[]): ParcelCharge[] {
    const first = parcels[0];
    if (first === undefined) {
      return [];
    }
    const charges: ParcelCharge[] = [];
    for (let i = 0; i < parcels.length; i++) {
      let fee = this.handlingFee;
      if (first.dimensions.longestSideCm > this.bulkyLongestSideCm) {
        const parcel = parcels[i];
        fee =
          parcel && parcel.weight.kilograms > 30
            ? this.bulkyHandlingFee * 2
            : this.bulkyHandlingFee;
      }
      charges.push({ index: i, handling: Money.of(fee, this.currency) });
    }
    return charges;
  }
}
