import { describe, expect, it } from 'vitest';
import { availabilityText, formatDate, formatLabel } from '../src/catalogue/format';

describe('catalogue formatting', () => {
  it('labels formats for people', () => {
    expect(formatLabel('dvd')).toBe('DVD');
    expect(formatLabel('audiobook')).toBe('Audiobook');
  });

  it('describes availability', () => {
    expect(availabilityText({ availableCopies: 2, totalCopies: 3 })).toBe('2 of 3 available');
    expect(availabilityText({ availableCopies: 0, totalCopies: 2 })).toBe('All 2 copies on loan');
  });

  it('formats ISO dates without shifting the day', () => {
    expect(formatDate('2026-10-07')).toBe('7 Oct 2026');
  });
});
