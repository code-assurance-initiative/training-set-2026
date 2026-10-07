import type { MemberId } from '../members/member-id.js';
import type { ClassBooking } from './class-booking.js';
import type { ClassBookingId } from './class-booking-id.js';
import type { ClassSessionId } from './class-session-id.js';

export interface ClassBookingRepository {
  get(id: ClassBookingId): Promise<ClassBooking | undefined>;
  /** Places taken in a session (bookings that are not cancelled). */
  countBooked(session: ClassSessionId): Promise<number>;
  findActive(session: ClassSessionId, member: MemberId): Promise<ClassBooking | undefined>;
  /** The member's booked places in sessions that start at or after `from`. */
  listFutureForMember(member: MemberId, from: Date): Promise<ClassBooking[]>;
  /** Every booking the member ever made, cancelled ones included. */
  listForMember(member: MemberId): Promise<ClassBooking[]>;
  save(booking: ClassBooking): Promise<void>;
}
