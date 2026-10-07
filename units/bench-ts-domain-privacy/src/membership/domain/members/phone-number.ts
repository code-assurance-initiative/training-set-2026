import { ValueObject } from '../../../shared-kernel/value-object.js';

const e164 = /^\+[1-9]\d{6,14}$/;

/** A phone number in E.164 form (`+4512345678`). */
export class PhoneNumber extends ValueObject<{ value: string }> {
  private constructor(value: string) {
    super({ value });
  }

  static of(value: string): PhoneNumber {
    const compact = value.replace(/[\s()-]/g, '');
    if (!e164.test(compact)) {
      throw new RangeError('A phone number must be in international form, e.g. +4512345678.');
    }
    return new PhoneNumber(compact);
  }

  get value(): string {
    return this.props.value;
  }
}
