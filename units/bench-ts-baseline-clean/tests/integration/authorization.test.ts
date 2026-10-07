import request from 'supertest';
import { beforeAll, describe, expect, it } from 'vitest';
import { Scopes } from '../../src/http/scopes.js';
import { createTokenIssuer } from '../support/test-tokens.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

const sku = { code: 'BOLT-M8-40', description: 'Hex bolt', unitOfMeasure: 'EA' };
const reservationId = '7d444840-9dc0-11d1-b245-5ffdce74fad2';

describe('authorization', () => {
  let api: TestApp;

  beforeAll(async () => {
    api = await createTestApp();
  });

  it.each([
    ['get', '/api/skus'],
    ['post', '/api/skus'],
    ['get', '/api/bins/A01-01-01'],
    ['get', '/api/stock/BOLT-M8-40'],
    ['post', '/api/reservations'],
    ['get', '/api/no-such-route'],
  ] as const)('refuses %s %s without a token', async (method, path) => {
    const response = await request(api.app)[method](path).expect(401);

    expect(response.headers['www-authenticate']).toBe('Bearer');
    expect(response.headers['content-type']).toMatch(/^application\/problem\+json/);
  });

  it('refuses a token signed by someone else', async () => {
    const forger = await createTokenIssuer();
    const token = await forger.issue({ scopes: Object.values(Scopes) });

    await request(api.app).get('/api/skus').auth(token, { type: 'bearer' }).expect(401);
  });

  it('refuses a valid token presented under another scheme', async () => {
    const token = await api.tokens.issue({ scopes: [Scopes.stockRead] });

    await request(api.app).get('/api/skus').set('Authorization', `Token ${token}`).expect(401);
  });

  it('refuses a write with a read-only token', async () => {
    const token = await api.tokens.issue({ scopes: [Scopes.stockRead] });

    const response = await request(api.app)
      .post('/api/skus')
      .auth(token, { type: 'bearer' })
      .send(sku)
      .expect(403);

    expect(response.body).toMatchObject({ status: 403, title: 'Forbidden' });
  });

  it('refuses a reservation with a stock-only token', async () => {
    const token = await api.tokens.issue({ scopes: [Scopes.stockRead, Scopes.stockWrite] });

    await request(api.app)
      .post('/api/reservations')
      .auth(token, { type: 'bearer' })
      .send({ skuCode: sku.code, binCode: 'A01-01-01', quantity: 1 })
      .expect(403);
  });

  it.each([
    ['get', '/api/skus', Scopes.stockRead],
    ['get', '/api/skus/BOLT-M8-40', Scopes.stockRead],
    ['post', '/api/skus', Scopes.stockWrite],
    ['get', '/api/bins', Scopes.stockRead],
    ['get', '/api/bins/A01-01-01', Scopes.stockRead],
    ['post', '/api/bins', Scopes.stockWrite],
    ['get', '/api/stock/BOLT-M8-40', Scopes.stockRead],
    ['post', '/api/stock/BOLT-M8-40/receipts', Scopes.stockWrite],
    ['post', '/api/stock/BOLT-M8-40/counts', Scopes.stockWrite],
    ['get', `/api/reservations/${reservationId}`, Scopes.stockRead],
    ['post', '/api/reservations', Scopes.reservationsWrite],
    ['post', `/api/reservations/${reservationId}/release`, Scopes.reservationsWrite],
    ['post', `/api/reservations/${reservationId}/fulfilment`, Scopes.reservationsWrite],
  ] as const)('requires %s %s to carry %s', async (method, path, scope) => {
    const otherScopes = Object.values(Scopes).filter((candidate) => candidate !== scope);
    const token = await api.tokens.issue({ scopes: otherScopes });

    await request(api.app)[method](path).auth(token, { type: 'bearer' }).send({}).expect(403);
  });

  it('lets a token with the right scope through', async () => {
    const token = await api.tokens.issue({ scopes: [Scopes.stockRead] });

    await request(api.app).get('/api/skus').auth(token, { type: 'bearer' }).expect(200);
  });
});
