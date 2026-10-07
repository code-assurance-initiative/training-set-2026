import { Router } from 'express';
import { requireScope, Scopes } from '../../../platform/http/scopes.js';
import { idRoute } from '../../../platform/http/validation.js';
import { fail, ok, type Result } from '../../../shared-kernel/result.js';
import { MemberId } from '../../domain/members/member-id.js';
import type { MembershipTransactions } from '../../domain/membership-transactions.js';

export interface MemberDataExport {
  readonly memberId: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  readonly phone: string | null;
  readonly dateOfBirth: string;
  readonly status: string;
  readonly membershipEndsOn: string;
  readonly consents: readonly { purpose: string; granted: boolean; recordedAt: string }[];
  readonly bookings: readonly { sessionId: string; status: string; bookedAt: string }[];
}

/** Everything Membership holds about a member, in a portable form (right of access and portability). */
export class ExportMemberDataHandler {
  constructor(private readonly transactions: MembershipTransactions) {}

  handle(memberId: string): Promise<Result<MemberDataExport>> {
    return this.transactions.run(async ({ members, bookings }) => {
      const member = await members.get(MemberId.of(memberId));
      if (!member || member.status === 'erased') {
        return fail('not-found', 'No personal data is held for this member.');
      }
      const history = await bookings.listForMember(member.id);
      return ok({
        memberId: member.id.value,
        firstName: member.name.firstName,
        lastName: member.name.lastName,
        email: member.email.value,
        phone: member.phone?.value ?? null,
        dateOfBirth: member.dateOfBirth.toString(),
        status: member.status,
        membershipEndsOn: member.membershipEndsOn.toISOString().slice(0, 10),
        consents: member.consents.map((consent) => ({
          purpose: consent.purpose,
          granted: consent.granted,
          recordedAt: consent.recordedAt.toISOString(),
        })),
        bookings: history.map((booking) => ({
          sessionId: booking.sessionId.value,
          status: booking.status,
          bookedAt: booking.bookedAt.toISOString(),
        })),
      });
    });
  }
}

export function exportMemberDataRoutes(handler: ExportMemberDataHandler): Router {
  return Router().get(
    '/members/:memberId/personal-data',
    requireScope(Scopes.privacy),
    idRoute('memberId', (memberId) => handler.handle(memberId)),
  );
}
