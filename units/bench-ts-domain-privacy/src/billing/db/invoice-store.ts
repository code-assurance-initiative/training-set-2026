import type { Knex } from 'knex';
import { dayFrom, isoDay } from '../../platform/clock.js';
import { Money } from '../../shared-kernel/money.js';
import { Invoice, type InvoiceStatus } from '../domain/invoices/invoice.js';
import { InvoiceId } from '../domain/invoices/invoice-id.js';
import { InvoiceLine, type InvoiceLineKind } from '../domain/invoices/invoice-line.js';
import type { InvoiceLineRepository } from '../domain/invoices/invoice-line-repository.js';
import { InvoiceLineId } from '../domain/invoices/invoice-line-id.js';
import type { InvoiceRepository } from '../domain/invoices/invoice-repository.js';

interface InvoiceRow {
  id: string;
  account_id: string;
  period: string;
  currency: string;
  status: InvoiceStatus;
  due_on: Date | string;
  issued_at: Date | string | null;
  paid_at: Date | string | null;
}

interface InvoiceLineRow {
  id: string;
  invoice_id: string;
  kind: InvoiceLineKind;
  description: string;
  amount_minor: number;
}

export class KnexInvoiceRepository implements InvoiceRepository {
  constructor(private readonly db: Knex) {}

  async get(id: InvoiceId): Promise<Invoice | undefined> {
    const row = await this.db<InvoiceRow>('invoices').where({ id: id.value }).first();
    return row && this.toInvoice(row);
  }

  async findForPeriod(accountId: string, period: string): Promise<Invoice | undefined> {
    const row = await this.db<InvoiceRow>('invoices')
      .where({ account_id: accountId, period })
      .first();
    return row && this.toInvoice(row);
  }

  async listIssued(): Promise<Invoice[]> {
    const rows = await this.db<InvoiceRow>('invoices')
      .where({ status: 'issued' })
      .orderBy('due_on');
    return Promise.all(rows.map((row) => this.toInvoice(row)));
  }

  async save(invoice: Invoice): Promise<void> {
    const row = {
      account_id: invoice.accountId,
      period: invoice.period,
      currency: invoice.currency,
      status: invoice.status,
      due_on: isoDay(invoice.dueOn),
      issued_at: invoice.issuedAt ?? null,
      paid_at: invoice.paidAt ?? null,
    };
    const updated = await this.db('invoices').where({ id: invoice.id.value }).update(row);
    if (updated === 0) {
      await this.db('invoices').insert({ id: invoice.id.value, ...row });
    }
    await this.db('invoice_lines').where({ invoice_id: invoice.id.value }).delete();
    if (invoice.lines.length > 0) {
      await this.db('invoice_lines').insert(invoice.lines.map((line) => lineRow(invoice.id, line)));
    }
  }

  private async toInvoice(row: InvoiceRow): Promise<Invoice> {
    const lines = await this.db<InvoiceLineRow>('invoice_lines').where({ invoice_id: row.id });
    return Invoice.rehydrate({
      id: InvoiceId.of(row.id),
      accountId: row.account_id,
      period: row.period,
      currency: row.currency,
      status: row.status,
      dueOn: dayFrom(row.due_on),
      issuedAt: row.issued_at === null ? undefined : new Date(row.issued_at),
      paidAt: row.paid_at === null ? undefined : new Date(row.paid_at),
      lines: lines.map((line) => toLine(line, row.currency)),
    });
  }
}

export class KnexInvoiceLineRepository implements InvoiceLineRepository {
  constructor(private readonly db: Knex) {}

  async add(invoice: InvoiceId, line: InvoiceLine): Promise<void> {
    await this.db('invoice_lines').insert(lineRow(invoice, line));
  }
}

function lineRow(invoice: InvoiceId, line: InvoiceLine): InvoiceLineRow {
  return {
    id: line.id.value,
    invoice_id: invoice.value,
    kind: line.kind,
    description: line.description,
    amount_minor: line.amount.minorUnits,
  };
}

function toLine(row: InvoiceLineRow, currency: string): InvoiceLine {
  return new InvoiceLine(
    InvoiceLineId.of(row.id),
    row.kind,
    row.description,
    Money.of(row.amount_minor, currency),
  );
}
