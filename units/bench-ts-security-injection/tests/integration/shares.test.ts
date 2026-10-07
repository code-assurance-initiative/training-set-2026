import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { documentId } from '../support/documents.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let api: TestApp;

beforeEach(async () => {
  api = await createTestApp();
});

/** The grant cookie set by an unlock (Secure cookies are not replayed over the test's plain HTTP). */
function grantCookie(response: request.Response): string {
  const header = response.headers['set-cookie'] as unknown as string[] | undefined;
  return (header?.[0] ?? '').split(';')[0] ?? '';
}

async function share(): Promise<string> {
  const response = await request(api.app)
    .post(`/api/documents/${documentId}/shares`)
    .set('Authorization', api.fullAccess)
    .send({ password: 'board-minutes', validDays: 7 })
    .expect(201);
  return String(response.body.token);
}

describe('share links', () => {
  it('creates a link that expires after the requested days', async () => {
    const token = await share();

    expect(token).toMatch(/^[\w-]{32}$/);
    expect(api.shares[0]).toMatchObject({ documentId, createdBy: 'archivist-4' });
    expect(api.shares[0]?.expiresAt.toISOString()).toBe('2026-10-14T08:00:00.000Z');
  });

  it('refuses a link for an unknown document or with invalid settings', async () => {
    await request(api.app)
      .post('/api/documents/00000000-0000-4000-8000-000000000000/shares')
      .set('Authorization', api.fullAccess)
      .send({ password: 'board-minutes', validDays: 7 })
      .expect(404);
    await request(api.app)
      .post(`/api/documents/${documentId}/shares`)
      .set('Authorization', api.fullAccess)
      .send({ password: 'board-minutes', validDays: 365 })
      .expect(400);
  });

  it('unlocks with the password, then serves the document to that browser', async () => {
    const token = await share();
    const unlocked = await request(api.app)
      .post(`/shares/${token}/unlock`)
      .type('form')
      .send({ password: 'board-minutes' })
      .expect(303);
    const document = await request(api.app)
      .get(`/shares/${token}/document`)
      .set('Cookie', grantCookie(unlocked))
      .expect(200);

    expect(unlocked.headers.location).toBe(`/shares/${token}/document`);
    expect(String(unlocked.headers['set-cookie'])).toMatch(/HttpOnly; Secure; SameSite=Strict/);
    expect(document.body).toMatchObject({ id: documentId, number: 'HR-2024-001337' });
  });

  it('refuses a wrong password, an unknown token and a browser that did not unlock', async () => {
    const token = await share();

    await request(api.app)
      .post(`/shares/${token}/unlock`)
      .type('form')
      .send({ password: 'guess' })
      .expect(403);
    await request(api.app)
      .post(`/shares/${'x'.repeat(32)}/unlock`)
      .type('form')
      .send({ password: 'guess' })
      .expect(403);
    await request(api.app)
      .post('/shares/short/unlock')
      .type('form')
      .send({ password: 'guess' })
      .expect(400);
    await request(api.app).get(`/shares/${token}/document`).expect(403);
    await request(api.app).get('/shares/short/document').expect(400);
  });

  it('answers 404 when the shared document was deleted', async () => {
    const token = await share();
    const unlocked = await request(api.app)
      .post(`/shares/${token}/unlock`)
      .type('form')
      .send({ password: 'board-minutes' });
    api.documents.clear();

    await request(api.app)
      .get(`/shares/${token}/document`)
      .set('Cookie', grantCookie(unlocked))
      .expect(404);
  });
});
