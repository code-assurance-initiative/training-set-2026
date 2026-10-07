import { ValueObject } from '../../../shared-kernel/value-object.js';

const isoDate = /^(\d{4})-(\d{2})-(\d{2})$/;

export type Concession = 'none' | 'student' | 'senior';

/** Discount on the monthly fee per concession, in percent. */
export const concessionDiscount: Readonly<Record<Concession, number>> = {
  none: 0,
  student: 20,
  senior: 30,
};

/** The account holder's birth date, kept by Billing to grant age-based concessions. */
export class HolderBirthDate extends ValueObject<{ value: string }> {
  private constructor(value: string) {
    super({ value });
  }

  static parse(value: string): HolderBirthDate {
    if (!isoDate.test(value) || Number.isNaN(Date.parse(`${value}T00:00:00Z`))) {
      throw new RangeError('A birth date is a date in the form YYYY-MM-DD.');
    }
    return new HolderBirthDate(value);
  }

  get value(): string {
    return this.props.value;
  }

  /** The concession a holder of this age gets on the UTC calendar day of `on`. */
  concessionOn(on: Date): Concession {
    const [year, month, day] = this.props.value.split('-').map(Number) as [number, number, number];
    const hadBirthday =
      on.getUTCMonth() + 1 > month || (on.getUTCMonth() + 1 === month && on.getUTCDate() >= day);
    const age = on.getUTCFullYear() - year - (hadBirthday ? 0 : 1);
    if (age >= 65) {
      return 'senior';
    }
    return age < 25 ? 'student' : 'none';
  }
}
