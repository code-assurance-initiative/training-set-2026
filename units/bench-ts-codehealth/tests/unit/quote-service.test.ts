import { pino } from 'pino';
import { describe, expect, it } from 'vitest';
import { MultiParcelQuoter } from '../../src/application/pricing/multi-parcel-quoter.js';
import { QuoteService } from '../../src/application/pricing/quote-service.js';
import { RateCalculator } from '../../src/application/pricing/rate-calculator.js';
import { RemoteAreaLookup } from '../../src/application/pricing/remote-area-lookup.js';
import { SurchargePolicy } from '../../src/application/pricing/surcharge-policy.js';
import { surchargeTable } from '../../src/composition.js';
import { zoneFor } from '../../src/infrastructure/zones/country-zones.generated.js';
import { quoteRequest, rateCard } from '../support/builders.js';
import { RateCardList } from '../support/fakes.js';

function service(level: 'debug' | 'info', lines: string[]): QuoteService {
  const logger = pino({ level }, { write: (line: string) => lines.push(line) });
  const cards = new RateCardList([
    rateCard(),
    rateCard({ carrier: 'corvid', transitDays: 1, minimum: { Z1: 9_900 } }),
  ]);
  return new QuoteService(
    new RateCalculator(
      cards,
      new SurchargePolicy(surchargeTable),
      new RemoteAreaLookup(logger),
      zoneFor,
    ),
    new MultiParcelQuoter('DKK', 500, 2_000, 120),
    logger,
  );
}

describe('QuoteService', () => {
  it('picks the cheapest and the fastest quote', () => {
    const summary = service('info', []).quote(quoteRequest());
    expect(summary.cheapest?.carrier).toBe('alder');
    expect(summary.fastest?.carrier).toBe('corvid');
    expect(summary.handling).toHaveLength(1);
  });

  it('logs the quotes at debug level', () => {
    const lines: string[] = [];
    service('debug', lines).quote(quoteRequest());
    expect(lines.some((line) => line.includes('Quotes computed'))).toBe(true);
  });

  it('logs Saturday quotes only when debug is on', () => {
    const quiet: string[] = [];
    const verbose: string[] = [];
    expect(
      service('info', quiet).quoteSaturday(quoteRequest({ serviceLevel: 'standard' })),
    ).toHaveLength(2);
    service('debug', verbose).quoteSaturday(quoteRequest());
    expect(quiet).toEqual([]);
    expect(verbose.some((line) => line.includes('Saturday quotes'))).toBe(true);
  });
});
