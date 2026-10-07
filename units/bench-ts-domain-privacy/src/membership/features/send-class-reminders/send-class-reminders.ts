import { addMinutes, utcDay, type Clock } from '../../../platform/clock.js';
import type { ClassBooking } from '../../domain/classes/class-booking.js';
import { ClassBookingId } from '../../domain/classes/class-booking-id.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';
import type {
  UpcomingBooking,
  UpcomingBookingsQuery,
} from '../book-class/upcoming-bookings-query.js';
import type { EmailGateway } from './email-gateway.js';
import type { ReminderDeliveryLog } from './reminder-log.js';
import type { SmsGateway } from './sms-gateway.js';

/**
 * Reminds members of the classes they booked, once, ahead of the start: by e-mail always (part of
 * the membership), and by SMS to members who gave a phone number.
 */
export class SendClassRemindersHandler {
  constructor(
    private readonly upcoming: UpcomingBookingsQuery,
    private readonly transactions: MembershipTransactions,
    private readonly deliveries: ReminderDeliveryLog,
    private readonly email: EmailGateway,
    private readonly sms: SmsGateway,
    private readonly clock: Clock,
    private readonly leadMinutes: number,
  ) {}

  /** Sends the reminders that are due; returns how many bookings were reminded. */
  async run(): Promise<number> {
    const now = this.clock.now();
    const due = await this.upcoming.list({ from: now, to: addMinutes(now, this.leadMinutes) });
    let reminded = 0;
    for (const item of due) {
      if (await this.remind(item, now)) {
        reminded += 1;
      }
    }
    return reminded;
  }

  private remind(item: UpcomingBooking, now: Date): Promise<boolean> {
    return this.transactions.run(async ({ bookings }) => {
      const booking = await bookings.get(ClassBookingId.of(item.bookingId));
      if (booking?.status !== 'booked' || booking.remindedAt) {
        return false;
      }
      const member = booking.member;
      const today = utcDay(now);
      if (member.status !== 'active' || member.membershipEndsOn.getTime() < today.getTime()) {
        return false;
      }
      const text = reminderText(item);
      await this.sendEmail(booking, item, text, now);
      await this.sendSms(booking, text, now);
      booking.markReminded(now);
      await bookings.save(booking);
      return true;
    });
  }

  private async sendEmail(
    booking: ClassBooking,
    item: UpcomingBooking,
    text: string,
    now: Date,
  ): Promise<void> {
    const { member } = booking;
    const subject = `Reminder: ${item.title}`;
    if (await this.email.send({ memberId: member.id, to: member.email, subject, text })) {
      await this.deliveries.record(booking.id, 'email', member.email.value, now);
    }
  }

  private async sendSms(booking: ClassBooking, text: string, now: Date): Promise<void> {
    const phone = booking.member.phone;
    if (phone === undefined) {
      return;
    }
    if (await this.sms.send(phone, text)) {
      await this.deliveries.record(booking.id, 'sms', phone.value, now);
    }
  }
}

function reminderText(item: UpcomingBooking): string {
  const time = item.startsAt.toISOString().slice(11, 16);
  return `Your class "${item.title}" starts at ${time} UTC. Cancel in the app if you cannot come.`;
}
