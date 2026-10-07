import { describe, expect, it } from 'vitest';
import { DateOfBirth } from '../../src/membership/domain/members/date-of-birth.js';
import { EmailAddress } from '../../src/membership/domain/members/email-address.js';
import { MemberId } from '../../src/membership/domain/members/member-id.js';
import { PersonName } from '../../src/membership/domain/members/person-name.js';
import { PhoneNumber } from '../../src/membership/domain/members/phone-number.js';
import { ClassSessionId } from '../../src/membership/domain/classes/class-session-id.js';
import { Consent } from '../../src/membership/domain/members/consent.js';

describe('membership value objects', () => {
  it('normalises e-mail addresses and exposes only the domain separately', () => {
    const email = EmailAddress.of('  Ada.Lindqvist@Example.NET ');

    expect(email.value).toBe('ada.lindqvist@example.net');
    expect(email.domain).toBe('example.net');
    expect(email.equals(EmailAddress.of('ada.lindqvist@example.net'))).toBe(true);
    expect(() => EmailAddress.of('not an address')).toThrow(RangeError);
  });

  it('accepts phone numbers in international form only', () => {
    expect(PhoneNumber.of('+45 20 12-34 56').value).toBe('+4520123456');
    expect(() => PhoneNumber.of('20123456')).toThrow(RangeError);
  });

  it('requires a first and a last name', () => {
    const name = PersonName.of(' Ada ', 'Lindqvist');

    expect(name.fullName).toBe('Ada Lindqvist');
    expect(() => PersonName.of('Ada', ' ')).toThrow(RangeError);
    expect(() => PersonName.of('x'.repeat(101), 'Lindqvist')).toThrow(RangeError);
  });

  it('computes the age on a day from a date of birth', () => {
    const born = DateOfBirth.parse('2010-10-07');

    expect(born.ageOn(new Date('2026-10-06T23:59:00Z'))).toBe(15);
    expect(born.ageOn(new Date('2026-10-07T00:00:00Z'))).toBe(16);
    expect(born.toString()).toBe('2010-10-07');
  });

  it('rejects impossible dates of birth', () => {
    for (const value of ['2010-02-30', '1899-12-31', '07-10-2010']) {
      expect(() => DateOfBirth.parse(value), value).toThrow(RangeError);
    }
  });

  it('compares ids by value and type', () => {
    const value = '9b0c5e2d-4f1a-4c3b-8d7e-6a5f4e3d2c1b';

    expect(MemberId.of(value).equals(MemberId.of(value.toUpperCase()))).toBe(true);
    expect(MemberId.of(value).equals(ClassSessionId.of(value))).toBe(false);
    expect(MemberId.create().equals(MemberId.create())).toBe(false);
    expect(() => MemberId.of('42')).toThrow(RangeError);
  });
});

describe('consent', () => {
  it('compares answers by purpose, answer and time', () => {
    const at = new Date('2026-10-07T08:00:00Z');

    expect(Consent.given('newsletter', at).equals(Consent.given('newsletter', new Date(at)))).toBe(
      true,
    );
    expect(Consent.given('newsletter', at).equals(Consent.refused('newsletter', at))).toBe(false);
    expect(Consent.given('newsletter', at).equals(undefined)).toBe(false);
  });
});
