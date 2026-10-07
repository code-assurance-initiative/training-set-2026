import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { createTestApp } from '../support/test-app.js';

describe('HTTP security', () => {
  it('requires a bearer token on the API', async () => {
    const api = await createTestApp();

    const response = await request(api.app).get('/api/billing/invoices/overdue').expect(401);

    expect(response.headers['www-authenticate']).toBe('Bearer');
  });

  it('requires the scope of the operation', async () => {
    const api = await createTestApp();
    const token = await api.tokens.issue({ scopes: ['members.read'] });

    await request(api.app)
      .post('/api/members/3f2b8a51-6c1d-4e0f-9a7b-2d5c8e1f4a60/erasure')
      .set('Authorization', `Bearer ${token}`)
      .expect(403);
  });

  it('rejects a token from another issuer', async () => {
    const api = await createTestApp();
    const token = await api.tokens.issue({
      scopes: ['billing.read'],
      issuer: 'https://identity.elsewhere.test',
    });

    await request(api.app)
      .get('/api/billing/invoices/overdue')
      .set('Authorization', `Bearer ${token}`)
      .expect(401);
  });

  it('refuses plain HTTP when HTTPS is required', async () => {
    const api = await createTestApp({ requireHttps: true });

    await request(api.app)
      .get('/api/billing/invoices/overdue')
      .set('Authorization', api.fullAccess)
      .expect(403);
    await request(api.app)
      .get('/api/billing/invoices/overdue')
      .set('X-Forwarded-Proto', 'https')
      .set('Authorization', api.fullAccess)
      .expect(200);
  });

  it('sends security headers and answers unknown paths with a problem document', async () => {
    const api = await createTestApp();

    const response = await request(api.app).get('/nowhere').expect(404);

    expect(response.headers['content-security-policy']).toContain("default-src 'none'");
    expect(response.headers['x-content-type-options']).toBe('nosniff');
    expect(response.headers['x-powered-by']).toBeUndefined();
    expect(response.body).toMatchObject({ status: 404, title: 'Not Found' });
  });

  it('answers malformed JSON with 400', async () => {
    const api = await createTestApp();

    await request(api.app)
      .post('/api/members')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/json')
      .send('{"firstName":')
      .expect(400);
  });

  it('reports liveness and database readiness', async () => {
    const api = await createTestApp();

    await request(api.app).get('/health/live').expect(200, { status: 'ok' });
    await request(api.app).get('/health/ready').expect(200, { status: 'ok' });
    await api.db.destroy();
    await request(api.app).get('/health/ready').expect(503);
  });
});
