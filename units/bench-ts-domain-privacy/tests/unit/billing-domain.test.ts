import { describe, expect, it } from 'vitest';
import { BillingAccount } from '../../src/billing/domain/accounts/billing-account.js';
import { BillingAccountId } from '../../src/billing/domain/accounts/billing-account-id.js';
import { HolderBirthDate } from '../../src/billing/domain/accounts/concession.js';
import { Invoice } from '../../src/billing/domain/invoices/invoice.js';
import { InvoiceId } from '../../src/billing/domain/invoices/invoice-id.js';
import { InvoiceLine } from '../../src/billing/domain/invoices/invoice-line.js';
import { InvoiceLineId } from '../../src/billing/domain/invoices/invoice-line-id.js';
import { MemberId } from '../../src/membership/domain/members/member-id.js';
import { Money } from '../../src/shared-kernel/money.js';

const opened = new Date('2026-10-07T08:00:00Z');
const holder = {
  name: 'Ada Lindqvist',
  email: 'ada.lindqvist@example.net',
  birthDate: HolderBirthDate.parse('1961-10-08'),
};

function draft(): Invoice {
  return Invoice.draft(
    InvoiceId.create(),
    BillingAccountId.create().value,
    '2026-10',
    'EUR',
    new Date('2026-10-15'),
  );
}

describe('BillingAccount', () => {
  it('validates the holder it is opened for', () => {
    const open = (name: string, email: string) =>
      new BillingAccount(
        BillingAccountId.create(),
        MemberId.create(),
        { ...holder, name, email },
        opened,
      );

    expect(() => open(' ', holder.email)).toThrow('A billing account needs the holder name.');
    expect(() => open(holder.name, 'nobody')).toThrow(
      'A billing account needs a valid e-mail address.',
    );
    expect(
      () => new BillingAccount(BillingAccountId.create(), MemberId.create(), undefined, opened),
    ).toThrow('A billing account needs its holder.');
  });

  it('grants the senior concession from the 65th birthday, and none once anonymised', () => {
    const account = new BillingAccount(
      BillingAccountId.create(),
      MemberId.create(),
      holder,
      opened,
    );

    expect(account.concessionOn(new Date('2026-10-07'))).toBe('none');
    expect(account.concessionOn(new Date('2026-10-08'))).toBe('senior');
    account.anonymise(opened);
    account.anonymise(new Date('2027-01-01'));
    expect(account.holder).toBeUndefined();
    expect(account.anonymisedAt).toEqual(opened);
    expect(account.concessionOn(new Date('2026-10-08'))).toBe('none');
  });

  it('reads birth dates in ISO form only', () => {
    expect(HolderBirthDate.parse('2004-05-01').concessionOn(new Date('2026-10-07'))).toBe(
      'student',
    );
    expect(() => HolderBirthDate.parse('01.05.2004')).toThrow(RangeError);
  });
});

describe('Invoice', () => {
  it('totals its lines and fixes them when issued', () => {
    const invoice = draft();
    invoice.addLine('membership-fee', 'Membership 2026-10', Money.of(3_900, 'EUR'));
    invoice.addLine('concession', 'student concession', Money.of(-780, 'EUR'));

    invoice.issue(opened);

    expect(invoice.total.minorUnits).toBe(3_120);
    expect(() => {
      invoice.addLine('late-fee', 'Late payment fee', Money.of(500, 'EUR'));
    }).toThrow('Lines can only be added to a draft invoice.');
    expect(() => {
      invoice.issue(opened);
    }).toThrow('The invoice has already been issued.');
  });

  it('is overdue only when issued, unpaid and past due', () => {
    const invoice = draft();
    invoice.addLine('membership-fee', 'Membership 2026-10', Money.of(3_900, 'EUR'));
    const dayAfterDue = new Date('2026-10-16');

    expect(invoice.isOverdue(dayAfterDue)).toBe(false);
    invoice.issue(opened);
    expect(invoice.isOverdue(new Date('2026-10-15'))).toBe(false);
    expect(invoice.isOverdue(dayAfterDue)).toBe(true);
    invoice.markPaid(dayAfterDue);
    expect(invoice.isOverdue(dayAfterDue)).toBe(false);
    expect(() => {
      invoice.markPaid(dayAfterDue);
    }).toThrow('Only an issued invoice can be paid.');
  });

  it('refuses empty invoices, other currencies and bad periods', () => {
    const invoice = draft();

    expect(() => {
      invoice.issue(opened);
    }).toThrow();
    expect(() => {
      invoice.addLine('membership-fee', 'Fee', Money.of(3_900, 'DKK'));
    }).toThrow('The invoice is in EUR.');
    expect(() => Invoice.draft(InvoiceId.create(), 'x', '2026-13', 'EUR', opened)).toThrow(
      RangeError,
    );
    expect(
      () => new InvoiceLine(InvoiceLineId.create(), 'late-fee', ' ', Money.of(500, 'EUR')),
    ).toThrow(RangeError);
  });
});
