import { z } from 'zod';

/**
 * Integration messages Membership publishes through the outbox (ADR 0005). This module is the whole
 * contract other contexts may depend on: plain values, ids as strings, no Membership domain types.
 */
export interface MemberMessage {
  readonly memberId: string;
}

export const memberRegisteredMessage = 'membership.member-registered';

export interface MemberRegisteredPayload extends MemberMessage {
  readonly firstName: string;
  readonly lastName: string;
  readonly email: string;
  /** `YYYY-MM-DD`; Billing needs it for age-based concessions (data inventory). */
  readonly dateOfBirth: string;
}

export const memberErasedMessage = 'membership.member-erased';

export type MemberErasedPayload = MemberMessage;

const memberId = z.uuid();

export const memberRegisteredSchema: z.ZodType<MemberRegisteredPayload> = z.object({
  memberId,
  firstName: z.string().min(1),
  lastName: z.string().min(1),
  email: z.email(),
  dateOfBirth: z.iso.date(),
});

export const memberErasedSchema: z.ZodType<MemberErasedPayload> = z.object({ memberId });
