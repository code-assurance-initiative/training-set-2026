import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { createTestApp, registerMember, type TestApp } from '../support/test-app.js';

interface InvoiceRow {
  id: string;
  account_id: string;
  status: string;
}

describe('billing', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
  });

  async function runInvoices(period: string): Promise<number> {
    const response = await request(api.app)
      .post('/api/billing/invoice-runs')
      .set('Authorization', api.fullAccess)
      .send({ period })
      .expect(200);
    return (response.body as { issued: number }).issued;
  }

  async function invoiceLines(): Promise<{ kind: string; amount_minor: number }[]> {
    return (await api.db('invoice_lines').orderBy('kind')) as {
      kind: string;
      amount_minor: number;
    }[];
  }

  it('opens one billing account per registered member, even when the message is redelivered', async () => {
    const memberId = await registerMember(api);
    const [message] = await api.db<{ id: string }>('outbox_messages');

    await api.services.jobs.outbox.dispatchPending();
    await api.db('outbox_messages').insert({
      id: '6e1f0c2a-9b4d-4f3e-8a7c-5d2b1e0f9c38',
      type: 'membership.member-registered',
      payload: JSON.stringify({
        memberId,
        firstName: 'Ada',
        lastName: 'Lindqvist',
        email: 'ada.lindqvist@example.net',
        dateOfBirth: '1990-04-12',
      }),
      occurred_at: new Date(),
    });
    await api.services.jobs.outbox.dispatchPending();

    expect(message).toBeDefined();
    const accounts = await api.db<Record<string, unknown>>('billing_accounts');
    expect(accounts).toHaveLength(1);
    expect(accounts[0]).toMatchObject({
      member_id: memberId,
      holder_name: 'Ada Lindqvist',
      holder_email: 'ada.lindqvist@example.net',
    });
  });

  it('issues one monthly invoice per open account and period', async () => {
    await registerMember(api);
    await api.services.jobs.outbox.dispatchPending();

    expect(await runInvoices('2026-11')).toBe(1);
    expect(await runInvoices('2026-11')).toBe(0);

    expect(await invoiceLines()).toEqual([
      expect.objectContaining({ kind: 'membership-fee', amount_minor: 3_900 }),
    ]);
  });

  it('gives students and seniors their concession', async () => {
    await registerMember(api, { email: 'student@example.net', dateOfBirth: '2004-05-01' });
    await registerMember(api, { email: 'senior@example.net', dateOfBirth: '1950-05-01' });
    await api.services.jobs.outbox.dispatchPending();

    await runInvoices('2026-11');

    const concessions = (await invoiceLines()).filter((line) => line.kind === 'concession');
    expect(concessions.map((line) => line.amount_minor).sort()).toEqual([-1170, -780]);
  });

  it('lists overdue invoices, charges the late fee once and records a payment', async () => {
    await registerMember(api);
    await api.services.jobs.outbox.dispatchPending();
    await runInvoices('2026-10');

    api.clock.advanceDays(10);
    const overdue = await request(api.app)
      .get('/api/billing/invoices/overdue')
      .set('Authorization', api.fullAccess)
      .expect(200);
    expect(overdue.body).toEqual([
      expect.objectContaining({
        period: '2026-10',
        dueOn: '2026-10-15',
        total: { amount: 3_900, currency: 'EUR' },
      }),
    ]);

    for (const charged of [1, 0]) {
      await request(api.app)
        .post('/api/billing/late-fees')
        .set('Authorization', api.fullAccess)
        .expect(200, { charged });
    }
    expect((await invoiceLines()).map((line) => line.kind)).toEqual(['late-fee', 'membership-fee']);

    const [invoice] = await api.db<InvoiceRow>('invoices');
    await request(api.app)
      .post(`/api/billing/invoices/${invoice?.id ?? ''}/payment`)
      .set('Authorization', api.fullAccess)
      .expect(200, { status: 'paid' });
    await request(api.app)
      .get('/api/billing/invoices/overdue')
      .set('Authorization', api.fullAccess)
      .expect(200, []);
  });

  it('answers 404 for payment of an unknown invoice', async () => {
    await request(api.app)
      .post('/api/billing/invoices/5a0d2c8e-1f3b-4e7a-9c6d-8b2e4f1a7c90/payment')
      .set('Authorization', api.fullAccess)
      .expect(404);
  });

  it('rejects a malformed billing period', async () => {
    await request(api.app)
      .post('/api/billing/invoice-runs')
      .set('Authorization', api.fullAccess)
      .send({ period: '2026-13' })
      .expect(400);
  });
});
