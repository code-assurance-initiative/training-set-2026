import express from 'express';
import request from 'supertest';
import { describe, expect, it, vi } from 'vitest';
import { quoteRoutes } from '../../src/api/routes/quote-routes.js';
import type { QuoteService } from '../../src/application/pricing/quote-service.js';
import { addressBody, parcelBody, senderBody } from '../support/builders.js';

vi.mock('../../src/api/scopes.js', () => ({
  Scopes: { ratesRead: 'rates.read' },
  requireScope: vi.fn(() => (_req: unknown, _res: unknown, next: () => void) => {
    next();
  }),
}));

describe('quote routes', () => {
  it('answers a quote', async () => {
    const quote = vi.fn(() => ({
      quotes: [],
      cheapest: undefined,
      fastest: undefined,
      handling: [],
    }));
    const quotes = { quote, quoteSaturday: vi.fn() } as unknown as QuoteService;
    const clock = vi.fn(() => new Date('2026-10-07T09:00:00.000Z'));
    const app = express().use(express.json(), quoteRoutes(quotes, clock));
    await request(app)
      .post('/quotes')
      .send({
        serviceLevel: 'standard',
        sender: senderBody,
        recipient: addressBody,
        parcels: [parcelBody],
      });
    expect(quote).toHaveBeenCalledTimes(1);
    expect(clock).toHaveBeenCalledTimes(1);
  });
});
