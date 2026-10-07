import { Router } from 'express';
import { z } from 'zod';
import { type Clock } from '../../../platform/clock.js';
import { sendResult } from '../../../platform/http/problem.js';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import { parseInput } from '../../../platform/http/validation.js';
import { Money } from '../../../shared-kernel/money.js';
import { ok, type Result } from '../../../shared-kernel/result.js';
import { concessionDiscount } from '../../domain/accounts/concession.js';
import type { BillingTransactions } from '../../domain/billing-transactions.js';
import { Invoice } from '../../domain/invoices/invoice.js';
import { InvoiceId } from '../../domain/invoices/invoice-id.js';

const invoiceRunBody = z.object({ period: z.string().regex(/^\d{4}-(0[1-9]|1[0-2])$/) });

export interface MembershipFee {
  readonly monthly: Money;
  /** Days after the first of the period that an invoice is due. */
  readonly paymentTermDays: number;
}

/** Issues the monthly membership invoice of every open account that has none for the period yet. */
export class IssueInvoicesHandler {
  constructor(
    private readonly transactions: BillingTransactions,
    private readonly fee: MembershipFee,
    private readonly clock: Clock,
  ) {}

  async handle(period: string): Promise<Result<{ issued: number }>> {
    const now = this.clock.now();
    const [year, month] = period.split('-').map(Number) as [number, number];
    const firstDay = new Date(Date.UTC(year, month - 1, 1));
    const dueOn = new Date(firstDay.getTime() + this.fee.paymentTermDays * 86_400_000);
    const accounts = await this.transactions.run(({ accounts: store }) => store.listOpen());
    let issued = 0;
    for (const account of accounts) {
      const accountId = account.id.value;
      const created = await this.transactions.run(async ({ invoices }) => {
        if (await invoices.findForPeriod(accountId, period)) {
          return false;
        }
        const invoice = Invoice.draft(
          InvoiceId.create(),
          accountId,
          period,
          this.fee.monthly.currency,
          dueOn,
        );
        invoice.addLine('membership-fee', `Membership ${period}`, this.fee.monthly);
        const concession = account.concessionOn(firstDay);
        if (concession !== 'none') {
          const discount = this.fee.monthly.percent(-concessionDiscount[concession]);
          invoice.addLine('concession', `${concession} concession`, discount);
        }
        invoice.issue(now);
        await invoices.save(invoice);
        return true;
      });
      issued += created ? 1 : 0;
    }
    return ok({ issued });
  }
}

export function issueInvoicesRoutes(handler: IssueInvoicesHandler): Router {
  return Router().post(
    '/billing/invoice-runs',
    requireScope(Scopes.billingWrite),
    async (req, res) => {
      const body = parseInput(invoiceRunBody, req.body, res);
      if (body) {
        sendResult(res, await handler.handle(body.period));
      }
    },
  );
}
