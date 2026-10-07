import { describe, expect, it } from 'vitest';
import { CityMatcher } from '../../src/addresses/city-matcher.js';

const matcher = new CityMatcher(['Aarhus C', 'Aarhus N', 'Viby J', 'Højbjerg', 'Brabrand']);

describe('city matcher', () => {
  it.each([
    ['Aarhus C', 'Aarhus C'],
    ['aarhus c', 'Aarhus C'],
    ['AARHUS-N', 'Aarhus N'],
    ['Hojbjerg', 'Højbjerg'],
    ['Hoejbjerg', 'Højbjerg'],
    ['Viby  J.', 'Viby J'],
    ['Braband', 'Brabrand'],
  ])('matches %s to %s', (typed, city) => {
    expect(matcher.match(typed)?.city).toBe(city);
  });

  it('rates an exact match 1', () => {
    expect(matcher.match('Brabrand')).toEqual({ city: 'Brabrand', rating: 1 });
  });

  it.each(['Odense C', 'Skagen', '', '  --  '])('does not match %j', (typed) => {
    expect(matcher.match(typed)).toBeUndefined();
  });

  it('needs a service area', () => {
    expect(() => new CityMatcher([])).toThrow(/at least one city/);
  });
});
