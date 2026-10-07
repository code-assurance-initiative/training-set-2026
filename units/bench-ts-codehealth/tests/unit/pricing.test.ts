import { describe, expect, it } from 'vitest';
import { CutoffCalendar, parseCutoff } from '../../src/application/pricing/cutoff-calendar.js';
import { MultiParcelQuoter } from '../../src/application/pricing/multi-parcel-quoter.js';
import { parseRateCardCsv } from '../../src/application/pricing/rate-card-csv.js';
import { serviceCode } from '../../src/application/pricing/service-code-map.js';
import { SurchargePolicy } from '../../src/application/pricing/surcharge-policy.js';
import { surchargeTable } from '../../src/composition.js';
import {
  requiresCustomsDeclaration,
  zoneFor,
} from '../../src/infrastructure/zones/country-zones.generated.js';
import { parcel, quoteRequest } from '../support/builders.js';
import { silentLogger } from '../support/silent-logger.js';

describe('SurchargePolicy', () => {
  const policy = new SurchargePolicy(surchargeTable);

  it('lists the per-parcel surcharges that apply', () => {
    const request = quoteRequest({ parcels: [parcel(30_000, [130, 20, 20], 60_000), parcel()] });
    expect(policy.perParcel(request).map((s) => s.code)).toEqual([
      'OVERSIZE',
      'HEAVY',
      'INSURANCE',
    ]);
  });

  it('insures only above the free amount', () => {
    expect(policy.insurance(parcel(1_000, [10, 10, 10], 50_000))).toBeUndefined();
    expect(policy.insurance(parcel(1_000, [10, 10, 10], 100_000))?.amount.minorUnits).toBe(1_500);
    expect(policy.remoteArea().code).toBe('REMOTE');
  });
});

describe('serviceCode', () => {
  it('maps carrier, level and scope to a product code', () => {
    expect(serviceCode('alder', 'express', true)).toBe('ALD-EXP-X');
    expect(serviceCode('corvid', 'economy', false)).toBe('CV10');
    expect(() => serviceCode('owl', 'economy', false)).toThrow(RangeError);
  });
});

describe('generated zones', () => {
  it('puts Denmark in zone 1 and the EU in zone 2, with customs outside the EU', () => {
    expect(zoneFor('DK')).toBe('Z1');
    expect(zoneFor('DE')).toBe('Z2');
    expect(zoneFor('US')).toBe('Z4');
    expect(zoneFor('??')).toBe('Z5');
    expect(requiresCustomsDeclaration('DE')).toBe(false);
    expect(requiresCustomsDeclaration('NO')).toBe(true);
  });
});

describe('MultiParcelQuoter', () => {
  it('charges standard handling per piece', () => {
    const quoter = new MultiParcelQuoter('DKK', 500, 2_000, 120);
    const charges = quoter.handlingCharges([parcel(), parcel()]);
    expect(charges.map((c) => c.handling.minorUnits)).toEqual([500, 500]);
    expect(quoter.handlingCharges([])).toEqual([]);
  });
});

describe('CutoffCalendar', () => {
  it('parses HH:MM and ships tomorrow after the cut-off', () => {
    expect(parseCutoff('09:45')).toEqual({ hour: 9, minute: 45 });
    const calendar = new CutoffCalendar({ alder: '16:00', corvid: 'late' }, silentLogger);
    const before = new Date('2026-10-07T15:59:00.000Z');
    const after = new Date('2026-10-07T16:00:00.000Z');
    expect(calendar.shipDate('alder', before).toISOString()).toBe('2026-10-07T00:00:00.000Z');
    expect(calendar.shipDate('alder', after).toISOString()).toBe('2026-10-08T00:00:00.000Z');
    expect(calendar.cutoffFor('corvid')).toEqual({ hour: 15, minute: 0 });
    expect(calendar.cutoffFor('owl')).toEqual({ hour: 15, minute: 0 });
  });
});

describe('parseRateCardCsv', () => {
  const header =
    'carrier,service_level,currency,zone,per_kg,minimum,fuel_percent,volumetric_divisor,transit_days';

  it('folds the zone rows of one card', () => {
    const cards = parseRateCardCsv(
      [
        header,
        'alder,standard,DKK,Z1,1000,4900,5,5000,2',
        'alder,standard,DKK,Z2,1800,8900,5,5000,2',
      ].join('\n'),
    );
    expect(cards).toHaveLength(1);
    expect(cards[0]?.perKilogram).toEqual({ Z1: 1000, Z2: 1800 });
    expect(cards[0]?.fuelPercent).toBe(5);
  });

  it('rejects other files and bad rows', () => {
    expect(() => parseRateCardCsv('a,b\n1,2')).toThrow(/header/);
    expect(() => parseRateCardCsv(`${header}\nalder,standard,DKK`)).toThrow(/9 columns/);
    expect(() => parseRateCardCsv(`${header}\nalder,overnight,DKK,Z1,1,1,0,5000,1`)).toThrow(
      /service level/,
    );
    expect(() => parseRateCardCsv(`${header}\nalder,standard,DKK,Z1,1.5,1,0,5000,1`)).toThrow(
      /whole/,
    );
  });
});

describe('service codes for every carrier and scope', () => {
  it('covers the whole table', () => {
    const levels = ['economy', 'standard', 'express'] as const;
    const codes = ['alder', 'corvid'].flatMap((carrier) =>
      levels.flatMap((level) => [
        serviceCode(carrier, level, false),
        serviceCode(carrier, level, true),
      ]),
    );
    expect(new Set(codes).size).toBe(12);
  });
});
