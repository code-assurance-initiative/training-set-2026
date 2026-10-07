import request from 'supertest';
import { beforeAll, describe, expect, it } from 'vitest';
import { createTestApp, type TestApp } from '../support/test-app.js';

describe('security headers', () => {
  it('are set on every response', async () => {
    const { app } = await createTestApp();

    const response = await request(app).get('/health');

    expect(response.headers['content-security-policy']).toBe(
      "default-src 'none';frame-ancestors 'none'",
    );
    expect(response.headers['strict-transport-security']).toMatch(/^max-age=\d+/);
    expect(response.headers).toMatchObject({
      'x-content-type-options': 'nosniff',
      'x-frame-options': 'SAMEORIGIN',
      'referrer-policy': 'no-referrer',
    });
    expect(response.headers['x-powered-by']).toBeUndefined();
    expect(response.headers['x-request-id']).toMatch(/^[0-9a-f-]{36}$/);
  });
});

describe('HTTPS enforcement', () => {
  let api: TestApp;

  beforeAll(async () => {
    api = await createTestApp({ requireHttps: true });
  });

  it('refuses an API request that arrived over plain HTTP', async () => {
    const response = await request(api.app)
      .get('/api/skus')
      .set('Authorization', api.fullAccess)
      .expect(403);

    expect(response.body).toMatchObject({ detail: 'This API is only served over HTTPS.' });
  });

  it('accepts a request the trusted proxy received over HTTPS', async () => {
    await request(api.app)
      .get('/api/skus')
      .set('X-Forwarded-Proto', 'https')
      .set('Authorization', api.fullAccess)
      .expect(200);
  });
});

describe('error responses', () => {
  let api: TestApp;

  beforeAll(async () => {
    api = await createTestApp();
  });

  it('answers malformed JSON with 400', async () => {
    const response = await request(api.app)
      .post('/api/skus')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/json')
      .send('{"code":')
      .expect(400);

    expect(response.body).toMatchObject({ detail: 'The request body is not valid JSON.' });
  });

  it('answers an oversized body with 413', async () => {
    await request(api.app)
      .post('/api/skus')
      .set('Authorization', api.fullAccess)
      .send({ code: 'BOLT-M8-40', description: 'x'.repeat(20_000), unitOfMeasure: 'EA' })
      .expect(413);
  });

  it('answers an unknown path with 404', async () => {
    await request(api.app).get('/nowhere').expect(404);
  });
});
