import { Identifier } from '../../../shared-kernel/identifier.js';

export class ClassBookingId extends Identifier {
  private constructor(value: string) {
    super(value);
  }

  static of(value: string): ClassBookingId {
    return new ClassBookingId(value);
  }

  static create(): ClassBookingId {
    return new ClassBookingId(Identifier.newValue());
  }
}
