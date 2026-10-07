import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { createTestApp, type TestApp } from '../support/test-app.js';

describe('catalogue API', () => {
  let api: TestApp;

  beforeEach(async () => {
    api = await createTestApp();
  });

  it('registers a SKU and reads it back', async () => {
    const sku = { code: 'BOLT-M8-40', description: 'Hex bolt M8 x 40', unitOfMeasure: 'EA' };

    const created = await request(api.app)
      .post('/api/skus')
      .set('Authorization', api.fullAccess)
      .send(sku)
      .expect(201);

    expect(created.headers.location).toBe('/api/skus/BOLT-M8-40');
    const read = await request(api.app)
      .get('/api/skus/BOLT-M8-40')
      .set('Authorization', api.fullAccess)
      .expect(200);
    expect(read.body).toEqual(sku);
  });

  it('answers a duplicate SKU with 409', async () => {
    const sku = { code: 'NUT-M8', description: 'Hex nut M8', unitOfMeasure: 'EA' };
    await request(api.app).post('/api/skus').set('Authorization', api.fullAccess).send(sku);

    const response = await request(api.app)
      .post('/api/skus')
      .set('Authorization', api.fullAccess)
      .send(sku)
      .expect(409);

    expect(response.body).toEqual({
      type: 'about:blank',
      title: 'Conflict',
      status: 409,
      detail: "SKU 'NUT-M8' is already registered.",
    });
  });

  it('rejects an invalid SKU with every problem listed', async () => {
    const response = await request(api.app)
      .post('/api/skus')
      .set('Authorization', api.fullAccess)
      .send({ code: 'bad code', description: '', unitOfMeasure: 'EA', price: 3 })
      .expect(400);

    expect(response.body).toMatchObject({
      status: 400,
      errors: expect.arrayContaining([
        expect.objectContaining({ path: 'code' }),
        expect.objectContaining({ path: 'description' }),
      ]) as unknown,
    });
  });

  it('registers bins and lists them a page at a time', async () => {
    for (const code of ['A01-01-02', 'A01-01-01']) {
      await request(api.app)
        .post('/api/bins')
        .set('Authorization', api.fullAccess)
        .send({ code, zone: 'DRY', capacity: 50 })
        .expect(201);
    }

    const response = await request(api.app)
      .get('/api/bins')
      .query({ offset: 0, limit: 1 })
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.body).toEqual({
      items: [{ code: 'A01-01-01', zone: 'DRY', capacity: 50 }],
      total: 2,
      offset: 0,
      limit: 1,
    });
  });

  it('lists SKUs with the default page size', async () => {
    const response = await request(api.app)
      .get('/api/skus')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.body).toEqual({ items: [], total: 0, offset: 0, limit: 25 });
  });

  it('rejects a page size above the maximum', async () => {
    await request(api.app)
      .get('/api/skus?limit=500')
      .set('Authorization', api.fullAccess)
      .expect(400);
  });

  it('answers an unknown bin with 404 and a malformed bin code with 400', async () => {
    await request(api.app)
      .get('/api/bins/Z99-99-99')
      .set('Authorization', api.fullAccess)
      .expect(404);
    await request(api.app).get('/api/bins/nope').set('Authorization', api.fullAccess).expect(400);
  });
});
