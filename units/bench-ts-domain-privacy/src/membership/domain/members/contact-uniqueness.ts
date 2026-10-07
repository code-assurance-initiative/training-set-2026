import type { Knex } from 'knex';
import type { EmailAddress } from './email-address.js';
import type { MemberId } from './member-id.js';

/** Decides whether an e-mail address may be used by a member: one address, one member. */
export class ContactUniqueness {
  constructor(private readonly db: Knex) {}

  async isAvailableFor(email: EmailAddress, member: MemberId): Promise<boolean> {
    const holder = await this.holderOf(email);
    return holder === undefined || holder === member.value;
  }

  private async holderOf(email: EmailAddress): Promise<string | undefined> {
    const row = await this.db<{ id: string; email: string }>('members')
      .select('id')
      .where({ email: email.value })
      .first();
    return row?.id;
  }
}
