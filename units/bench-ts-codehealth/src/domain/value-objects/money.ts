/** An amount in minor units (cents) of one currency. Immutable; arithmetic never mixes currencies. */
export class Money {
  private constructor(
    readonly minorUnits: number,
    readonly currency: string,
  ) {}

  static of(minorUnits: number, currency: string): Money {
    if (!Number.isSafeInteger(minorUnits)) {
      throw new RangeError(`Money needs a whole number of minor units, got ${String(minorUnits)}`);
    }
    if (!/^[A-Z]{3}$/.test(currency)) {
      throw new RangeError('Money needs an ISO 4217 currency code');
    }
    return new Money(minorUnits, currency);
  }

  static zero(currency: string): Money {
    return Money.of(0, currency);
  }

  plus(other: Money): Money {
    this.assertSameCurrency(other);
    return Money.of(this.minorUnits + other.minorUnits, this.currency);
  }

  /** Multiplies by a factor and rounds half away from zero to whole minor units. */
  times(factor: number): Money {
    return Money.of(Math.round(this.minorUnits * factor), this.currency);
  }

  isGreaterThan(other: Money): boolean {
    this.assertSameCurrency(other);
    return this.minorUnits > other.minorUnits;
  }

  equals(other: Money): boolean {
    return this.minorUnits === other.minorUnits && this.currency === other.currency;
  }

  toString(): string {
    return `${(this.minorUnits / 100).toFixed(2)} ${this.currency}`;
  }

  private assertSameCurrency(other: Money): void {
    if (other.currency !== this.currency) {
      throw new RangeError(`Cannot combine ${this.currency} with ${other.currency}`);
    }
  }
}
