import { Router } from 'express';
import type { Clock } from '../../../platform/clock.js';
import { actorOf, requireScope, Scopes } from '../../../platform/http/scopes.js';
import { idRoute } from '../../../platform/http/validation.js';
import { ok, type Result } from '../../../shared-kernel/result.js';
import { MemberId } from '../../domain/members/member-id.js';
import { existingMember } from '../../domain/members/member-repository.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

/**
 * Cancels a membership at the member's request. The paid period runs out as normal; the member's
 * places in classes that have not started yet are released.
 */
export class CancelMembershipHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly clock: Clock,
  ) {}

  async handle(memberId: string, actor: string): Promise<Result<{ releasedBookings: number }>> {
    const now = this.clock.now();
    return await this.transactions.run(async ({ members, bookings }) => {
      const member = await existingMember(members, MemberId.of(memberId));
      member.cancel(now);
      await members.save(member, actor);
      const future = await bookings.listFutureForMember(member.id, now);
      for (const booking of future) {
        booking.cancel(now);
        await bookings.save(booking);
      }
      return ok({ releasedBookings: future.length });
    });
  }
}

export function cancelMembershipRoutes(handler: CancelMembershipHandler): Router {
  return Router().post(
    '/members/:memberId/cancellation',
    requireScope(Scopes.membersWrite),
    idRoute('memberId', (memberId, _req, res) => handler.handle(memberId, actorOf(res))),
  );
}
