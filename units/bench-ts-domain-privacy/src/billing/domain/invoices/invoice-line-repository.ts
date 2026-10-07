import type { InvoiceId } from './invoice-id.js';
import type { InvoiceLine } from './invoice-line.js';

export interface InvoiceLineRepository {
  add(invoice: InvoiceId, line: InvoiceLine): Promise<void>;
}
