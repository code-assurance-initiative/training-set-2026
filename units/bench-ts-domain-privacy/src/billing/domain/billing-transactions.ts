import type { BillingAccountRepository } from './accounts/billing-account-repository.js';
import type { InvoiceLineRepository } from './invoices/invoice-line-repository.js';
import type { InvoiceRepository } from './invoices/invoice-repository.js';

/** The repositories of one database transaction of the Billing context. */
export interface BillingRepositories {
  readonly accounts: BillingAccountRepository;
  readonly invoices: InvoiceRepository;
  readonly invoiceLines: InvoiceLineRepository;
}

export interface BillingTransactions {
  /** Runs `work` in one transaction: everything it saves commits together or not at all. */
  run<T>(work: (repositories: BillingRepositories) => Promise<T>): Promise<T>;
}
