import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { reportId } from '../support/documents.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let api: TestApp;

beforeEach(async () => {
  api = await createTestApp();
});

async function subscribe(email: string): Promise<request.Response> {
  return request(api.app)
    .post(`/api/reports/${reportId}/subscriptions`)
    .set('Authorization', api.fullAccess)
    .send({ email });
}

describe('subscriptions', () => {
  it('subscribes an address once and lists subscribers without their tokens', async () => {
    const first = await subscribe('Ann@Example.org');
    const again = await subscribe('ann@example.org');
    const list = await request(api.app)
      .get(`/api/reports/${reportId}/subscriptions`)
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(first.status).toBe(201);
    expect(again.status).toBe(200);
    expect(again.body.id).toBe(first.body.id);
    expect(list.body.subscriptions).toEqual([
      { id: first.body.id, email: 'ann@example.org', createdAt: '2026-10-07T08:00:00.000Z' },
    ]);
  });

  it('refuses something that is not an address', async () => {
    expect((await subscribe('not an address')).status).toBe(400);
    expect(
      (
        await request(api.app)
          .post(`/api/reports/${reportId}/subscriptions`)
          .set('Authorization', api.fullAccess)
          .send({ email: { $ne: '' } })
      ).status,
    ).toBe(400);
  });

  it('delivers a report to its subscribers', async () => {
    await subscribe('ann@example.org');
    await subscribe('bob@example.org');

    const response = await request(api.app)
      .post(`/api/reports/${reportId}/deliveries`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app)
      .post('/api/reports/00000000-0000-4000-8000-000000000000/deliveries')
      .set('Authorization', api.fullAccess)
      .expect(404);

    expect(response.body).toEqual({ delivered: 2, bounced: 0 });
    expect(api.deliveries).toEqual([{ reportId, recipients: 2 }]);
  });

  it('switches the e-mail channel in the CRM', async () => {
    const id = '1e2d3c4b-5a69-4788-9a0b-c1d2e3f4a5b6';

    await request(api.app)
      .put(`/api/subscriptions/${id}/channels/email`)
      .set('Authorization', api.fullAccess)
      .send({ enabled: false })
      .expect(204);

    expect(api.channelChanges).toEqual([{ id, enabled: false }]);
  });

  it('unsubscribes with the address and the token from the mail', async () => {
    await subscribe('ann@example.org');
    const token = api.subscriptions.documents[0]?.unsubscribeToken;

    await request(api.app)
      .post('/unsubscribe')
      .send({ email: 'ann@example.org', token: 'wrong' })
      .expect(404);
    await request(api.app).post('/unsubscribe').send({ email: 'ann@example.org' }).expect(400);
    await request(api.app)
      .post('/unsubscribe')
      .send({ email: 'ann@example.org', token })
      .expect(204);

    expect(api.subscriptions.documents).toHaveLength(0);
  });
});

describe('preferences', () => {
  it('starts from the defaults and merges changes', async () => {
    const before = await request(api.app)
      .get('/api/preferences')
      .set('Authorization', api.fullAccess)
      .expect(200);
    const changed = await request(api.app)
      .patch('/api/preferences')
      .set('Authorization', api.fullAccess)
      .send({ search: { pageSize: 50 } })
      .expect(200);
    const after = await request(api.app)
      .get('/api/preferences')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(before.body.search.pageSize).toBe(25);
    expect(changed.body.search).toEqual({ pageSize: 50, defaultSort: 'newest', collections: [] });
    expect(after.body).toEqual(changed.body);
  });

  it('refuses a change that is not a valid preference', async () => {
    await request(api.app)
      .patch('/api/preferences')
      .set('Authorization', api.fullAccess)
      .send({ search: { pageSize: 5 } })
      .expect(400);
    await request(api.app)
      .patch('/api/preferences')
      .set('Authorization', api.fullAccess)
      .send({ theme: 'dark' })
      .expect(400);
    await request(api.app)
      .patch('/api/preferences')
      .set('Authorization', api.fullAccess)
      .send([1])
      .expect(400);
  });
});
