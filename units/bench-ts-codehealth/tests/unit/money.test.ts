import { describe, expect, it } from 'vitest';
import { Money } from '../../src/domain/value-objects/money.js';

describe('Money', () => {
  it('adds amounts of one currency', () => {
    expect(Money.of(1_250, 'DKK').plus(Money.of(750, 'DKK')).toString()).toBe('20.00 DKK');
  });

  it('refuses to mix currencies', () => {
    expect(() => Money.of(100, 'DKK').plus(Money.of(100, 'EUR'))).toThrow(RangeError);
  });

  it('rounds multiplication to whole minor units', () => {
    expect(Money.of(999, 'DKK').times(0.015).minorUnits).toBe(15);
  });

  it('compares amounts', () => {
    expect(Money.of(2, 'DKK').isGreaterThan(Money.of(1, 'DKK'))).toBe(true);
    expect(Money.of(2, 'DKK').equals(Money.of(2, 'DKK'))).toBe(true);
    expect(Money.zero('SEK').minorUnits).toBe(0);
  });

  it('rejects fractional minor units and bad currency codes', () => {
    expect(() => Money.of(1.5, 'DKK')).toThrow(RangeError);
    expect(() => Money.of(1, 'dkk')).toThrow(RangeError);
  });

  it('rejects a value that is not a number at run time', () => {
    // @ts-expect-error -- deliberately ill-typed: proves the runtime guard rejects JSON strings
    expect(() => Money.of('100', 'DKK')).toThrow(RangeError);
  });
});
