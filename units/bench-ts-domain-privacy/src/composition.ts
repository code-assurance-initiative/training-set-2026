import type { Knex } from 'knex';
import type { Logger } from 'pino';
import { KnexBillingTransactions } from './billing/db/unit-of-work.js';
import { AnonymiseBillingAccountHandler } from './billing/features/anonymise-billing-account/anonymise-billing-account.js';
import { ChargeLateFeesHandler } from './billing/features/charge-late-fees/charge-late-fees.js';
import { IssueInvoicesHandler } from './billing/features/issue-invoices/issue-invoices.js';
import { ListOverdueInvoicesQuery } from './billing/features/list-overdue-invoices/list-overdue-invoices.js';
import { OpenBillingAccountHandler } from './billing/features/open-billing-account/open-billing-account.js';
import { RecordPaymentHandler } from './billing/features/record-payment/record-payment.js';
import type { AppConfig } from './config.js';
import { KnexMembershipTransactions } from './membership/db/unit-of-work.js';
import {
  memberErasedMessage,
  memberRegisteredMessage,
} from './membership/contracts/integration-events.js';
import { ContactUniqueness } from './membership/domain/members/contact-uniqueness.js';
import { BookClassHandler } from './membership/features/book-class/book-class.js';
import { UpcomingBookingsQuery } from './membership/features/book-class/upcoming-bookings-query.js';
import { CancelMembershipHandler } from './membership/features/cancel-membership/cancel-membership.js';
import { ChangeContactDetailsHandler } from './membership/features/change-contact-details/change-contact-details.js';
import { EraseMemberHandler } from './membership/features/erase-member/erase-member.js';
import { ExportMemberDataHandler } from './membership/features/export-member-data/export-member-data.js';
import { PurgeLapsedMembersHandler } from './membership/features/purge-lapsed-members/purge-lapsed-members.js';
import { RecordConsentHandler } from './membership/features/record-consent/record-consent.js';
import { RegisterMemberHandler } from './membership/features/register-member/register-member.js';
import { ScheduleClassHandler } from './membership/features/schedule-class/schedule-class.js';
import { HttpEmailGateway } from './membership/features/send-class-reminders/email-gateway.js';
import { ReminderDeliveryLog } from './membership/features/send-class-reminders/reminder-log.js';
import { SendClassRemindersHandler } from './membership/features/send-class-reminders/send-class-reminders.js';
import { HttpSmsGateway } from './membership/features/send-class-reminders/sms-gateway.js';
import type { Clock } from './platform/clock.js';
import { createFieldCipher } from './platform/field-cipher.js';
import { IntegrationBus } from './platform/integration-bus.js';
import { OutboxDispatcher } from './platform/outbox.js';
import { createPseudonymiser } from './platform/pseudonym.js';
import { Money } from './shared-kernel/money.js';

export interface Dependencies {
  readonly config: AppConfig;
  readonly db: Knex;
  readonly logger: Logger;
  readonly clock: Clock;
  /** The HTTP client of the e-mail and SMS gateways. */
  readonly fetchFn?: typeof fetch;
}

export type Services = ReturnType<typeof composeServices>;

/** Wires the slices of both contexts and the integration-message subscriptions between them. */
export function composeServices(deps: Dependencies) {
  const { config, db, logger, clock } = deps;
  const fetchFn = deps.fetchFn ?? fetch;
  const membership = new KnexMembershipTransactions(
    db,
    createFieldCipher(config.fieldEncryptionKey),
  );
  const billing = new KnexBillingTransactions(db);
  const uniqueness = new ContactUniqueness(db);
  const upcoming = new UpcomingBookingsQuery(db);
  const fee = Money.of(config.billing.monthlyFeeMinor, config.billing.currency);

  const bus = new IntegrationBus();
  const openAccount = new OpenBillingAccountHandler(billing);
  const anonymiseAccount = new AnonymiseBillingAccountHandler(billing);
  bus.subscribe(memberRegisteredMessage, (message) => openAccount.handle(message));
  bus.subscribe(memberErasedMessage, (message) => anonymiseAccount.handle(message));

  const reminderLogger = logger.child({ component: 'class-reminders' });
  return {
    clock,
    db,
    membership: {
      register: new RegisterMemberHandler(membership, uniqueness, clock),
      changeContactDetails: new ChangeContactDetailsHandler(membership, uniqueness, clock),
      recordConsent: new RecordConsentHandler(membership, clock),
      scheduleClass: new ScheduleClassHandler(membership),
      bookClass: new BookClassHandler(membership, clock),
      upcoming,
      cancelMembership: new CancelMembershipHandler(membership, clock),
      eraseMember: new EraseMemberHandler(membership, clock),
      exportMemberData: new ExportMemberDataHandler(membership),
    },
    billing: {
      issueInvoices: new IssueInvoicesHandler(
        billing,
        { monthly: fee, paymentTermDays: config.billing.paymentTermDays },
        clock,
      ),
      chargeLateFees: new ChargeLateFeesHandler(
        billing,
        Money.of(config.billing.lateFeeMinor, config.billing.currency),
        clock,
      ),
      listOverdueInvoices: new ListOverdueInvoicesQuery(billing, clock),
      recordPayment: new RecordPaymentHandler(billing, clock),
    },
    jobs: {
      outbox: new OutboxDispatcher(db, bus, logger.child({ component: 'outbox' })),
      reminders: new SendClassRemindersHandler(
        upcoming,
        membership,
        new ReminderDeliveryLog(db),
        new HttpEmailGateway(
          config.email,
          createPseudonymiser(config.pseudonymKey),
          reminderLogger,
          fetchFn,
        ),
        new HttpSmsGateway(config.sms, reminderLogger, fetchFn),
        clock,
        config.reminderLeadMinutes,
      ),
      purgeLapsedMembers: new PurgeLapsedMembersHandler(
        membership,
        clock,
        config.memberRetentionMonths,
        logger.child({ component: 'retention' }),
      ),
    },
  };
}
