import type { Knex } from 'knex';
import { ClassBooking, type BookingStatus } from '../domain/classes/class-booking.js';
import { ClassBookingId } from '../domain/classes/class-booking-id.js';
import type { ClassBookingRepository } from '../domain/classes/class-booking-repository.js';
import { ClassSessionId } from '../domain/classes/class-session-id.js';
import { MemberId } from '../domain/members/member-id.js';
import type { MemberRepository } from '../domain/members/member-repository.js';

interface ClassBookingRow {
  id: string;
  session_id: string;
  member_id: string;
  status: BookingStatus;
  booked_at: Date | string;
  cancelled_at: Date | string | null;
  reminded_at: Date | string | null;
}

/** Bookings in PostgreSQL; each booking is loaded with its member. */
export class KnexClassBookingRepository implements ClassBookingRepository {
  constructor(
    private readonly db: Knex,
    private readonly members: MemberRepository,
  ) {}

  async get(id: ClassBookingId): Promise<ClassBooking | undefined> {
    const row = await this.db<ClassBookingRow>('class_bookings').where({ id: id.value }).first();
    return row ? this.toBooking(row) : undefined;
  }

  async countBooked(session: ClassSessionId): Promise<number> {
    const rows = await this.db<ClassBookingRow>('class_bookings').where({
      session_id: session.value,
      status: 'booked',
    });
    return rows.length;
  }

  async findActive(session: ClassSessionId, member: MemberId): Promise<ClassBooking | undefined> {
    const row = await this.db<ClassBookingRow>('class_bookings')
      .where({ session_id: session.value, member_id: member.value, status: 'booked' })
      .first();
    return row ? this.toBooking(row) : undefined;
  }

  async listFutureForMember(member: MemberId, from: Date): Promise<ClassBooking[]> {
    const rows: ClassBookingRow[] = await this.db<ClassBookingRow>('class_bookings')
      .join('class_sessions', 'class_sessions.id', 'class_bookings.session_id')
      .where('class_bookings.member_id', member.value)
      .where('class_bookings.status', 'booked')
      .where('class_sessions.starts_at', '>=', from)
      .select<ClassBookingRow[]>('class_bookings.*');
    return Promise.all(rows.map((row) => this.toBooking(row)));
  }

  async listForMember(member: MemberId): Promise<ClassBooking[]> {
    const rows = await this.db<ClassBookingRow>('class_bookings')
      .where({ member_id: member.value })
      .orderBy('booked_at');
    return Promise.all(rows.map((row) => this.toBooking(row)));
  }

  async save(booking: ClassBooking): Promise<void> {
    const row = {
      session_id: booking.sessionId.value,
      member_id: booking.member.id.value,
      status: booking.status,
      booked_at: booking.bookedAt,
      cancelled_at: booking.cancelledAt ?? null,
      reminded_at: booking.remindedAt ?? null,
    };
    const updated = await this.db('class_bookings').where({ id: booking.id.value }).update(row);
    if (updated === 0) {
      await this.db('class_bookings').insert({ id: booking.id.value, ...row });
    }
  }

  private async toBooking(row: ClassBookingRow): Promise<ClassBooking> {
    const member = await this.members.get(MemberId.of(row.member_id));
    if (!member) {
      throw new Error(`Booking ${row.id} refers to a member that does not exist.`);
    }
    return ClassBooking.rehydrate({
      id: ClassBookingId.of(row.id),
      sessionId: ClassSessionId.of(row.session_id),
      member,
      bookedAt: new Date(row.booked_at),
      status: row.status,
      cancelledAt: row.cancelled_at === null ? undefined : new Date(row.cancelled_at),
      remindedAt: row.reminded_at === null ? undefined : new Date(row.reminded_at),
    });
  }
}
