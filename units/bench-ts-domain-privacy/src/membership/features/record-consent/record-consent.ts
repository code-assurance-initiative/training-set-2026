import { Router } from 'express';
import { z } from 'zod';
import type { Clock } from '../../../platform/clock.js';
import { actorOf, requireScope, Scopes } from '../../../platform/http/scopes.js';
import { idRoute, parseInput } from '../../../platform/http/validation.js';
import { ok, type Result } from '../../../shared-kernel/result.js';
import { consentPurposes, type ConsentPurpose } from '../../domain/members/consent.js';
import { MemberId } from '../../domain/members/member-id.js';
import { existingMember } from '../../domain/members/member-repository.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

const consentBody = z.object({ granted: z.boolean() });
const purposeParam = z.enum(consentPurposes);

/** Records a member's answer for one consent purpose; a later answer replaces (withdraws) an earlier one. */
export class RecordConsentHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly clock: Clock,
  ) {}

  handle(
    memberId: string,
    purpose: ConsentPurpose,
    granted: boolean,
    actor: string,
  ): Promise<Result<{ purpose: ConsentPurpose; granted: boolean }>> {
    return this.transactions.run(async ({ members }) => {
      const member = await existingMember(members, MemberId.of(memberId));
      member.recordConsent(purpose, granted, this.clock.now());
      await members.save(member, actor);
      return ok({ purpose, granted });
    });
  }
}

export function recordConsentRoutes(handler: RecordConsentHandler): Router {
  return Router().put(
    '/members/:memberId/consents/:purpose',
    requireScope(Scopes.membersWrite),
    idRoute('memberId', async (memberId, req, res) => {
      const purpose = parseInput(purposeParam, req.params.purpose, res);
      const body = purpose && parseInput(consentBody, req.body, res);
      return body && (await handler.handle(memberId, purpose, body.granted, actorOf(res)));
    }),
  );
}
