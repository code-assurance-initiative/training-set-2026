import { EventEmitter } from 'node:events';
import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { loadConfig } from '../../src/config.js';
import { run } from '../../src/lifecycle.js';
import { startService } from '../../src/server.js';
import { silentLogger } from '../support/silent-logger.js';
import { createTestApp } from '../support/test-app.js';
import { createTokenIssuer, testIssuer } from '../support/test-tokens.js';

function environment(publicKeyPem: string) {
  return {
    NODE_ENV: 'test',
    PORT: '0',
    LOG_LEVEL: 'silent',
    JWT_ISSUER: testIssuer,
    JWT_PUBLIC_KEY: publicKeyPem,
    DATABASE_URL: 'postgres://archive@127.0.0.1:1/archive',
    MONGODB_URL: 'mongodb://127.0.0.1:1/archive',
    CRM_BASE_URL: 'https://crm.internal.test/api',
    MAIL_RELAY_URL: 'https://mail-relay.internal.test',
    PSEUDONYM_KEY: 'p'.repeat(48),
    PUBLIC_BASE_URL: 'https://archive.example.org',
  };
}

describe('service lifecycle', () => {
  it('listens until closed; stores connect only when first used', async () => {
    const tokens = await createTokenIssuer();
    const service = await startService(loadConfig(environment(tokens.publicKeyPem)), silentLogger);

    const response = await request(`http://127.0.0.1:${String(service.port)}`).get('/health');
    await service.close();

    expect(response.status).toBe(200);
    expect(response.body).toEqual({ status: 'ok' });
  });

  it('starts from the environment and stops on SIGTERM', async () => {
    const tokens = await createTokenIssuer();
    const signals = new EventEmitter();

    const service = await run(environment(tokens.publicKeyPem), signals, silentLogger);
    expect(service?.port).toBeGreaterThan(0);
    signals.emit('SIGTERM', 'SIGTERM');
    signals.emit('SIGINT', 'SIGINT');
    await new Promise((resolve) => setTimeout(resolve, 50));

    await expect(
      request(`http://127.0.0.1:${String(service?.port)}`).get('/health'),
    ).rejects.toThrow();
  });

  it('refuses to start without configuration', async () => {
    await expect(run({}, new EventEmitter(), silentLogger)).resolves.toBeUndefined();
  });
});

describe('security headers', () => {
  it('are set on every response', async () => {
    const { app } = await createTestApp();

    const response = await request(app).get('/health');

    expect(response.headers['content-security-policy']).toBe(
      "default-src 'none';frame-ancestors 'none'",
    );
    expect(response.headers['strict-transport-security']).toMatch(/^max-age=\d+/);
    expect(response.headers['x-content-type-options']).toBe('nosniff');
    expect(response.headers['x-powered-by']).toBeUndefined();
    expect(response.headers['x-request-id']).toMatch(/^[0-9a-f-]{36}$/);
  });
});

describe('HTTPS enforcement', () => {
  it('refuses plain HTTP and accepts what the trusted proxy received over HTTPS', async () => {
    const api = await createTestApp({ requireHttps: true });

    await request(api.app)
      .get('/api/documents/by-number/HR-2024-001337')
      .set('Authorization', api.fullAccess)
      .expect(403);
    await request(api.app).post('/unsubscribe').send({ email: 'a@b.c', token: 't' }).expect(403);
    await request(api.app)
      .get('/api/documents/by-number/HR-2024-001337')
      .set('X-Forwarded-Proto', 'https')
      .set('Authorization', api.fullAccess)
      .expect(200);
  });
});

describe('errors', () => {
  it('answers malformed JSON with 400, an oversized body with 413 and an unknown path with 404', async () => {
    const api = await createTestApp();

    await request(api.app)
      .post('/api/saved-searches')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/json')
      .send('{"name":')
      .expect(400);
    await request(api.app)
      .post('/api/saved-searches')
      .set('Authorization', api.fullAccess)
      .send({ name: 'x'.repeat(300_000) })
      .expect(413);
    await request(api.app)
      .get('/api/nothing-here')
      .set('Authorization', api.fullAccess)
      .expect(404);
  });

  it('answers an unexpected failure with 500 and no details', async () => {
    const api = await createTestApp();

    const response = await request(api.app)
      .get('/api/feeds/entries?url=https://unreachable.partner.test/feed')
      .set('Authorization', api.fullAccess)
      .expect(500);

    expect(response.body).toEqual({
      type: 'about:blank',
      title: 'Internal Server Error',
      status: 500,
      detail: 'The request could not be processed.',
    });
  });
});

describe('authorization', () => {
  it.each([
    ['get', '/api/search?q=board'],
    ['get', '/api/documents/by-number/HR-2024-001337'],
    ['post', '/api/imports/metadata'],
    ['get', '/api/reports?owner=archivist-4'],
    ['get', '/api/preferences'],
  ] as const)('refuses %s %s without a token', async (method, path) => {
    const { app } = await createTestApp();

    const response = await request(app)[method](path).expect(401);

    expect(response.headers['www-authenticate']).toBe('Bearer');
  });

  it('refuses a route whose scope the token lacks', async () => {
    const api = await createTestApp();
    const token = await api.tokens.issue({ scopes: ['documents.read'] });

    await request(api.app)
      .get('/api/reports?owner=archivist-4')
      .auth(token, { type: 'bearer' })
      .expect(403);
    await request(api.app).get('/api/search?q=board').auth(token, { type: 'bearer' }).expect(200);
  });
});
