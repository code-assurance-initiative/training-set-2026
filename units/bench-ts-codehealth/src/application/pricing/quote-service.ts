import type { Logger } from 'pino';
import type { MultiParcelQuoter, ParcelCharge } from './multi-parcel-quoter.js';
import type { Quote, QuoteRequest } from './quote.js';
import type { RateCalculator } from './rate-calculator.js';

export interface QuoteSummary {
  readonly cheapest: Quote | undefined;
  readonly fastest: Quote | undefined;
  readonly quotes: readonly Quote[];
  readonly handling: readonly ParcelCharge[];
}

/** Quotes a shipment across carriers and picks the cheapest and the fastest. */
export class QuoteService {
  constructor(
    private readonly calculator: RateCalculator,
    private readonly handling: MultiParcelQuoter,
    private readonly logger: Logger,
  ) {}

  quote(request: QuoteRequest): QuoteSummary {
    const quotes = this.calculator.quoteAll(request);
    this.logger.debug(
      { quotes: JSON.stringify(quotes.map((quote) => [quote.carrier, quote.total.toString()])) },
      'Quotes computed',
    );
    return {
      cheapest: pick(quotes, (a, b) => a.total.minorUnits - b.total.minorUnits),
      fastest: pick(quotes, (a, b) => a.transitDays - b.transitDays),
      quotes,
      handling: this.handling.handlingCharges(request.parcels),
    };
  }

  /** Quotes with the Saturday-delivery surcharge included where a carrier offers it. */
  quoteSaturday(request: QuoteRequest): readonly Quote[] {
    const quotes = this.calculator.quoteAll(request, true);
    if (this.logger.isLevelEnabled('debug')) {
      this.logger.debug(
        {
          quotes: quotes.map((quote) => ({
            carrier: quote.carrier,
            total: quote.total.toString(),
          })),
        },
        'Saturday quotes',
      );
    }
    return quotes;
  }
}

function pick(
  quotes: readonly Quote[],
  compare: (a: Quote, b: Quote) => number,
): Quote | undefined {
  return [...quotes].sort(compare)[0];
}
