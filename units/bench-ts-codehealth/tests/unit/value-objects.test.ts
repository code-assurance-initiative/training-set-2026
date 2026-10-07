import { describe, expect, it } from 'vitest';
import { Address } from '../../src/domain/value-objects/address.js';
import { Dimensions } from '../../src/domain/value-objects/dimensions.js';
import { chargeableWeight } from '../../src/domain/value-objects/parcel.js';
import { TrackingNumber } from '../../src/domain/value-objects/tracking-number.js';
import { Weight } from '../../src/domain/value-objects/weight.js';
import { parcel } from '../support/builders.js';

describe('Weight', () => {
  it('bills per started kilogram', () => {
    expect(Weight.grams(2_001).billableKilograms).toBe(3);
    expect(Weight.kilograms(1.5).grams).toBe(1_500);
  });

  it('rejects zero and fractional grams', () => {
    expect(() => Weight.grams(0)).toThrow(RangeError);
    expect(() => Weight.grams(1.5)).toThrow(RangeError);
  });
});

describe('Dimensions', () => {
  it('computes length plus girth from the longest side', () => {
    expect(Dimensions.of(20, 60, 10).lengthPlusGirthCm).toBe(120);
    expect(Dimensions.of(20, 60, 10).longestSideCm).toBe(60);
  });

  it('rejects non-positive sides', () => {
    expect(() => Dimensions.of(10, 0, 10)).toThrow(RangeError);
  });

  it('bills the volumetric weight of a light, bulky parcel', () => {
    expect(chargeableWeight(parcel(1_000, [60, 50, 40]), 5_000)).toBe(24);
    expect(chargeableWeight(parcel(4_200, [20, 10, 10]), 5_000)).toBe(5);
  });
});

describe('Address', () => {
  it('trims and drops empty street lines', () => {
    const address = Address.of({
      name: ' Mette ',
      lines: ['Klostergade 3', '  '],
      postcode: '8000',
      city: 'Aarhus',
      country: 'DK',
    });
    expect(address.toLines()).toEqual(['Mette', 'Klostergade 3', '8000 Aarhus', 'DK']);
    expect(address.isDomestic).toBe(true);
  });

  it('needs a name, a street and a country code', () => {
    const fields = { name: 'x', lines: [], postcode: '1', city: 'y', country: 'DK' };
    expect(() => Address.of(fields)).toThrow(RangeError);
    expect(() => Address.of({ ...fields, lines: ['a'], country: 'Denmark' })).toThrow(RangeError);
  });
});

describe('TrackingNumber', () => {
  it('normalises and recognises the carrier from its prefix', () => {
    const tracking = TrackingNumber.parse(' cvd0000000001 ');
    expect(tracking?.toString()).toBe('CVD0000000001');
    expect(tracking?.carrier).toBe('corvid');
    expect(TrackingNumber.parse('ALD123456789012')?.carrier).toBe('alder');
  });

  it('rejects other shapes', () => {
    expect(TrackingNumber.parse('CVD-1')).toBeUndefined();
    expect(TrackingNumber.parse('XXX0000000001')?.carrier).toBeUndefined();
  });
});
