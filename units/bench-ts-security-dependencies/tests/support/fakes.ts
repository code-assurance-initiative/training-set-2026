import type { Geocoder } from '../../src/addresses/geocoder.js';
import type { Linehaul, Quote, QuoteRequest } from '../../src/carriers/linehaul-client.js';
import type { Address, Coordinates } from '../../src/dispatch/dispatch-run.js';
import type { Clock } from '../../src/dispatch/dispatch-service.js';
import { labelPdf } from './label-pdf.js';

export class FakeGeocoder implements Geocoder {
  readonly asked: Address[] = [];

  constructor(private readonly known: Readonly<Record<string, Coordinates>> = {}) {}

  locate(address: Address): Promise<Coordinates | null> {
    this.asked.push(address);
    return Promise.resolve(this.known[address.street] ?? null);
  }
}

export class FakeLinehaul implements Linehaul {
  readonly quotes: QuoteRequest[] = [];
  readonly labelsFetched: string[] = [];
  #failure: Error | undefined;

  /** Every later call rejects with `error`. */
  failWith(error: Error): void {
    this.#failure = error;
  }

  quote(request: QuoteRequest): Promise<Quote> {
    if (this.#failure) {
      return Promise.reject(this.#failure);
    }
    this.quotes.push(request);
    return Promise.resolve({
      quoteId: 'Q-1001',
      priceMinor: 125_000 + request.parcels * 1_500,
      currency: 'DKK',
      validUntil: '2026-11-02T06:00:00Z',
    });
  }

  fetchLabel(trackingNumber: string): Promise<Uint8Array> {
    if (this.#failure) {
      return Promise.reject(this.#failure);
    }
    this.labelsFetched.push(trackingNumber);
    return Promise.resolve(labelPdf(trackingNumber));
  }
}

export const fixedClock = (iso: string): Clock => ({ now: () => new Date(iso) });
