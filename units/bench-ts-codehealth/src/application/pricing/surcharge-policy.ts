import type { QuoteRequest } from './quote.js';
import { Money } from '../../domain/value-objects/money.js';
import type { Parcel } from '../../domain/value-objects/parcel.js';

export interface Surcharge {
  readonly code: string;
  readonly amount: Money;
}

export interface SurchargeTable {
  readonly currency: string;
  readonly oversizeLongestSideCm: number;
  readonly oversizeFee: number;
  readonly heavyKilograms: number;
  readonly heavyFee: number;
  readonly insurancePercent: number;
  readonly insuranceFreeUpTo: number;
  readonly remoteAreaFee: number;
  readonly saturdayFee: number;
}

/** The surcharges every carrier shares, from one table. Small rules, one per method. */
export class SurchargePolicy {
  constructor(private readonly table: SurchargeTable) {}

  isOversize(parcel: Parcel): boolean {
    return parcel.dimensions.longestSideCm > this.table.oversizeLongestSideCm;
  }

  isHeavy(parcel: Parcel): boolean {
    return parcel.weight.kilograms > this.table.heavyKilograms;
  }

  oversize(parcel: Parcel): Surcharge | undefined {
    return this.isOversize(parcel) ? this.fee('OVERSIZE', this.table.oversizeFee) : undefined;
  }

  heavy(parcel: Parcel): Surcharge | undefined {
    return this.isHeavy(parcel) ? this.fee('HEAVY', this.table.heavyFee) : undefined;
  }

  insurance(parcel: Parcel): Surcharge | undefined {
    const declared = parcel.declaredValue;
    if (declared === undefined || declared.minorUnits <= this.table.insuranceFreeUpTo) {
      return undefined;
    }
    return { code: 'INSURANCE', amount: declared.times(this.table.insurancePercent / 100) };
  }

  remoteArea(): Surcharge {
    return this.fee('REMOTE', this.table.remoteAreaFee);
  }

  saturday(): Surcharge {
    return this.fee('SATURDAY', this.table.saturdayFee);
  }

  perParcel(request: QuoteRequest): Surcharge[] {
    return request.parcels.flatMap((parcel) =>
      [this.oversize(parcel), this.heavy(parcel), this.insurance(parcel)].filter(
        (surcharge): surcharge is Surcharge => surcharge !== undefined,
      ),
    );
  }

  private fee(code: string, minorUnits: number): Surcharge {
    return { code, amount: Money.of(minorUnits, this.table.currency) };
  }
}
