import { Router } from 'express';
import { z } from 'zod';
import type { Clock } from '../../../platform/clock.js';
import { actorOf, requireScope, Scopes } from '../../../platform/http/scopes.js';
import { idRoute, parseInput } from '../../../platform/http/validation.js';
import { fail, ok, type Result } from '../../../shared-kernel/result.js';
import type { ContactUniqueness } from '../../domain/members/contact-uniqueness.js';
import { EmailAddress } from '../../domain/members/email-address.js';
import { MemberId } from '../../domain/members/member-id.js';
import { existingMember } from '../../domain/members/member-repository.js';
import { PhoneNumber } from '../../domain/members/phone-number.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

export const contactDetailsBody = z.object({
  email: z.email(),
  phone: z.string().min(1).max(32).optional(),
});

export type ChangeContactDetails = z.infer<typeof contactDetailsBody> & { memberId: string };

export class ChangeContactDetailsHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly uniqueness: ContactUniqueness,
    private readonly clock: Clock,
  ) {}

  async handle(command: ChangeContactDetails, actor: string): Promise<Result<{ changed: true }>> {
    const id = MemberId.of(command.memberId);
    const email = EmailAddress.of(command.email);
    const phone = command.phone === undefined ? undefined : PhoneNumber.of(command.phone);
    if (!(await this.uniqueness.isAvailableFor(email, id))) {
      return fail('conflict', 'Another member uses this e-mail address.');
    }
    return this.transactions.run(async ({ members }) => {
      const member = await existingMember(members, id);
      member.changeContactDetails(email, phone, this.clock.now());
      await members.save(member, actor);
      return ok({ changed: true });
    });
  }
}

export function changeContactDetailsRoutes(handler: ChangeContactDetailsHandler): Router {
  return Router().put(
    '/members/:memberId/contact-details',
    requireScope(Scopes.membersWrite),
    idRoute('memberId', async (memberId, req, res) => {
      const body = parseInput(contactDetailsBody, req.body, res);
      return body && (await handler.handle({ ...body, memberId }, actorOf(res)));
    }),
  );
}
