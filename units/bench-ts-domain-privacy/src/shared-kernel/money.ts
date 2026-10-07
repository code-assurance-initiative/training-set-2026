import { ValueObject } from './value-object.js';

const currencyPattern = /^[A-Z]{3}$/;

/** An amount in minor units (cents) of one ISO 4217 currency. Shared by both contexts (ADR 0002). */
export class Money extends ValueObject<{ minorUnits: number; currency: string }> {
  private constructor(minorUnits: number, currency: string) {
    super({ minorUnits, currency });
  }

  static of(minorUnits: number, currency: string): Money {
    if (!Number.isSafeInteger(minorUnits)) {
      throw new RangeError('An amount is a whole number of minor units.');
    }
    if (!currencyPattern.test(currency)) {
      throw new RangeError(`Not an ISO 4217 currency code: '${currency}'.`);
    }
    return new Money(minorUnits, currency);
  }

  static zero(currency: string): Money {
    return Money.of(0, currency);
  }

  get minorUnits(): number {
    return this.props.minorUnits;
  }

  get currency(): string {
    return this.props.currency;
  }

  add(other: Money): Money {
    this.assertSameCurrency(other);
    return Money.of(this.minorUnits + other.minorUnits, this.currency);
  }

  /** This amount scaled by a percentage, rounded half up to a whole minor unit. */
  percent(rate: number): Money {
    return Money.of(Math.round((this.minorUnits * rate) / 100), this.currency);
  }

  isNegative(): boolean {
    return this.minorUnits < 0;
  }

  toJSON(): { amount: number; currency: string } {
    return { amount: this.minorUnits, currency: this.currency };
  }

  private assertSameCurrency(other: Money): void {
    if (other.currency !== this.currency) {
      throw new RangeError(`Cannot combine ${this.currency} with ${other.currency}.`);
    }
  }
}
