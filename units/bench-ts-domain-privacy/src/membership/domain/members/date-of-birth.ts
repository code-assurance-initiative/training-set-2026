import { ValueObject } from '../../../shared-kernel/value-object.js';

const isoDate = /^(\d{4})-(\d{2})-(\d{2})$/;

/** A calendar date of birth, without a time zone. */
export class DateOfBirth extends ValueObject<{ year: number; month: number; day: number }> {
  private constructor(year: number, month: number, day: number) {
    super({ year, month, day });
  }

  static parse(value: string): DateOfBirth {
    const match = isoDate.exec(value);
    if (!match) {
      throw new RangeError('A date of birth is a date in the form YYYY-MM-DD.');
    }
    const [year, month, day] = [match[1], match[2], match[3]].map(Number) as [
      number,
      number,
      number,
    ];
    const date = new Date(Date.UTC(year, month - 1, day));
    if (date.getUTCMonth() !== month - 1 || date.getUTCDate() !== day || year < 1900) {
      throw new RangeError('Not a valid date of birth.');
    }
    return new DateOfBirth(year, month, day);
  }

  /** Completed years of age on the UTC calendar day of `on`. */
  ageOn(on: Date): number {
    const { year, month, day } = this.props;
    const hadBirthday =
      on.getUTCMonth() + 1 > month || (on.getUTCMonth() + 1 === month && on.getUTCDate() >= day);
    return on.getUTCFullYear() - year - (hadBirthday ? 0 : 1);
  }

  override toString(): string {
    const { year, month, day } = this.props;
    return `${String(year)}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  }
}
