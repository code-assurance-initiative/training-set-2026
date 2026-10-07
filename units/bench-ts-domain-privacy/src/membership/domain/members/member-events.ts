import { DomainEvent } from '../../../shared-kernel/domain-event.js';
import type { ConsentPurpose } from './consent.js';
import type { MemberId } from './member-id.js';

export class MemberRegistered extends DomainEvent {
  readonly type = 'member-registered';

  constructor(
    readonly memberId: MemberId,
    occurredAt: Date,
  ) {
    super(occurredAt);
  }
}

export class ContactDetailsChanged extends DomainEvent {
  readonly type = 'contact-details-changed';

  constructor(
    readonly memberId: MemberId,
    readonly changedFields: readonly string[],
    occurredAt: Date,
  ) {
    super(occurredAt);
  }
}

export class ConsentRecorded extends DomainEvent {
  readonly type = 'consent-recorded';

  constructor(
    readonly memberId: MemberId,
    readonly purpose: ConsentPurpose,
    readonly granted: boolean,
    occurredAt: Date,
  ) {
    super(occurredAt);
  }
}

export class MembershipCancelled extends DomainEvent {
  readonly type = 'membership-cancelled';

  constructor(
    readonly memberId: MemberId,
    occurredAt: Date,
  ) {
    super(occurredAt);
  }
}

export class MemberErased extends DomainEvent {
  readonly type = 'member-erased';

  constructor(
    readonly memberId: MemberId,
    occurredAt: Date,
  ) {
    super(occurredAt);
  }
}
