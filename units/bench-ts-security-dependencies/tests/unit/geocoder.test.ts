import nock from 'nock';
import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { GeocodingError, MapsGeocoder } from '../../src/addresses/geocoder.js';

const maps = 'https://maps.googleapis.com';
const address = { street: 'Søndergade 12', postcode: '8000', city: 'Aarhus C' };

describe('maps geocoder', () => {
  beforeAll(() => {
    nock.disableNetConnect();
  });
  afterEach(() => {
    nock.cleanAll();
  });
  afterAll(() => {
    nock.enableNetConnect();
  });

  it('asks for the full Danish address and returns its position', async () => {
    const scope = nock(maps)
      .get('/maps/api/geocode/json')
      .query(
        (query) =>
          query.address === 'Søndergade 12, 8000 Aarhus C' && query.components === 'country:DK',
      )
      .reply(200, {
        status: 'OK',
        results: [{ geometry: { location: { lat: 56.1554, lng: 10.2106 } } }],
      });

    await expect(new MapsGeocoder('k'.repeat(20)).locate(address)).resolves.toEqual({
      lat: 56.1554,
      lng: 10.2106,
    });
    expect(scope.isDone()).toBe(true);
  });

  it('returns null for an address the provider does not know', async () => {
    nock(maps)
      .get('/maps/api/geocode/json')
      .query(true)
      .reply(200, { status: 'ZERO_RESULTS', results: [] });

    await expect(new MapsGeocoder('k'.repeat(20)).locate(address)).resolves.toBeNull();
  });

  it('fails on any other status', async () => {
    nock(maps)
      .get('/maps/api/geocode/json')
      .query(true)
      .reply(200, { status: 'OVER_QUERY_LIMIT', results: [] });

    await expect(new MapsGeocoder('k'.repeat(20)).locate(address)).rejects.toThrow(GeocodingError);
  });
});
