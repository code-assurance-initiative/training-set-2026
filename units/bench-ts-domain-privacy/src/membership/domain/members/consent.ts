import { ValueObject } from '../../../shared-kernel/value-object.js';

/**
 * Purposes that need the member's consent (docs/privacy/data-inventory.md). Reminders of booked classes
 * by e-mail are part of the membership contract and need none.
 */
export const consentPurposes = ['sms-reminders', 'newsletter'] as const;

export type ConsentPurpose = (typeof consentPurposes)[number];

/** The member's latest answer for one purpose. */
export class Consent extends ValueObject<{
  purpose: ConsentPurpose;
  granted: boolean;
  recordedAt: Date;
}> {
  private constructor(purpose: ConsentPurpose, granted: boolean, recordedAt: Date) {
    super({ purpose, granted, recordedAt });
  }

  static given(purpose: ConsentPurpose, at: Date): Consent {
    return new Consent(purpose, true, at);
  }

  static refused(purpose: ConsentPurpose, at: Date): Consent {
    return new Consent(purpose, false, at);
  }

  get purpose(): ConsentPurpose {
    return this.props.purpose;
  }

  get granted(): boolean {
    return this.props.granted;
  }

  get recordedAt(): Date {
    return this.props.recordedAt;
  }
}
