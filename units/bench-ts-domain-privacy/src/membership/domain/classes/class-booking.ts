import { AggregateRoot } from '../../../shared-kernel/aggregate-root.js';
import { DomainRuleViolation } from '../../../shared-kernel/result.js';
import type { Member } from '../members/member.js';
import type { ClassBookingId } from './class-booking-id.js';
import type { ClassSessionId } from './class-session-id.js';

export type BookingStatus = 'booked' | 'cancelled';

/** A member's place in one class session. */
export class ClassBooking extends AggregateRoot<ClassBookingId> {
  readonly member: Member;
  #status: BookingStatus;
  #cancelledAt: Date | undefined;
  #remindedAt: Date | undefined;

  private constructor(
    id: ClassBookingId,
    readonly sessionId: ClassSessionId,
    member: Member,
    readonly bookedAt: Date,
    status: BookingStatus,
    cancelledAt: Date | undefined,
    remindedAt: Date | undefined,
  ) {
    super(id);
    this.member = member;
    this.#status = status;
    this.#cancelledAt = cancelledAt;
    this.#remindedAt = remindedAt;
  }

  static book(
    id: ClassBookingId,
    sessionId: ClassSessionId,
    member: Member,
    at: Date,
  ): ClassBooking {
    return new ClassBooking(id, sessionId, member, at, 'booked', undefined, undefined);
  }

  static rehydrate(state: {
    id: ClassBookingId;
    sessionId: ClassSessionId;
    member: Member;
    bookedAt: Date;
    status: BookingStatus;
    cancelledAt: Date | undefined;
    remindedAt: Date | undefined;
  }): ClassBooking {
    return new ClassBooking(
      state.id,
      state.sessionId,
      state.member,
      state.bookedAt,
      state.status,
      state.cancelledAt,
      state.remindedAt,
    );
  }

  get status(): BookingStatus {
    return this.#status;
  }

  get cancelledAt(): Date | undefined {
    return this.#cancelledAt;
  }

  get remindedAt(): Date | undefined {
    return this.#remindedAt;
  }

  cancel(at: Date): void {
    if (this.#status === 'cancelled') {
      return;
    }
    this.#status = 'cancelled';
    this.#cancelledAt = at;
  }

  markReminded(at: Date): void {
    if (this.#status !== 'booked') {
      throw new DomainRuleViolation('conflict', 'A cancelled booking is not reminded.');
    }
    this.#remindedAt = at;
  }
}
