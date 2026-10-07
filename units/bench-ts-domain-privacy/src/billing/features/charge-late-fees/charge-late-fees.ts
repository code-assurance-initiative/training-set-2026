import { Router } from 'express';
import { utcDay, type Clock } from '../../../platform/clock.js';
import { sendResult } from '../../../platform/http/problem.js';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import type { Money } from '../../../shared-kernel/money.js';
import { ok, type Result } from '../../../shared-kernel/result.js';
import type { BillingTransactions } from '../../domain/billing-transactions.js';
import { InvoiceLine } from '../../domain/invoices/invoice-line.js';
import { InvoiceLineId } from '../../domain/invoices/invoice-line-id.js';

/** Adds the late-payment fee to every overdue invoice that does not carry one yet. */
export class ChargeLateFeesHandler {
  constructor(
    private readonly transactions: BillingTransactions,
    private readonly lateFee: Money,
    private readonly clock: Clock,
  ) {}

  handle(): Promise<Result<{ charged: number }>> {
    const today = utcDay(this.clock.now());
    return this.transactions.run(async ({ invoices, invoiceLines }) => {
      let charged = 0;
      for (const invoice of await invoices.listIssued()) {
        if (invoice.isOverdue(today) && !invoice.hasLineOfKind('late-fee')) {
          const line = new InvoiceLine(
            InvoiceLineId.create(),
            'late-fee',
            'Late payment fee',
            this.lateFee,
          );
          await invoiceLines.add(invoice.id, line);
          charged += 1;
        }
      }
      return ok({ charged });
    });
  }
}

export function chargeLateFeesRoutes(handler: ChargeLateFeesHandler): Router {
  return Router().post(
    '/billing/late-fees',
    requireScope(Scopes.billingWrite),
    async (_req, res) => {
      sendResult(res, await handler.handle());
    },
  );
}
