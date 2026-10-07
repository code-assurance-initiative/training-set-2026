import { ValueObject } from '../../../shared-kernel/value-object.js';

const pattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export class EmailAddress extends ValueObject<{ value: string }> {
  private constructor(value: string) {
    super({ value });
  }

  static of(value: string): EmailAddress {
    const normalised = value.trim().toLowerCase();
    if (normalised.length > 254 || !pattern.test(normalised)) {
      throw new RangeError('Not an e-mail address.');
    }
    return new EmailAddress(normalised);
  }

  get value(): string {
    return this.props.value;
  }

  /** The part after the '@': identifies a mail provider, not a person. */
  get domain(): string {
    return this.props.value.slice(this.props.value.lastIndexOf('@') + 1);
  }
}
