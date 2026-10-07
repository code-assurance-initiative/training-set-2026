import { Router } from 'express';
import { z } from 'zod';
import type { Clock } from '../../../platform/clock.js';
import { dayFrom } from '../../../platform/clock.js';
import { sendResult } from '../../../platform/http/problem.js';
import { actorOf, requireScope, Scopes } from '../../../platform/http/scopes.js';
import { parseInput } from '../../../platform/http/validation.js';
import { fail, ok, type Result } from '../../../shared-kernel/result.js';
import type { ContactUniqueness } from '../../domain/members/contact-uniqueness.js';
import { DateOfBirth } from '../../domain/members/date-of-birth.js';
import { EmailAddress } from '../../domain/members/email-address.js';
import { Member } from '../../domain/members/member.js';
import { MemberId } from '../../domain/members/member-id.js';
import { PersonName } from '../../domain/members/person-name.js';
import { PhoneNumber } from '../../domain/members/phone-number.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

export const registerMemberBody = z.object({
  firstName: z.string().min(1).max(100),
  lastName: z.string().min(1).max(100),
  email: z.email(),
  phone: z.string().min(1).max(32).optional(),
  dateOfBirth: z.iso.date(),
  membershipEndsOn: z.iso.date(),
});

export type RegisterMember = z.infer<typeof registerMemberBody>;

export class RegisterMemberHandler {
  constructor(
    private readonly transactions: MembershipTransactions,
    private readonly uniqueness: ContactUniqueness,
    private readonly clock: Clock,
  ) {}

  async handle(command: RegisterMember, actor: string): Promise<Result<{ memberId: string }>> {
    const id = MemberId.create();
    const email = EmailAddress.of(command.email);
    if (!(await this.uniqueness.isAvailableFor(email, id))) {
      return fail('conflict', 'A member with this e-mail address is already registered.');
    }
    const member = Member.register(
      id,
      {
        name: PersonName.of(command.firstName, command.lastName),
        email,
        phone: command.phone === undefined ? undefined : PhoneNumber.of(command.phone),
        dateOfBirth: DateOfBirth.parse(command.dateOfBirth),
      },
      dayFrom(command.membershipEndsOn),
      this.clock.now(),
    );
    await this.transactions.run(({ members }) => members.save(member, actor));
    return ok({ memberId: id.value });
  }
}

export function registerMemberRoutes(handler: RegisterMemberHandler): Router {
  return Router().post('/members', requireScope(Scopes.membersWrite), async (req, res) => {
    const command = parseInput(registerMemberBody, req.body, res);
    if (command) {
      const result = await handler.handle(command, actorOf(res));
      sendResult(res, result, (created) => `/api/members/${created.memberId}`);
    }
  });
}
