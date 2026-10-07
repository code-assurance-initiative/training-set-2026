import { afterEach, describe, expect, it, vi } from 'vitest';
import { CarrierError } from '../../src/infrastructure/carriers/carrier-error.js';
import { CarrierRegistry } from '../../src/infrastructure/carriers/carrier-registry.js';
import { AlderParcelAdapter } from '../../src/infrastructure/carriers/alder-parcel-adapter.js';
import { CorvidCourierAdapter } from '../../src/infrastructure/carriers/corvid-courier-adapter.js';
import { CorvidTracking } from '../../src/infrastructure/carriers/corvid-tracking.js';
import { quoteRequest } from '../support/builders.js';
import { silentLogger } from '../support/silent-logger.js';

const alder = new AlderParcelAdapter({
  baseUrl: 'https://alder.test/v2',
  apiKey: 'k',
  timeoutMs: 1_000,
});
const corvid = new CorvidCourierAdapter({
  baseUrl: 'https://corvid.test',
  accountNumber: 'A1',
  token: 't',
});

function answer(status: number, body: unknown): ReturnType<typeof vi.fn> {
  const fetch = vi.fn(() =>
    Promise.resolve(
      new Response(typeof body === 'string' ? body : JSON.stringify(body), { status }),
    ),
  );
  vi.stubGlobal('fetch', fetch);
  return fetch;
}

afterEach(() => {
  vi.unstubAllGlobals();
});

describe('AlderParcelAdapter', () => {
  it('rates a shipment and sends rounded-up weights', async () => {
    const fetch = answer(200, { price: { amount: 5_900, currency: 'DKK' }, transit_days: 2 });
    const rate = await alder.rate(quoteRequest());
    expect(rate.price.toString()).toBe('59.00 DKK');
    expect(rate.transitDays).toBe(2);
    const [url, init] = fetch.mock.calls[0] as [string, RequestInit];
    expect(url).toBe('https://alder.test/v2/rates');
    expect(JSON.parse(init.body as string)).toMatchObject({
      service: 'standard',
      pieces: [{ weight_kg: 2 }],
    });
  });

  it('creates and voids labels', async () => {
    answer(200, { tracking_number: 'ALD0000000001', label: '^XA^XZ' });
    expect(await alder.createLabel(quoteRequest())).toEqual({
      trackingNumber: 'ALD0000000001',
      zpl: '^XA^XZ',
    });
    answer(200, {});
    await expect(alder.voidLabel('ALD0000000001')).resolves.toBeUndefined();
  });

  it('reports the carrier error code', async () => {
    answer(422, '{ "code": "POSTCODE_UNKNOWN", "message": "no such postcode" }');
    await expect(alder.rate(quoteRequest())).rejects.toMatchObject({
      status: 422,
      code: 'POSTCODE_UNKNOWN',
    });
  });
});

describe('CorvidCourierAdapter', () => {
  it('rates a shipment with an ISO duration', async () => {
    answer(200, { amount: 4_500, currency: 'DKK', transit: 'P1D' });
    expect((await corvid.rate(quoteRequest())).transitDays).toBe(1);
  });

  it('assumes three days when the duration is not in days', async () => {
    answer(200, { amount: 4_500, currency: 'DKK', transit: 'PT36H' });
    expect((await corvid.rate(quoteRequest())).transitDays).toBe(3);
  });

  it('creates a label, without ZPL while the consignment is pending', async () => {
    answer(200, { trackingNumber: 'CVD0000000001', status: 'TODO' });
    expect(await corvid.createLabel(quoteRequest())).toEqual({ trackingNumber: 'CVD0000000001' });
    answer(200, { trackingNumber: 'CVD0000000002', status: 'READY', zpl: '^XA^XZ' });
    expect(await corvid.createLabel(quoteRequest())).toEqual({
      trackingNumber: 'CVD0000000002',
      zpl: '^XA^XZ',
    });
  });

  it('maps the Corvid error code', async () => {
    answer(503, 'unavailable');
    try {
      await corvid.createLabel(quoteRequest());
      expect.fail('expected a carrier error');
    } catch {
      // the adapter throws
    }
  });

  it('cannot void labels yet', () => {
    expect(() => corvid.voidLabel('CVD0000000001')).toThrow(/Not implemented/);
  });

  it('builds tracking links', () => {
    const tracking = new CorvidTracking('https://track.corvid.test');
    expect(tracking.parcelUrl('CVD 1')).toBe('https://track.corvid.test/v2/parcels/CVD%201');
    expect(tracking.awaitingCollectionUrl('A1')).toBe(
      'https://track.corvid.test/v2/accounts/A1/parcels?state=TODO',
    );
  });
});

describe('CarrierRegistry', () => {
  const registry = new CarrierRegistry(
    {
      alder: { baseUrl: 'https://alder.test', apiKey: 'k', timeoutMs: 1_000 },
      corvid: { baseUrl: 'https://corvid.test', accountNumber: 'A1', token: 't' },
    },
    silentLogger,
  );

  it('finds carriers by code and alias, case-insensitively', () => {
    expect(registry.codes).toEqual(['alder', 'corvid']);
    expect(registry.find('alder')?.code).toBe('alder');
    expect(registry.find('ALX')?.code).toBe('alder');
    expect(registry.find('cvd')?.code).toBe('corvid');
    expect(registry.find('jjd')?.code).toBe('corvid');
    expect(registry.find('owl')).toBeUndefined();
  });

  it('is a CarrierError', () => {
    expect(new CarrierError('alder', 500, 'X').message).toBe('alder answered 500 (X)');
  });
});
