import type { MemberId } from '../../../membership/domain/members/member-id.js';
import type { BillingAccount } from './billing-account.js';

export interface BillingAccountRepository {
  findByMember(member: MemberId): Promise<BillingAccount | undefined>;
  /** Accounts that are not anonymised. */
  listOpen(): Promise<BillingAccount[]>;
  save(account: BillingAccount): Promise<void>;
}
