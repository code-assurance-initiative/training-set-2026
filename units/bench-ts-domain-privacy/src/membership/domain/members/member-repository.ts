import { DomainRuleViolation } from '../../../shared-kernel/result.js';
import type { EmailAddress } from './email-address.js';
import type { Member } from './member.js';
import type { MemberId } from './member-id.js';

export interface MemberRepository {
  get(id: MemberId): Promise<Member | undefined>;
  findByEmail(email: EmailAddress): Promise<Member | undefined>;
  /** Members whose paid period ended before `day` and who have not been erased. */
  listLapsedBefore(day: Date): Promise<Member[]>;
  save(member: Member, actor: string): Promise<void>;
}

/** The member with `id`; a member that does not exist is a not-found rule violation. */
export async function existingMember(members: MemberRepository, id: MemberId): Promise<Member> {
  const member = await members.get(id);
  if (!member) {
    throw new DomainRuleViolation('not-found', 'No such member.');
  }
  return member;
}
