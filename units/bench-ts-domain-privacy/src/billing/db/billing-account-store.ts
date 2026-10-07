import type { Knex } from 'knex';
import { dayFrom, isoDay } from '../../platform/clock.js';
import { MemberId } from '../../membership/domain/members/member-id.js';
import { BillingAccount } from '../domain/accounts/billing-account.js';
import { BillingAccountId } from '../domain/accounts/billing-account-id.js';
import type { BillingAccountRepository } from '../domain/accounts/billing-account-repository.js';
import { HolderBirthDate } from '../domain/accounts/concession.js';

interface BillingAccountRow {
  id: string;
  member_id: string;
  holder_name: string | null;
  holder_email: string | null;
  date_of_birth: Date | string | null;
  opened_at: Date | string;
  anonymised_at: Date | string | null;
}

export class KnexBillingAccountRepository implements BillingAccountRepository {
  constructor(private readonly db: Knex) {}

  async findByMember(member: MemberId): Promise<BillingAccount | undefined> {
    const row = await this.db<BillingAccountRow>('billing_accounts')
      .where({ member_id: member.value })
      .first();
    return row && toAccount(row);
  }

  async listOpen(): Promise<BillingAccount[]> {
    const rows = await this.db<BillingAccountRow>('billing_accounts')
      .whereNull('anonymised_at')
      .orderBy('opened_at');
    return rows.map(toAccount);
  }

  async save(account: BillingAccount): Promise<void> {
    const { holder } = account;
    const row = {
      member_id: account.memberId.value,
      holder_name: holder?.name ?? null,
      holder_email: holder?.email ?? null,
      date_of_birth: holder ? holder.birthDate.value : null,
      opened_at: account.openedAt,
      anonymised_at: account.anonymisedAt ?? null,
    };
    const updated = await this.db('billing_accounts').where({ id: account.id.value }).update(row);
    if (updated === 0) {
      await this.db('billing_accounts').insert({ id: account.id.value, ...row });
    }
  }
}

function toAccount(row: BillingAccountRow): BillingAccount {
  const holder =
    row.holder_name === null || row.holder_email === null || row.date_of_birth === null
      ? undefined
      : {
          name: row.holder_name,
          email: row.holder_email,
          birthDate: HolderBirthDate.parse(isoDay(dayFrom(row.date_of_birth))),
        };
  return new BillingAccount(
    BillingAccountId.of(row.id),
    MemberId.of(row.member_id),
    holder,
    new Date(row.opened_at),
    row.anonymised_at === null ? undefined : new Date(row.anonymised_at),
  );
}
