import type { Knex } from 'knex';
import type { BillingRepositories, BillingTransactions } from '../domain/billing-transactions.js';
import { KnexBillingAccountRepository } from './billing-account-store.js';
import { KnexInvoiceLineRepository, KnexInvoiceRepository } from './invoice-store.js';

/** Billing's repositories bound to one knex transaction. */
export class KnexBillingTransactions implements BillingTransactions {
  constructor(private readonly db: Knex) {}

  run<T>(work: (repositories: BillingRepositories) => Promise<T>): Promise<T> {
    return this.db.transaction((tx) =>
      work({
        accounts: new KnexBillingAccountRepository(tx),
        invoices: new KnexInvoiceRepository(tx),
        invoiceLines: new KnexInvoiceLineRepository(tx),
      }),
    );
  }
}
