import { Entity } from '../../../shared-kernel/entity.js';
import type { Money } from '../../../shared-kernel/money.js';
import type { InvoiceLineId } from './invoice-line-id.js';

export type InvoiceLineKind = 'membership-fee' | 'concession' | 'late-fee';

/** One line of an invoice; it exists only as part of its invoice. */
export class InvoiceLine extends Entity<InvoiceLineId> {
  constructor(
    id: InvoiceLineId,
    readonly kind: InvoiceLineKind,
    readonly description: string,
    readonly amount: Money,
  ) {
    super(id);
    if (description.trim().length === 0) {
      throw new RangeError('An invoice line needs a description.');
    }
  }
}
