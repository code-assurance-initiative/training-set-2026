import { describe, expect, it } from 'vitest';
import { cutoffBefore, InvalidLeadTimeError, isLeadTime } from '../../src/scheduling/cutoff.js';

const departure = new Date('2026-11-02T07:00:00Z');

describe('cut-off', () => {
  it.each([
    ['90m', '2026-11-02T05:30:00.000Z'],
    ['2h', '2026-11-02T05:00:00.000Z'],
    ['1.5h', '2026-11-02T05:30:00.000Z'],
    ['45 mins', '2026-11-02T06:15:00.000Z'],
  ])('closes %s before departure', (leadTime, expected) => {
    expect(cutoffBefore(departure, leadTime).toISOString()).toBe(expected);
  });

  it.each(['', '90', 'two hours', '3d', '-1h'])('refuses the lead time %j', (leadTime) => {
    expect(isLeadTime(leadTime)).toBe(false);
    expect(() => cutoffBefore(departure, leadTime)).toThrow(InvalidLeadTimeError);
  });
});
