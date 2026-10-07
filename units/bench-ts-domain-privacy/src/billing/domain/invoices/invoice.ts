import { AggregateRoot } from '../../../shared-kernel/aggregate-root.js';
import { Money } from '../../../shared-kernel/money.js';
import { DomainRuleViolation } from '../../../shared-kernel/result.js';
import type { InvoiceId } from './invoice-id.js';
import { InvoiceLine, type InvoiceLineKind } from './invoice-line.js';
import { InvoiceLineId } from './invoice-line-id.js';

export type InvoiceStatus = 'draft' | 'issued' | 'paid';

const periodPattern = /^\d{4}-(0[1-9]|1[0-2])$/;

export interface InvoiceState {
  readonly id: InvoiceId;
  readonly accountId: string;
  readonly period: string;
  readonly currency: string;
  readonly status: InvoiceStatus;
  readonly dueOn: Date;
  readonly issuedAt: Date | undefined;
  readonly paidAt: Date | undefined;
  readonly lines: readonly InvoiceLine[];
}

/** A monthly membership invoice. Lines can be added while it is a draft; issuing fixes them. */
export class Invoice extends AggregateRoot<InvoiceId> {
  readonly accountId: string;
  readonly period: string;
  readonly currency: string;
  readonly dueOn: Date;
  #status: InvoiceStatus;
  #issuedAt: Date | undefined;
  #paidAt: Date | undefined;
  readonly #lines: InvoiceLine[];

  private constructor(state: InvoiceState) {
    super(state.id);
    this.accountId = state.accountId;
    this.period = state.period;
    this.currency = state.currency;
    this.dueOn = state.dueOn;
    this.#status = state.status;
    this.#issuedAt = state.issuedAt;
    this.#paidAt = state.paidAt;
    this.#lines = [...state.lines];
  }

  static draft(
    id: InvoiceId,
    accountId: string,
    period: string,
    currency: string,
    dueOn: Date,
  ): Invoice {
    if (!periodPattern.test(period)) {
      throw new RangeError(`Not a billing period (YYYY-MM): '${period}'.`);
    }
    return new Invoice({
      id,
      accountId,
      period,
      currency,
      status: 'draft',
      dueOn,
      issuedAt: undefined,
      paidAt: undefined,
      lines: [],
    });
  }

  static rehydrate(state: InvoiceState): Invoice {
    return new Invoice(state);
  }

  get status(): InvoiceStatus {
    return this.#status;
  }

  get issuedAt(): Date | undefined {
    return this.#issuedAt;
  }

  get paidAt(): Date | undefined {
    return this.#paidAt;
  }

  get lines(): readonly InvoiceLine[] {
    return [...this.#lines];
  }

  get total(): Money {
    return this.#lines.reduce((sum, line) => sum.add(line.amount), Money.zero(this.currency));
  }

  addLine(kind: InvoiceLineKind, description: string, amount: Money): void {
    if (this.#status !== 'draft') {
      throw new DomainRuleViolation('conflict', 'Lines can only be added to a draft invoice.');
    }
    if (amount.currency !== this.currency) {
      throw new DomainRuleViolation('invalid', `The invoice is in ${this.currency}.`);
    }
    this.#lines.push(new InvoiceLine(InvoiceLineId.create(), kind, description, amount));
  }

  issue(at: Date): void {
    if (this.#status !== 'draft') {
      throw new DomainRuleViolation('conflict', 'The invoice has already been issued.');
    }
    if (this.#lines.length === 0 || this.total.isNegative()) {
      throw new DomainRuleViolation(
        'invalid',
        'An invoice needs lines and a total of zero or more.',
      );
    }
    this.#status = 'issued';
    this.#issuedAt = at;
  }

  markPaid(at: Date): void {
    if (this.#status !== 'issued') {
      throw new DomainRuleViolation('conflict', 'Only an issued invoice can be paid.');
    }
    this.#status = 'paid';
    this.#paidAt = at;
  }

  /** Issued, unpaid and past its due date on the UTC calendar day `today`. */
  isOverdue(today: Date): boolean {
    return this.#status === 'issued' && today.getTime() > this.dueOn.getTime();
  }

  hasLineOfKind(kind: InvoiceLineKind): boolean {
    return this.#lines.some((line) => line.kind === kind);
  }
}
