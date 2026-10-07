import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { sampleDocument } from '../support/documents.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let api: TestApp;

beforeEach(async () => {
  api = await createTestApp();
  api.hits.push(
    { ...sampleDocument, id: 'a', title: 'Board minutes 1998', collection: 'BOARD' },
    { ...sampleDocument, id: 'b', title: 'Board minutes 2024', collection: 'HR' },
  );
});

async function create(body: object): Promise<request.Response> {
  return request(api.app)
    .post('/api/saved-searches')
    .set('Authorization', api.fullAccess)
    .send(body);
}

describe('saved searches', () => {
  it('saves a search with a filter tree and runs it', async () => {
    const created = await create({
      name: 'HR board minutes',
      term: 'Board',
      tree: { all: [{ field: 'collection', equals: 'HR' }] },
    });
    expect(created.status).toBe(201);

    const run = await request(api.app)
      .post(`/api/saved-searches/${String(created.body.id)}/run`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    const list = await request(api.app)
      .get('/api/saved-searches')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(run.body.hits.map((hit: { id: string }) => hit.id)).toEqual(['b']);
    expect(list.body.savedSearches).toHaveLength(1);
  });

  it('saves a search with a filter expression and runs it', async () => {
    const created = await create({
      name: 'Old minutes',
      term: 'Board',
      expression: "doc.title.endsWith('1998')",
    });

    const run = await request(api.app)
      .post(`/api/saved-searches/${String(created.body.id)}/run`)
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(run.body.hits.map((hit: { id: string }) => hit.id)).toEqual(['a']);
  });

  it('refuses invalid filters', async () => {
    expect(
      (await create({ name: 'x', term: 'y', tree: { field: 'owner', equals: 'me' } })).status,
    ).toBe(400);
    expect((await create({ name: 'x', term: 'y', expression: 'doc.title ===' })).status).toBe(400);
    expect((await create({ name: '', term: 'y' })).status).toBe(400);
  });

  it('answers 404 for a search of someone else', async () => {
    const created = await create({ name: 'Mine', term: 'Board' });
    const other = await api.tokens.issue({ scopes: ['documents.read'] });
    api.savedSearches[0] = {
      ...api.savedSearches[0],
      owner: 'archivist-9',
    } as (typeof api.savedSearches)[number];

    await request(api.app)
      .post(`/api/saved-searches/${String(created.body.id)}/run`)
      .auth(other, { type: 'bearer' })
      .expect(404);
    await request(api.app)
      .post('/api/saved-searches/x/run')
      .auth(other, { type: 'bearer' })
      .expect(400);
  });

  it('opens a saved search and returns to a page of this site only', async () => {
    const id = '7c1a7a52-91f4-4f4b-8d3a-2b1c0d9e8f7a';

    const local = await request(api.app)
      .get(`/api/saved-searches/${id}/open?returnTo=${encodeURIComponent('/documents?q=board')}`)
      .set('Authorization', api.fullAccess)
      .expect(303);
    const foreign = await request(api.app)
      .get(`/api/saved-searches/${id}/open?returnTo=${encodeURIComponent('//attacker.test/')}`)
      .set('Authorization', api.fullAccess)
      .expect(303);
    await request(api.app)
      .get('/api/saved-searches/x/open')
      .set('Authorization', api.fullAccess)
      .expect(400);

    expect(local.headers.location).toBe('/documents?q=board');
    expect(foreign.headers.location).toBe(`/searches/${id}`);
    expect(api.opened).toEqual([id, id]);
  });
});
