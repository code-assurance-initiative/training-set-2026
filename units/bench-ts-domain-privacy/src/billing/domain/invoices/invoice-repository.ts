import type { Invoice } from './invoice.js';
import type { InvoiceId } from './invoice-id.js';

export interface InvoiceRepository {
  get(id: InvoiceId): Promise<Invoice | undefined>;
  findForPeriod(accountId: string, period: string): Promise<Invoice | undefined>;
  listIssued(): Promise<Invoice[]>;
  save(invoice: Invoice): Promise<void>;
}
