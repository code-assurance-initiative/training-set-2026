import request from 'supertest';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { documentId } from '../support/documents.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let api: TestApp;

beforeEach(async () => {
  api = await createTestApp();
});

afterEach(() => {
  vi.restoreAllMocks();
});

describe('exports', () => {
  it('creates an export of a document', async () => {
    const response = await request(api.app)
      .post(`/api/documents/${documentId}/exports`)
      .set('Authorization', api.fullAccess)
      .send({ format: 'pdf', fileName: 'handbook.pdf' })
      .expect(201);

    expect(response.headers.location).toBe(`/api/exports/${String(response.body.id)}`);
    expect(api.exports.created).toEqual([
      {
        caller: 'archivist-4',
        id: documentId,
        request: { format: 'pdf', fileName: 'handbook.pdf' },
      },
    ]);
  });

  it('validates the request and answers 404 for unknown documents and exports', async () => {
    await request(api.app)
      .post(`/api/documents/${documentId}/exports`)
      .set('Authorization', api.fullAccess)
      .send({ format: 'pdf', fileName: 'h.pdf', password: 'short' })
      .expect(400);
    await request(api.app)
      .post('/api/documents/00000000-0000-4000-8000-000000000000/exports')
      .set('Authorization', api.fullAccess)
      .send({ format: 'pdf', fileName: 'h.pdf' })
      .expect(404);
    await request(api.app)
      .post('/api/exports/00000000-0000-4000-8000-000000000000/download')
      .set('Authorization', api.fullAccess)
      .send({})
      .expect(404);
  });
});

describe('thumbnails', () => {
  it('renders the first page of a document', async () => {
    const response = await request(api.app)
      .get(`/api/documents/${documentId}/thumbnail?size=small`)
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.headers['content-type']).toBe('image/png');
    await request(api.app)
      .get(`/api/documents/${documentId}/thumbnail?size=huge`)
      .set('Authorization', api.fullAccess)
      .expect(400);
    await request(api.app)
      .get('/api/documents/00000000-0000-4000-8000-000000000000/thumbnail')
      .set('Authorization', api.fullAccess)
      .expect(404);
  });
});

describe('imports', () => {
  it('previews an import by URL', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(null, {
        status: 200,
        headers: {
          'content-type': 'application/pdf',
          'content-length': '2048',
          'last-modified': 'Tue, 06 Oct 2026 10:00:00 GMT',
        },
      }),
    );

    const response = await request(api.app)
      .get('/api/imports/preview?url=https://records.partner.test/batch/7.pdf')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.body).toEqual({
      status: 200,
      contentType: 'application/pdf',
      contentLength: 2048,
      lastModified: 'Tue, 06 Oct 2026 10:00:00 GMT',
    });
    expect(fetchSpy).toHaveBeenCalledOnce();
  });

  it('refuses a preview of something that is not a web URL', async () => {
    await request(api.app)
      .get('/api/imports/preview?url=file:///etc/passwd')
      .set('Authorization', api.fullAccess)
      .expect(400);
  });

  it('imports metadata XML', async () => {
    const response = await request(api.app)
      .post('/api/imports/metadata')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/xml')
      .send('<metadata><title>Board minutes</title></metadata>')
      .expect(200);

    expect(response.body).toEqual({ fields: [{ name: 'title', value: 'Board minutes' }] });
  });

  it('refuses metadata that is not XML, not metadata, or malformed', async () => {
    await request(api.app)
      .post('/api/imports/metadata')
      .set('Authorization', api.fullAccess)
      .send({ a: 1 })
      .expect(415);
    await request(api.app)
      .post('/api/imports/metadata')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/xml')
      .send('<catalogue/>')
      .expect(400);
    await request(api.app)
      .post('/api/imports/metadata')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'text/xml')
      .send('<metadata>')
      .expect(400);
  });

  it('imports metadata JSON as flattened fields', async () => {
    const response = await request(api.app)
      .post('/api/imports/metadata.json')
      .set('Authorization', api.fullAccess)
      .send({ creator: { name: 'Records Office' } })
      .expect(200);
    await request(api.app)
      .post('/api/imports/metadata.json')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'text/plain')
      .send('creator=x')
      .expect(415);

    expect(response.body).toEqual({ fields: { 'creator.name': 'Records Office' } });
  });

  it('reads a retention schedule', async () => {
    const response = await request(api.app)
      .post('/api/imports/retention-schedule')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/xml')
      .send('<retention-schedule><rule series="HR" years="10"/></retention-schedule>')
      .expect(200);
    await request(api.app)
      .post('/api/imports/retention-schedule')
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/xml')
      .send('<retention-schedule><rule series="HR" years="x"/></retention-schedule>')
      .expect(400);
    await request(api.app)
      .post('/api/imports/retention-schedule')
      .set('Authorization', api.fullAccess)
      .send({})
      .expect(415);

    expect(response.body).toEqual({ rules: [{ series: 'HR', years: 10 }] });
  });
});

describe('partner feeds', () => {
  it('lists the entries of a partner feed and explains a refusal', async () => {
    const response = await request(api.app)
      .get('/api/feeds/entries?url=https://feeds.partner.test/transfers.atom')
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app)
      .get('/api/feeds/entries?url=http://feeds.partner.test/transfers.atom')
      .set('Authorization', api.fullAccess)
      .expect(400);
    await request(api.app)
      .get('/api/feeds/entries?url=nope')
      .set('Authorization', api.fullAccess)
      .expect(400);

    expect(response.body.entries).toHaveLength(1);
  });
});
