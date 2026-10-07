import { describe, expect, it } from 'vitest';
import { RateCalculator } from '../../src/application/pricing/rate-calculator.js';
import { RemoteAreaLookup } from '../../src/application/pricing/remote-area-lookup.js';
import { SurchargePolicy } from '../../src/application/pricing/surcharge-policy.js';
import { surchargeTable } from '../../src/composition.js';
import { zoneFor } from '../../src/infrastructure/zones/country-zones.generated.js';
import { hamburg, parcel, quoteRequest, rateCard } from '../support/builders.js';
import { RateCardList } from '../support/fakes.js';
import { silentLogger } from '../support/silent-logger.js';

function calculator(...cards: ReturnType<typeof rateCard>[]): RateCalculator {
  return new RateCalculator(
    new RateCardList(cards),
    new SurchargePolicy(surchargeTable),
    new RemoteAreaLookup(silentLogger),
    zoneFor,
  );
}

const codes = (request = quoteRequest(), ...cards: ReturnType<typeof rateCard>[]) =>
  calculator(...cards)
    .quoteAll(request)[0]
    ?.surcharges.map((s) => s.code);

describe('RateCalculator', () => {
  it('prices the billable kilograms with the zone minimum', () => {
    const [quote] = calculator(rateCard()).quoteAll(quoteRequest());
    expect(quote?.zone).toBe('Z1');
    expect(quote?.base.minorUnits).toBe(4_900);
    expect(quote?.serviceCode).toBe('ALD-STD');
    expect(quote?.total.toString()).toBe('49.00 DKK');
  });

  it('quotes every carrier that has a card, or only the requested one', () => {
    const cards = [rateCard(), rateCard({ carrier: 'corvid' })];
    expect(calculator(...cards).quoteAll(quoteRequest())).toHaveLength(2);
    expect(calculator(...cards).quoteAll(quoteRequest({ carrier: 'corvid' }))).toHaveLength(1);
    expect(calculator(...cards).quoteAll(quoteRequest({ serviceLevel: 'express' }))).toEqual([]);
  });

  it('adds the Alder express bulky surcharge in the near zones', () => {
    const express = rateCard({ serviceLevel: 'express' });
    const bulky = quoteRequest({
      serviceLevel: 'express',
      parcels: [parcel(21_000, [130, 40, 40])],
    });
    expect(codes(bulky, express)).toEqual(['ALDER-EXPRESS-BULKY']);
    const oversize = quoteRequest({
      serviceLevel: 'express',
      parcels: [parcel(5_000, [130, 40, 40])],
    });
    expect(codes(oversize, express)).toEqual(['OVERSIZE']);
  });

  it('uses the policy for Alder express outside the near zones', () => {
    const request = quoteRequest({
      serviceLevel: 'express',
      recipient: hamburg,
      parcels: [parcel(5_000, [130, 40, 40])],
    });
    expect(codes(request, rateCard({ serviceLevel: 'express' }))).toEqual(['OVERSIZE']);
  });

  it('charges heavy Alder parcels except domestic economy', () => {
    expect(codes(quoteRequest({ parcels: [parcel(30_000)] }), rateCard())).toEqual(['HEAVY']);
    const economy = rateCard({ serviceLevel: 'economy' });
    expect(
      codes(quoteRequest({ serviceLevel: 'economy', parcels: [parcel(30_000)] }), economy),
    ).toEqual([]);
  });

  it('charges Corvid for non-conveyable and for oversize or heavy parcels', () => {
    const corvid = rateCard({ carrier: 'corvid' });
    expect(codes(quoteRequest({ parcels: [parcel(2_000, [200, 60, 50])] }), corvid)).toEqual([
      'CORVID-NONCONVEYABLE',
    ]);
    expect(codes(quoteRequest({ parcels: [parcel(30_000, [130, 20, 20])] }), corvid)).toEqual([
      'OVERSIZE',
      'HEAVY',
    ]);
  });

  it('adds insurance, Saturday delivery and fuel', () => {
    const card = rateCard({ serviceLevel: 'express', fuelPercent: 10 });
    const request = quoteRequest({
      serviceLevel: 'express',
      parcels: [parcel(2_000, [30, 20, 10], 100_000)],
    });
    const [quote] = calculator(card).quoteAll(request, true);
    expect(quote?.surcharges.map((s) => s.code)).toEqual(['INSURANCE', 'SATURDAY', 'FUEL']);
    expect(quote?.surcharges.at(-1)?.amount.minorUnits).toBe(Math.round((4_900 + 1_500) / 10));
  });
});
