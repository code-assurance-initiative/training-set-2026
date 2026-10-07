import { Identifier } from '../../../shared-kernel/identifier.js';

export class InvoiceLineId extends Identifier {
  private constructor(value: string) {
    super(value);
  }

  static of(value: string): InvoiceLineId {
    return new InvoiceLineId(value);
  }

  static create(): InvoiceLineId {
    return new InvoiceLineId(Identifier.newValue());
  }
}
