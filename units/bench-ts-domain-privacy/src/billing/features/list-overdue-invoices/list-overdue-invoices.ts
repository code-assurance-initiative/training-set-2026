import { Router } from 'express';
import { utcDay, type Clock } from '../../../platform/clock.js';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import type { BillingTransactions } from '../../domain/billing-transactions.js';

export interface OverdueInvoice {
  readonly invoiceId: string;
  readonly accountId: string;
  readonly period: string;
  readonly dueOn: string;
  readonly total: { amount: number; currency: string };
}

export class ListOverdueInvoicesQuery {
  constructor(
    private readonly transactions: BillingTransactions,
    private readonly clock: Clock,
  ) {}

  async list(): Promise<OverdueInvoice[]> {
    const today = utcDay(this.clock.now());
    const issued = await this.transactions.run(({ invoices }) => invoices.listIssued());
    return issued
      .filter((invoice) => invoice.isOverdue(today))
      .map((invoice) => ({
        invoiceId: invoice.id.value,
        accountId: invoice.accountId,
        period: invoice.period,
        dueOn: invoice.dueOn.toISOString().slice(0, 10),
        total: invoice.total.toJSON(),
      }));
  }
}

export function listOverdueInvoicesRoutes(query: ListOverdueInvoicesQuery): Router {
  return Router().get(
    '/billing/invoices/overdue',
    requireScope(Scopes.billingRead),
    async (_req, res) => {
      res.json(await query.list());
    },
  );
}
