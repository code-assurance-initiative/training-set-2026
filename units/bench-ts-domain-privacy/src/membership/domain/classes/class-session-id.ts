import { Identifier } from '../../../shared-kernel/identifier.js';

export class ClassSessionId extends Identifier {
  private constructor(value: string) {
    super(value);
  }

  static of(value: string): ClassSessionId {
    return new ClassSessionId(value);
  }

  static create(): ClassSessionId {
    return new ClassSessionId(Identifier.newValue());
  }
}
