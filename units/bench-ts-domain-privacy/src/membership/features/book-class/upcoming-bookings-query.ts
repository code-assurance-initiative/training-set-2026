import type { Knex } from 'knex';

export interface UpcomingBooking {
  readonly bookingId: string;
  readonly memberId: string;
  readonly sessionId: string;
  readonly title: string;
  readonly startsAt: Date;
}

export interface UpcomingBookingsFilter {
  readonly from: Date;
  readonly to?: Date;
  readonly memberId?: string;
}

interface UpcomingRow {
  booking_id: string;
  member_id: string;
  session_id: string;
  title: string;
  starts_at: Date | string;
}

/** Read model behind "my upcoming classes": booked places in sessions that start in a window. */
export class UpcomingBookingsQuery {
  constructor(private readonly db: Knex) {}

  async list(filter: UpcomingBookingsFilter): Promise<UpcomingBooking[]> {
    const query = this.db('class_bookings')
      .join('class_sessions', 'class_sessions.id', 'class_bookings.session_id')
      .where('class_bookings.status', 'booked')
      .where('class_sessions.starts_at', '>=', filter.from)
      .select<UpcomingRow[]>(
        'class_bookings.id as booking_id',
        'class_bookings.member_id',
        'class_bookings.session_id',
        'class_sessions.title',
        'class_sessions.starts_at',
      )
      .orderBy('class_sessions.starts_at');
    if (filter.to) {
      void query.where('class_sessions.starts_at', '<', filter.to);
    }
    if (filter.memberId) {
      void query.where('class_bookings.member_id', filter.memberId);
    }
    const rows: UpcomingRow[] = await query;
    return rows.map((row) => ({
      bookingId: row.booking_id,
      memberId: row.member_id,
      sessionId: row.session_id,
      title: row.title,
      startsAt: new Date(row.starts_at),
    }));
  }
}
