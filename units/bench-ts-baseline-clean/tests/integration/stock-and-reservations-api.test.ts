import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { z } from 'zod';
import { createTestApp, type TestApp } from '../support/test-app.js';

const skuCode = 'BOLT-M8-40';
const binCode = 'B04-12-03';
const createdReservation = z.object({ id: z.uuid() });

describe('stock and reservations API', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
    await post('/api/skus', { code: skuCode, description: 'Hex bolt', unitOfMeasure: 'EA' });
    await post('/api/bins', { code: binCode, zone: 'DRY', capacity: 100 });
    await post(`/api/stock/${skuCode}/receipts`, { binCode, quantity: 20 });
  });

  function post(path: string, body: object) {
    return request(api.app).post(path).set('Authorization', api.fullAccess).send(body);
  }

  function get(path: string) {
    return request(api.app).get(path).set('Authorization', api.fullAccess);
  }

  it('reports stock per bin', async () => {
    const response = await get(`/api/stock/${skuCode}`).expect(200);

    expect(response.body).toEqual([{ skuCode, binCode, onHand: 20, reserved: 0, available: 20 }]);
  });

  it('refuses a receipt beyond the bin capacity', async () => {
    await post(`/api/stock/${skuCode}/receipts`, { binCode, quantity: 81 }).expect(409);
  });

  it('applies a physical count', async () => {
    const response = await post(`/api/stock/${skuCode}/counts`, { binCode, quantity: 18 }).expect(
      200,
    );

    expect(response.body).toMatchObject({ onHand: 18, available: 18 });
  });

  it('rejects a stock movement for a malformed SKU code', async () => {
    await post('/api/stock/bolt/counts', { binCode, quantity: 1 }).expect(400);
  });

  it('reserves, reads and fulfils stock', async () => {
    const created = await post('/api/reservations', { skuCode, binCode, quantity: 5 }).expect(201);
    const { id } = createdReservation.parse(created.body);
    expect(created.headers.location).toBe(`/api/reservations/${id}`);
    expect(created.body).toMatchObject({
      status: 'active',
      createdAt: '2026-10-07T08:00:00.000Z',
      expiresAt: '2026-10-07T08:15:00.000Z',
    });

    await get(`/api/reservations/${id}`).expect(200);
    await post(`/api/reservations/${id}/fulfilment`, {}).expect(200);

    const stock = await get(`/api/stock/${skuCode}`).expect(200);
    expect(stock.body).toEqual([{ skuCode, binCode, onHand: 15, reserved: 0, available: 15 }]);
  });

  it('releases a reservation and refuses to release it again', async () => {
    const created = await post('/api/reservations', { skuCode, binCode, quantity: 5 }).expect(201);
    const { id } = createdReservation.parse(created.body);

    await post(`/api/reservations/${id}/release`, {}).expect(200);
    await post(`/api/reservations/${id}/release`, {}).expect(409);
  });

  it('lets a lapsed hold expire so its stock can be reserved again', async () => {
    await post('/api/reservations', { skuCode, binCode, quantity: 20, holdMinutes: 5 }).expect(201);
    await post('/api/reservations', { skuCode, binCode, quantity: 1 }).expect(409);

    api.clock.advanceMinutes(6);

    await post('/api/reservations', { skuCode, binCode, quantity: 20 }).expect(201);
  });

  it('answers an unknown reservation with 404 and a malformed id with 400', async () => {
    await get('/api/reservations/7d444840-9dc0-11d1-b245-5ffdce74fad2').expect(404);
    await get('/api/reservations/12345').expect(400);
  });

  it('rejects a reservation held longer than a week', async () => {
    await post('/api/reservations', { skuCode, binCode, quantity: 1, holdMinutes: 20_000 }).expect(
      400,
    );
  });
});
