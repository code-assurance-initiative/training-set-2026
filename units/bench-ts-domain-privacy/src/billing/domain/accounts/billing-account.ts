import { AggregateRoot } from '../../../shared-kernel/aggregate-root.js';
import { DomainRuleViolation } from '../../../shared-kernel/result.js';
import type { MemberId } from '../../../membership/domain/members/member-id.js';
import type { BillingAccountId } from './billing-account-id.js';
import type { Concession, HolderBirthDate } from './concession.js';

const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** The person who pays, as Billing needs them on an invoice. */
export interface AccountHolder {
  readonly name: string;
  readonly email: string;
  readonly birthDate: HolderBirthDate;
}

/** Who pays for a membership, and the data Billing needs to invoice them. */
export class BillingAccount extends AggregateRoot<BillingAccountId> {
  readonly memberId: MemberId;
  readonly openedAt: Date;
  #holder: AccountHolder | undefined;
  #anonymisedAt: Date | undefined;

  constructor(
    id: BillingAccountId,
    memberId: MemberId,
    holder: AccountHolder | undefined,
    openedAt: Date,
    anonymisedAt?: Date,
  ) {
    super(id);
    if (holder === undefined && anonymisedAt === undefined) {
      throw new DomainRuleViolation('invalid', 'A billing account needs its holder.');
    }
    if (holder?.name.trim().length === 0) {
      throw new DomainRuleViolation('invalid', 'A billing account needs the holder name.');
    }
    if (holder !== undefined && !emailPattern.test(holder.email)) {
      throw new DomainRuleViolation('invalid', 'A billing account needs a valid e-mail address.');
    }
    this.memberId = memberId;
    this.openedAt = openedAt;
    this.#holder = holder && { ...holder, name: holder.name.trim() };
    this.#anonymisedAt = anonymisedAt;
  }

  get holder(): AccountHolder | undefined {
    return this.#holder;
  }

  get anonymisedAt(): Date | undefined {
    return this.#anonymisedAt;
  }

  /** The concession on the fee for `on`; an anonymised account no longer qualifies for any. */
  concessionOn(on: Date): Concession {
    return this.#holder?.birthDate.concessionOn(on) ?? 'none';
  }

  /** Removes the holder's personal data; the invoices stay for the bookkeeping period. */
  anonymise(at: Date): void {
    if (this.#anonymisedAt) {
      return;
    }
    this.#holder = undefined;
    this.#anonymisedAt = at;
  }
}
