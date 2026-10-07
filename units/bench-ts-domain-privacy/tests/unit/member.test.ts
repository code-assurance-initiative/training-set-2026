import { describe, expect, it } from 'vitest';
import { DateOfBirth } from '../../src/membership/domain/members/date-of-birth.js';
import { EmailAddress } from '../../src/membership/domain/members/email-address.js';
import { Member, type PersonalData } from '../../src/membership/domain/members/member.js';
import { MemberId } from '../../src/membership/domain/members/member-id.js';
import { PersonName } from '../../src/membership/domain/members/person-name.js';
import { PhoneNumber } from '../../src/membership/domain/members/phone-number.js';

const now = new Date('2026-10-07T08:00:00Z');
const personal: PersonalData = {
  name: PersonName.of('Ada', 'Lindqvist'),
  email: EmailAddress.of('ada.lindqvist@example.net'),
  phone: PhoneNumber.of('+4520123456'),
  dateOfBirth: DateOfBirth.parse('1990-04-12'),
};

function register(): Member {
  const member = Member.register(MemberId.create(), personal, new Date('2026-12-31'), now);
  member.takeEvents();
  return member;
}

describe('Member', () => {
  it('records registration as an event', () => {
    const member = Member.register(MemberId.create(), personal, new Date('2026-12-31'), now);

    expect(member.takeEvents().map((event) => event.type)).toEqual(['member-registered']);
    expect(member.takeEvents()).toEqual([]);
  });

  it('refuses a membership that ends before it starts', () => {
    expect(() => Member.register(MemberId.create(), personal, new Date('2026-10-01'), now)).toThrow(
      'A membership cannot end before it starts.',
    );
  });

  it('changes contact details only when they differ, naming the changed fields', () => {
    const member = register();

    member.changeContactDetails(personal.email, personal.phone, now);
    expect(member.takeEvents()).toEqual([]);

    member.changeContactDetails(personal.email, undefined, now);
    expect(member.takeEvents()).toMatchObject([{ changedFields: ['phone'] }]);
    expect(member.phone).toBeUndefined();
  });

  it('answers consent per purpose, defaulting to no', () => {
    const member = register();

    expect(member.hasConsented('sms-reminders')).toBe(false);
    member.recordConsent('sms-reminders', true, now);
    expect(member.hasConsented('sms-reminders')).toBe(true);
    expect(member.hasConsented('newsletter')).toBe(false);
    member.recordConsent('sms-reminders', false, now);
    expect(member.hasConsented('sms-reminders')).toBe(false);
  });

  it('cancels only an active membership', () => {
    const member = register();

    member.cancel(now);

    expect(member.status).toBe('cancelled');
    expect(() => {
      member.cancel(now);
    }).toThrow('Only an active membership can be cancelled.');
  });

  it('erases personal data and consents, once', () => {
    const member = register();
    member.recordConsent('newsletter', true, now);

    member.erase(now);

    expect(member.toState()).toMatchObject({ personal: undefined, consents: [], status: 'erased' });
    expect(() => member.email).toThrow("The member's personal data has been erased.");
    expect(() => {
      member.recordConsent('newsletter', true, now);
    }).toThrow();
    expect(() => {
      member.erase(now);
    }).toThrow('The member has already been erased.');
  });
});
