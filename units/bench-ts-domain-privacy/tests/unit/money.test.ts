import { describe, expect, it } from 'vitest';
import { Money } from '../../src/shared-kernel/money.js';

describe('Money', () => {
  it('adds amounts of one currency', () => {
    expect(Money.of(3_900, 'EUR').add(Money.of(-780, 'EUR')).toJSON()).toEqual({
      amount: 3_120,
      currency: 'EUR',
    });
  });

  it('refuses to combine currencies', () => {
    expect(() => Money.of(1, 'EUR').add(Money.of(1, 'DKK'))).toThrow(RangeError);
  });

  it('takes a percentage, rounded to a whole minor unit', () => {
    expect(Money.of(3_900, 'EUR').percent(-30).minorUnits).toBe(-1_170);
    expect(Money.of(5, 'EUR').percent(50).minorUnits).toBe(3);
  });

  it('accepts only whole minor units and ISO currency codes', () => {
    expect(() => Money.of(1.5, 'EUR')).toThrow(RangeError);
    expect(() => Money.of(1, 'euro')).toThrow(RangeError);
    expect(Money.zero('EUR').isNegative()).toBe(false);
  });
});
