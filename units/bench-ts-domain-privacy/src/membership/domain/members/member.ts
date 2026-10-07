import { AggregateRoot } from '../../../shared-kernel/aggregate-root.js';
import { DomainRuleViolation } from '../../../shared-kernel/result.js';
import { Consent, type ConsentPurpose } from './consent.js';
import type { DateOfBirth } from './date-of-birth.js';
import type { EmailAddress } from './email-address.js';
import {
  ConsentRecorded,
  ContactDetailsChanged,
  MemberErased,
  MemberRegistered,
  MembershipCancelled,
} from './member-events.js';
import type { MemberId } from './member-id.js';
import type { PersonName } from './person-name.js';
import type { PhoneNumber } from './phone-number.js';

export type MemberStatus = 'active' | 'cancelled' | 'erased';

export interface PersonalData {
  readonly name: PersonName;
  readonly email: EmailAddress;
  readonly phone: PhoneNumber | undefined;
  readonly dateOfBirth: DateOfBirth;
}

/** Everything the store needs to write a member and read it back. */
export interface MemberState {
  readonly id: MemberId;
  readonly personal: PersonalData | undefined;
  readonly status: MemberStatus;
  readonly membershipEndsOn: Date;
  readonly registeredAt: Date;
  readonly cancelledAt: Date | undefined;
  readonly erasedAt: Date | undefined;
  readonly consents: readonly Consent[];
}

const minimumAge = 16;

export class Member extends AggregateRoot<MemberId> {
  #personal: PersonalData | undefined;
  #status: MemberStatus;
  readonly #membershipEndsOn: Date;
  readonly #registeredAt: Date;
  #cancelledAt: Date | undefined;
  #erasedAt: Date | undefined;
  readonly #consents: Map<ConsentPurpose, Consent>;

  /** Registers a new member who has paid until `membershipEndsOn` (a UTC calendar day). */
  static register(
    id: MemberId,
    personal: PersonalData,
    membershipEndsOn: Date,
    registeredAt: Date,
  ): Member {
    if (personal.dateOfBirth.ageOn(registeredAt) < minimumAge) {
      throw new DomainRuleViolation('invalid', `Members must be at least ${minimumAge} years old.`);
    }
    if (membershipEndsOn.getTime() < registeredAt.getTime() - 86_400_000) {
      throw new DomainRuleViolation('invalid', 'A membership cannot end before it starts.');
    }
    const member = new Member({
      id,
      personal,
      status: 'active',
      membershipEndsOn,
      registeredAt,
      cancelledAt: undefined,
      erasedAt: undefined,
      consents: [],
    });
    member.raise(new MemberRegistered(id, registeredAt));
    return member;
  }

  static rehydrate(state: MemberState): Member {
    return new Member(state);
  }

  private constructor(state: MemberState) {
    super(state.id);
    this.#personal = state.personal;
    this.#status = state.status;
    this.#membershipEndsOn = state.membershipEndsOn;
    this.#registeredAt = state.registeredAt;
    this.#cancelledAt = state.cancelledAt;
    this.#erasedAt = state.erasedAt;
    this.#consents = new Map(state.consents.map((consent) => [consent.purpose, consent]));
  }

  get status(): MemberStatus {
    return this.#status;
  }

  /** The last UTC calendar day the member has paid for. */
  get membershipEndsOn(): Date {
    return this.#membershipEndsOn;
  }

  get name(): PersonName {
    return this.personal().name;
  }

  get email(): EmailAddress {
    return this.personal().email;
  }

  get phone(): PhoneNumber | undefined {
    return this.personal().phone;
  }

  get dateOfBirth(): DateOfBirth {
    return this.personal().dateOfBirth;
  }

  get consents(): readonly Consent[] {
    return [...this.#consents.values()];
  }

  hasConsented(purpose: ConsentPurpose): boolean {
    return this.#consents.get(purpose)?.granted ?? false;
  }

  changeContactDetails(email: EmailAddress, phone: PhoneNumber | undefined, at: Date): void {
    const current = this.personal();
    const changed = [
      ...(current.email.equals(email) ? [] : ['email']),
      ...(samePhone(current.phone, phone) ? [] : ['phone']),
    ];
    if (changed.length === 0) {
      return;
    }
    this.#personal = { ...current, email, phone };
    this.raise(new ContactDetailsChanged(this.id, changed, at));
  }

  recordConsent(purpose: ConsentPurpose, granted: boolean, at: Date): void {
    this.personal();
    this.#consents.set(
      purpose,
      granted ? Consent.given(purpose, at) : Consent.refused(purpose, at),
    );
    this.raise(new ConsentRecorded(this.id, purpose, granted, at));
  }

  cancel(at: Date): void {
    if (this.#status !== 'active') {
      throw new DomainRuleViolation('conflict', 'Only an active membership can be cancelled.');
    }
    this.#status = 'cancelled';
    this.#cancelledAt = at;
    this.raise(new MembershipCancelled(this.id, at));
  }

  /** Removes the member's personal data for good; the membership record keeps only its dates. */
  erase(at: Date): void {
    if (this.#status === 'erased') {
      throw new DomainRuleViolation('conflict', 'The member has already been erased.');
    }
    this.#personal = undefined;
    this.#consents.clear();
    this.#status = 'erased';
    this.#erasedAt = at;
    this.raise(new MemberErased(this.id, at));
  }

  toState(): MemberState {
    return {
      id: this.id,
      personal: this.#personal,
      status: this.#status,
      membershipEndsOn: this.#membershipEndsOn,
      registeredAt: this.#registeredAt,
      cancelledAt: this.#cancelledAt,
      erasedAt: this.#erasedAt,
      consents: this.consents,
    };
  }

  private personal(): PersonalData {
    if (!this.#personal) {
      throw new DomainRuleViolation('not-found', "The member's personal data has been erased.");
    }
    return this.#personal;
  }
}

function samePhone(left: PhoneNumber | undefined, right: PhoneNumber | undefined): boolean {
  return left === undefined ? right === undefined : left.equals(right);
}
