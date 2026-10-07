import type { Knex } from 'knex';
import type { AuditLog } from '../../platform/audit-log.js';
import type { FieldCipher } from '../../platform/field-cipher.js';
import type { Outbox } from '../../platform/outbox.js';
import { dayFrom, isoDay } from '../../platform/clock.js';
import type { DomainEvent } from '../../shared-kernel/domain-event.js';
import { Consent, type ConsentPurpose } from '../domain/members/consent.js';
import { DateOfBirth } from '../domain/members/date-of-birth.js';
import { EmailAddress } from '../domain/members/email-address.js';
import { Member, type MemberStatus, type PersonalData } from '../domain/members/member.js';
import {
  ConsentRecorded,
  ContactDetailsChanged,
  MemberErased,
  MemberRegistered,
  MembershipCancelled,
} from '../domain/members/member-events.js';
import { MemberId } from '../domain/members/member-id.js';
import type { MemberRepository } from '../domain/members/member-repository.js';
import { PersonName } from '../domain/members/person-name.js';
import { PhoneNumber } from '../domain/members/phone-number.js';
import {
  memberErasedMessage,
  memberRegisteredMessage,
  type MemberErasedPayload,
  type MemberRegisteredPayload,
} from '../contracts/integration-events.js';

interface MemberRow {
  id: string;
  first_name: string | null;
  last_name: string | null;
  email: string | null;
  phone_encrypted: string | null;
  date_of_birth_encrypted: string | null;
  status: MemberStatus;
  membership_ends_on: Date | string;
  registered_at: Date | string;
  cancelled_at: Date | string | null;
  erased_at: Date | string | null;
}

interface ConsentRow {
  member_id: string;
  purpose: ConsentPurpose;
  granted: boolean;
  recorded_at: Date | string;
}

const personalFields = ['firstName', 'lastName', 'email', 'phone', 'dateOfBirth'];

/**
 * Members in PostgreSQL. Phone and date of birth are encrypted before they are written (ADR 0004);
 * every change is recorded in the audit log, and integration messages are written to the outbox, in
 * the same transaction as the change.
 */
export class KnexMemberRepository implements MemberRepository {
  constructor(
    private readonly db: Knex,
    private readonly cipher: FieldCipher,
    private readonly audit: AuditLog,
    private readonly outbox: Outbox,
  ) {}

  async get(id: MemberId): Promise<Member | undefined> {
    const row = await this.db<MemberRow>('members').where({ id: id.value }).first();
    return row ? this.toMember(row) : undefined;
  }

  async findByEmail(email: EmailAddress): Promise<Member | undefined> {
    const row = await this.db<MemberRow>('members').where({ email: email.value }).first();
    return row ? this.toMember(row) : undefined;
  }

  async listLapsedBefore(day: Date): Promise<Member[]> {
    const rows = await this.db<MemberRow>('members')
      .where('membership_ends_on', '<', isoDay(day))
      .whereNot({ status: 'erased' });
    return Promise.all(rows.map((row) => this.toMember(row)));
  }

  async save(member: Member, actor: string): Promise<void> {
    const state = member.toState();
    const row = {
      ...this.personalColumns(state.personal),
      status: state.status,
      membership_ends_on: isoDay(state.membershipEndsOn),
      registered_at: state.registeredAt,
      cancelled_at: state.cancelledAt ?? null,
      erased_at: state.erasedAt ?? null,
    };
    const updated = await this.db('members').where({ id: state.id.value }).update(row);
    if (updated === 0) {
      await this.db('members').insert({ id: state.id.value, ...row });
    }
    await this.db('member_consents').where({ member_id: state.id.value }).delete();
    if (state.consents.length > 0) {
      await this.db('member_consents').insert(
        state.consents.map((consent) => ({
          member_id: state.id.value,
          purpose: consent.purpose,
          granted: consent.granted,
          recorded_at: consent.recordedAt,
        })),
      );
    }
    for (const event of member.takeEvents()) {
      await this.record(event, state.personal, actor);
    }
  }

  private personalColumns(personal: PersonalData | undefined): Record<string, string | null> {
    return {
      first_name: personal?.name.firstName ?? null,
      last_name: personal?.name.lastName ?? null,
      email: personal?.email.value ?? null,
      phone_encrypted: personal?.phone ? this.cipher.encrypt(personal.phone.value) : null,
      date_of_birth_encrypted: personal
        ? this.cipher.encrypt(personal.dateOfBirth.toString())
        : null,
    };
  }

  private async record(
    event: DomainEvent,
    personal: PersonalData | undefined,
    actor: string,
  ): Promise<void> {
    const memberId = memberOf(event);
    await this.audit.append({
      occurredAt: event.occurredAt,
      actor,
      action: event.type,
      subjectType: 'member',
      subjectId: memberId.value,
      changedFields: changedFieldsOf(event),
    });
    if (event instanceof MemberRegistered && personal) {
      const payload: MemberRegisteredPayload = {
        memberId: memberId.value,
        firstName: personal.name.firstName,
        lastName: personal.name.lastName,
        email: personal.email.value,
        dateOfBirth: personal.dateOfBirth.toString(),
      };
      await this.outbox.add(memberRegisteredMessage, payload, event.occurredAt);
    }
    if (event instanceof MemberErased) {
      const payload: MemberErasedPayload = { memberId: memberId.value };
      await this.outbox.add(memberErasedMessage, payload, event.occurredAt);
    }
  }

  private async toMember(row: MemberRow): Promise<Member> {
    const consents = await this.db<ConsentRow>('member_consents').where({ member_id: row.id });
    return Member.rehydrate({
      id: MemberId.of(row.id),
      personal: this.personalData(row),
      status: row.status,
      membershipEndsOn: dayFrom(row.membership_ends_on),
      registeredAt: new Date(row.registered_at),
      cancelledAt: row.cancelled_at === null ? undefined : new Date(row.cancelled_at),
      erasedAt: row.erased_at === null ? undefined : new Date(row.erased_at),
      consents: consents.map((consent) => {
        const at = new Date(consent.recorded_at);
        return consent.granted
          ? Consent.given(consent.purpose, at)
          : Consent.refused(consent.purpose, at);
      }),
    });
  }

  private personalData(row: MemberRow): PersonalData | undefined {
    if (
      row.first_name === null ||
      row.last_name === null ||
      row.email === null ||
      row.date_of_birth_encrypted === null
    ) {
      return undefined;
    }
    return {
      name: PersonName.of(row.first_name, row.last_name),
      email: EmailAddress.of(row.email),
      phone:
        row.phone_encrypted === null
          ? undefined
          : PhoneNumber.of(this.cipher.decrypt(row.phone_encrypted)),
      dateOfBirth: DateOfBirth.parse(this.cipher.decrypt(row.date_of_birth_encrypted)),
    };
  }
}

function memberOf(event: DomainEvent): MemberId {
  if (
    event instanceof MemberRegistered ||
    event instanceof ContactDetailsChanged ||
    event instanceof ConsentRecorded ||
    event instanceof MembershipCancelled ||
    event instanceof MemberErased
  ) {
    return event.memberId;
  }
  throw new Error(`The member store does not record '${event.type}' events.`);
}

function changedFieldsOf(event: DomainEvent): string[] {
  if (event instanceof ContactDetailsChanged) {
    return [...event.changedFields];
  }
  if (event instanceof ConsentRecorded) {
    return [`consent:${event.purpose}`];
  }
  if (event instanceof MembershipCancelled) {
    return ['status'];
  }
  return personalFields;
}
