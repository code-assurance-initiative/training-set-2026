import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { createTestApp } from '../support/test-app.js';

describe('GET /health', () => {
  it('answers without a token', async () => {
    const { app } = await createTestApp();

    const response = await request(app).get('/health').expect(200);

    expect(response.body).toEqual({ status: 'ok' });
    expect(response.headers['cache-control']).toBe('no-store');
  });

  it('answers over plain HTTP even where the API requires HTTPS', async () => {
    const { app } = await createTestApp({ requireHttps: true });

    await request(app).get('/health').expect(200);
  });
});
