import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { documentId, sampleDocument } from '../support/documents.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let api: TestApp;

beforeEach(async () => {
  api = await createTestApp();
  api.hits.push({ ...sampleDocument, title: 'Board minutes <1998>' });
  api.texts.set(documentId, 'Board minutes of the board meeting');
});

describe('search', () => {
  it('returns a page of hits', async () => {
    const response = await request(api.app)
      .get('/api/search?q=Board&limit=5')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.body).toMatchObject({ offset: 0, limit: 5, hits: [{ id: documentId }] });
  });

  it('validates the query', async () => {
    await request(api.app)
      .get('/api/search?q=Board&sort=owner')
      .set('Authorization', api.fullAccess)
      .expect(400);
  });

  it('renders the widget with escaped titles', async () => {
    const response = await request(api.app)
      .get('/api/search/widget?q=Board')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.headers['content-type']).toMatch(/^text\/html/);
    expect(response.text).toContain('Board minutes &lt;1998&gt;');
    expect(response.text).toContain(`href="/documents/${documentId}"`);
  });

  it('renders an empty widget', async () => {
    const response = await request(api.app)
      .get('/api/search/widget?q=Maps')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(response.text).toContain('No documents found.');
  });

  it('highlights a term or a pattern in a document', async () => {
    const term = await request(api.app)
      .get(`/api/search/highlights?documentId=${documentId}&term=board`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    const pattern = await request(api.app)
      .get(`/api/search/highlights?documentId=${documentId}&pattern=${encodeURIComponent('m\\w+')}`)
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(term.body.highlights).toHaveLength(2);
    expect(pattern.body.highlights).toEqual([
      { start: 6, end: 13 },
      { start: 27, end: 34 },
    ]);
  });

  it('answers 404 for a document without text', async () => {
    await request(api.app)
      .get('/api/search/highlights?documentId=00000000-0000-4000-8000-000000000000&term=x')
      .set('Authorization', api.fullAccess)
      .expect(404);
  });
});

describe('documents', () => {
  it('reads a document by id and by number', async () => {
    const byId = await request(api.app)
      .get(`/api/documents/${documentId}`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app)
      .get('/api/documents/by-number/HR-2024-001337')
      .set('Authorization', api.fullAccess)
      .expect(200);

    expect(byId.body).toEqual({
      id: documentId,
      number: 'HR-2024-001337',
      title: sampleDocument.title,
      collection: 'HR',
      pageCount: 42,
      createdAt: '2024-03-01T09:30:00.000Z',
    });
  });

  it('answers 404 and 400 where it should', async () => {
    await request(api.app)
      .get('/api/documents/00000000-0000-4000-8000-000000000000')
      .set('Authorization', api.fullAccess)
      .expect(404);
    await request(api.app)
      .get('/api/documents/not-a-uuid')
      .set('Authorization', api.fullAccess)
      .expect(400);
    await request(api.app)
      .get('/api/documents/by-number/HR-2024-000404')
      .set('Authorization', api.fullAccess)
      .expect(404);
    await request(api.app)
      .get('/api/documents/by-number/hr')
      .set('Authorization', api.fullAccess)
      .expect(400);
    await request(api.app)
      .get('/api/documents/not-a-uuid/card')
      .set('Authorization', api.fullAccess)
      .expect(400);
    await request(api.app)
      .get('/api/documents/00000000-0000-4000-8000-000000000000/card')
      .set('Authorization', api.fullAccess)
      .expect(404);
  });

  it('serves the card with an ETag and revalidates it', async () => {
    const first = await request(api.app)
      .get(`/api/documents/${documentId}/card`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    const etag = String(first.headers.etag);

    expect(first.text).toContain('Staff handbook &lt;revised&gt;');
    await request(api.app)
      .get(`/api/documents/${documentId}/card`)
      .set('Authorization', api.fullAccess)
      .set('If-None-Match', etag)
      .expect(304);
  });
});

describe('attachments and templates', () => {
  it('serves an attachment of a document and 404 for a missing one', async () => {
    const folder = path.join(api.storageRoot, 'attachments', documentId);
    await mkdir(folder, { recursive: true });
    await writeFile(path.join(folder, 'scan.pdf'), '%PDF-1.7');

    const response = await request(api.app)
      .get(`/api/documents/${documentId}/attachments/scan.pdf`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app)
      .get(`/api/documents/${documentId}/attachments/missing.pdf`)
      .set('Authorization', api.fullAccess)
      .expect(404);
    await request(api.app)
      .get('/api/documents/x/attachments/scan.pdf')
      .set('Authorization', api.fullAccess)
      .expect(400);

    expect(response.headers['content-type']).toBe('application/pdf');
    expect(response.headers['content-disposition']).toBe('attachment; filename="scan.pdf"');
  });

  it('serves a template and refuses names outside the template directory', async () => {
    await mkdir(path.join(api.storageRoot, 'templates'), { recursive: true });
    await writeFile(path.join(api.storageRoot, 'templates', 'cover.txt'), 'Dear reader');

    const response = await request(api.app)
      .get('/api/templates/cover.txt')
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app)
      .get('/api/templates/..%2Fattachments')
      .set('Authorization', api.fullAccess)
      .expect(404);

    expect(response.text).toBe('Dear reader');
  });
});
