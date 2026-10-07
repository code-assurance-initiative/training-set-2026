import { Identifier } from '../../../shared-kernel/identifier.js';

export class MemberId extends Identifier {
  private constructor(value: string) {
    super(value);
  }

  static of(value: string): MemberId {
    return new MemberId(value);
  }

  static create(): MemberId {
    return new MemberId(Identifier.newValue());
  }
}
