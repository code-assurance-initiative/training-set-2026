import { Identifier } from '../../../shared-kernel/identifier.js';

export class BillingAccountId extends Identifier {
  private constructor(value: string) {
    super(value);
  }

  static of(value: string): BillingAccountId {
    return new BillingAccountId(value);
  }

  static create(): BillingAccountId {
    return new BillingAccountId(Identifier.newValue());
  }
}
