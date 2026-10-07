import { randomUUID } from 'node:crypto';
import type { Knex } from 'knex';
import type { ClassBookingId } from '../../domain/classes/class-booking-id.js';

export type ReminderChannel = 'email' | 'sms';

/** Which reminders went where: answers "did I get a reminder?" and provider disputes. */
export class ReminderDeliveryLog {
  constructor(private readonly db: Knex) {}

  async record(
    booking: ClassBookingId,
    channel: ReminderChannel,
    recipient: string,
    sentAt: Date,
  ): Promise<void> {
    await this.db('reminder_deliveries').insert({
      id: randomUUID(),
      booking_id: booking.value,
      channel,
      recipient,
      sent_at: sentAt,
    });
  }
}
