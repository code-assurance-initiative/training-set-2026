import { Router } from 'express';
import type { Services } from '../composition.js';
import { chargeLateFeesRoutes } from './features/charge-late-fees/charge-late-fees.js';
import { issueInvoicesRoutes } from './features/issue-invoices/issue-invoices.js';
import { listOverdueInvoicesRoutes } from './features/list-overdue-invoices/list-overdue-invoices.js';
import { recordPaymentRoutes } from './features/record-payment/record-payment.js';

export function billingRoutes(services: Services): Router {
  const slices = services.billing;
  return Router().use(
    issueInvoicesRoutes(slices.issueInvoices),
    chargeLateFeesRoutes(slices.chargeLateFees),
    listOverdueInvoicesRoutes(slices.listOverdueInvoices),
    recordPaymentRoutes(slices.recordPayment),
  );
}
