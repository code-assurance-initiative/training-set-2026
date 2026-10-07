import { Router } from 'express';
import type { Clock } from '../../../platform/clock.js';
import { actorOf, requireScope, Scopes } from '../../../platform/http/scopes.js';
import { idRoute } from '../../../platform/http/validation.js';
import { ok, type Result } from '../../../shared-kernel/result.js';
import { MemberId } from '../../domain/members/member-id.js';
import { existingMember } from '../../domain/members/member-repository.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

/**
 * Erases a member's personal data on request (right to erasure). Billing anonymises its copy when
 * it receives the member-erased message.
 */
export class EraseMemberHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly clock: Clock,
  ) {}

  handle(memberId: string, actor: string): Promise<Result<{ erased: true }>> {
    return this.transactions.run(async ({ members }) => {
      const member = await existingMember(members, MemberId.of(memberId));
      member.erase(this.clock.now());
      await members.save(member, actor);
      return ok({ erased: true });
    });
  }
}

export function eraseMemberRoutes(handler: EraseMemberHandler): Router {
  return Router().post(
    '/members/:memberId/erasure',
    requireScope(Scopes.privacy),
    idRoute('memberId', (memberId, _req, res) => handler.handle(memberId, actorOf(res))),
  );
}
