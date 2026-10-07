import nock from 'nock';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { LinehaulClient, LinehaulError } from '../../src/carriers/linehaul-client.js';

const baseUrl = 'https://partners.linehaul.test';
const apiKey = 'a'.repeat(32);
const client = new LinehaulClient({ baseUrl, apiKey, timeoutMs: 500 });
const request = { depotId: 'AAR', serviceDate: '2026-11-02', parcels: 3, totalWeightKg: 7.5 };

describe('linehaul client', () => {
  beforeAll(() => {
    nock.disableNetConnect();
  });
  afterEach(() => {
    nock.cleanAll();
  });
  afterAll(() => {
    nock.enableNetConnect();
  });

  it('posts the quote request with the API key and reads the quote', async () => {
    const scope = nock(baseUrl, { reqheaders: { 'x-api-key': apiKey } })
      .post('/v2/quotes', request)
      .reply(200, {
        quoteId: 'Q-77',
        priceMinor: 129_500,
        currency: 'DKK',
        validUntil: '2026-11-02T06:00:00Z',
      });

    await expect(client.quote(request)).resolves.toEqual({
      quoteId: 'Q-77',
      priceMinor: 129_500,
      currency: 'DKK',
      validUntil: '2026-11-02T06:00:00Z',
    });
    expect(scope.isDone()).toBe(true);
  });

  it('refuses a quote it cannot read', async () => {
    nock(baseUrl).post('/v2/quotes').reply(200, { quoteId: 'Q-77', priceMinor: -1 });

    await expect(client.quote(request)).rejects.toThrow(/cannot read/);
  });

  it('reports the carrier status on an error answer', async () => {
    nock(baseUrl).post('/v2/quotes').reply(503);

    await expect(client.quote(request)).rejects.toThrow(
      new LinehaulError('The carrier answered with HTTP 503.'),
    );
  });

  it('reports an unreachable carrier', async () => {
    nock(baseUrl).post('/v2/quotes').replyWithError('socket hang up');

    await expect(client.quote(request)).rejects.toThrow(/could not be reached/);
  });

  it('downloads a label as bytes', async () => {
    const pdf = Buffer.from('%PDF-1.7\n%label\n');
    nock(baseUrl, { reqheaders: { accept: 'application/pdf' } })
      .get('/v2/labels/AB12345678')
      .reply(200, pdf, { 'Content-Type': 'application/pdf' });

    const label = await client.fetchLabel('AB12345678');

    expect(Buffer.from(label).equals(pdf)).toBe(true);
  });
});
