import { Router } from 'express';
import type { Clock } from '../../../platform/clock.js';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import { idRoute } from '../../../platform/http/validation.js';
import { fail, ok, type Result } from '../../../shared-kernel/result.js';
import type { BillingTransactions } from '../../domain/billing-transactions.js';
import { InvoiceId } from '../../domain/invoices/invoice-id.js';

/** Marks an invoice paid when the payment provider reports the payment. */
export class RecordPaymentHandler {
  constructor(
    private readonly transactions: BillingTransactions,
    private readonly clock: Clock,
  ) {}

  handle(invoiceId: string): Promise<Result<{ status: string }>> {
    return this.transactions.run(async ({ invoices }) => {
      const invoice = await invoices.get(InvoiceId.of(invoiceId));
      if (!invoice) {
        return fail('not-found', 'No such invoice.');
      }
      invoice.markPaid(this.clock.now());
      await invoices.save(invoice);
      return ok({ status: invoice.status });
    });
  }
}

export function recordPaymentRoutes(handler: RecordPaymentHandler): Router {
  return Router().post(
    '/billing/invoices/:invoiceId/payment',
    requireScope(Scopes.billingWrite),
    idRoute('invoiceId', (invoiceId) => handler.handle(invoiceId)),
  );
}
