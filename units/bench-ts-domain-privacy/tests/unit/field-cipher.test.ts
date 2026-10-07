import { randomBytes } from 'node:crypto';
import { describe, expect, it } from 'vitest';
import { createFieldCipher } from '../../src/platform/field-cipher.js';
import { createPseudonymiser } from '../../src/platform/pseudonym.js';

describe('field cipher', () => {
  const cipher = createFieldCipher(randomBytes(32));

  it('round-trips a value and never writes the same ciphertext twice', () => {
    const first = cipher.encrypt('+4520123456');
    const second = cipher.encrypt('+4520123456');

    expect(first).not.toBe(second);
    expect(first).not.toContain('4520123456');
    expect(cipher.decrypt(first)).toBe('+4520123456');
  });

  it('detects tampering and foreign values', () => {
    const stored = cipher.encrypt('1990-04-12');
    const tampered = stored.slice(0, -2) + (stored.endsWith('A') ? 'BB' : 'AA');

    expect(() => cipher.decrypt(tampered)).toThrow();
    expect(() => cipher.decrypt('1990-04-12')).toThrow('Not a value written by this field cipher.');
    expect(() => createFieldCipher(randomBytes(32)).decrypt(stored)).toThrow();
  });

  it('refuses a truncated authentication tag', () => {
    const [prefix, iv, tag, ciphertext] = cipher.encrypt('1990-04-12').split(':');
    const shortTag = Buffer.from(tag ?? '', 'base64url')
      .subarray(0, 4)
      .toString('base64url');

    expect(() => cipher.decrypt([prefix, iv, shortTag, ciphertext].join(':'))).toThrow();
  });

  it('requires a 256-bit key', () => {
    expect(() => createFieldCipher(randomBytes(16))).toThrow(RangeError);
  });
});

describe('pseudonymiser', () => {
  it('gives one identifier one stable token per key', () => {
    const key = randomBytes(32);
    const pseudonymise = createPseudonymiser(key);

    expect(pseudonymise('member-1')).toBe(createPseudonymiser(key)('member-1'));
    expect(pseudonymise('member-1')).not.toBe(pseudonymise('member-2'));
    expect(createPseudonymiser(randomBytes(32))('member-1')).not.toBe(pseudonymise('member-1'));
    expect(() => createPseudonymiser(randomBytes(8))).toThrow(RangeError);
  });
});
