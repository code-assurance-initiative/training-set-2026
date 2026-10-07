import { Router } from 'express';
import { z } from 'zod';
import { utcDay, type Clock } from '../../../platform/clock.js';
import { sendResult } from '../../../platform/http/problem.js';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import { parseInput, uuidParam } from '../../../platform/http/validation.js';
import { fail, ok, type Result } from '../../../shared-kernel/result.js';
import { ClassBooking } from '../../domain/classes/class-booking.js';
import { ClassBookingId } from '../../domain/classes/class-booking-id.js';
import { ClassSessionId } from '../../domain/classes/class-session-id.js';
import { MemberId } from '../../domain/members/member-id.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';
import type { UpcomingBookingsQuery } from './upcoming-bookings-query.js';

const bookClassBody = z.object({ memberId: z.uuid() });

export class BookClassHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly clock: Clock,
  ) {}

  handle(sessionId: string, memberId: string): Promise<Result<{ bookingId: string }>> {
    const now = this.clock.now();
    return this.transactions.run(async ({ members, sessions, bookings }) => {
      const member = await members.get(MemberId.of(memberId));
      const session = await sessions.get(ClassSessionId.of(sessionId));
      if (!member || !session) {
        return fail('not-found', 'No such member or class.');
      }
      const today = utcDay(now);
      if (member.status !== 'active' || member.membershipEndsOn.getTime() < today.getTime()) {
        return fail('conflict', 'Only members in good standing can book classes.');
      }
      if (session.hasStarted(now)) {
        return fail('conflict', 'The class has already started.');
      }
      if (await bookings.findActive(session.id, member.id)) {
        return fail('conflict', 'The member has already booked this class.');
      }
      if (!session.hasRoomFor(await bookings.countBooked(session.id))) {
        return fail('conflict', 'The class is full.');
      }
      const booking = ClassBooking.book(ClassBookingId.create(), session.id, member, now);
      await bookings.save(booking);
      return ok({ bookingId: booking.id.value });
    });
  }
}

export function bookClassRoutes(
  handler: BookClassHandler,
  upcoming: UpcomingBookingsQuery,
  clock: Clock,
): Router {
  return Router()
    .post('/classes/:sessionId/bookings', requireScope(Scopes.membersWrite), async (req, res) => {
      const sessionId = uuidParam(req, res, 'sessionId');
      const body = sessionId && parseInput(bookClassBody, req.body, res);
      if (sessionId && body) {
        const result = await handler.handle(sessionId, body.memberId);
        sendResult(res, result, (created) => `/api/bookings/${created.bookingId}`);
      }
    })
    .get('/members/:memberId/bookings', requireScope(Scopes.membersRead), async (req, res) => {
      const memberId = uuidParam(req, res, 'memberId');
      if (memberId) {
        res.json(await upcoming.list({ from: clock.now(), memberId }));
      }
    });
}
