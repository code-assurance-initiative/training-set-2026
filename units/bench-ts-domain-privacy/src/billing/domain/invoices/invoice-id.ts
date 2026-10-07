import { Identifier } from '../../../shared-kernel/identifier.js';

export class InvoiceId extends Identifier {
  private constructor(value: string) {
    super(value);
  }

  static of(value: string): InvoiceId {
    return new InvoiceId(value);
  }

  static create(): InvoiceId {
    return new InvoiceId(Identifier.newValue());
  }
}
