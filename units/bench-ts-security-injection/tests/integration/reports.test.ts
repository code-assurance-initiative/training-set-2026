import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { reportId } from '../support/documents.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let api: TestApp;

beforeEach(async () => {
  api = await createTestApp();
});

describe('reports', () => {
  it('lists the reports of an owner', async () => {
    const response = await request(api.app)
      .get('/api/reports?owner=archivist-4')
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app).get('/api/reports').set('Authorization', api.fullAccess).expect(400);

    expect(response.body.reports).toHaveLength(1);
  });

  it('lists the schedules of a report', async () => {
    const response = await request(api.app)
      .get(`/api/reports/${reportId}/schedules?state=active`)
      .set('Authorization', api.fullAccess)
      .expect(200);
    await request(api.app)
      .get(`/api/reports/${reportId}/schedules?state=sometimes`)
      .set('Authorization', api.fullAccess)
      .expect(400);
    await request(api.app)
      .get('/api/reports/00000000-0000-4000-8000-000000000000/schedules')
      .set('Authorization', api.fullAccess)
      .expect(404);

    expect(response.body).toEqual({ schedules: [] });
  });

  it('previews a report with computed columns, sorting and settings', async () => {
    const response = await request(api.app)
      .post(`/api/reports/${reportId}/preview`)
      .set('Authorization', api.fullAccess)
      .send({
        computed: [{ name: 'pages', formula: 'pageCount ?? 0' }],
        sort: { column: 'title', direction: 'asc' },
        settings: { orientation: 'landscape' },
      })
      .expect(200);

    expect(response.body.settings.orientation).toBe('landscape');
    expect(
      response.body.rows.map((row: { title: string; pages: number }) => [row.title, row.pages]),
    ).toEqual([
      ['Leave policy', 11],
      ['Onboarding', 0],
      ['Pay scales', 3],
    ]);
  });

  it('answers 400 for a failing formula or invalid settings', async () => {
    await request(api.app)
      .post(`/api/reports/${reportId}/preview`)
      .set('Authorization', api.fullAccess)
      .send({ computed: [{ name: 'x', formula: 'missing.field' }] })
      .expect(400);
    await request(api.app)
      .post(`/api/reports/${reportId}/preview`)
      .set('Authorization', api.fullAccess)
      .send({ settings: { fontSize: 99 } })
      .expect(400);
    await request(api.app)
      .post(`/api/reports/${reportId}/preview`)
      .set('Authorization', api.fullAccess)
      .send({ sort: { column: 'secret', direction: 'asc' } })
      .expect(400);
  });

  it('renders a report through an uploaded layout', async () => {
    const response = await request(api.app)
      .post(`/api/reports/${reportId}/render`)
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/yaml')
      .send('title: Intake\ncolumns:\n  - field: number\n    heading: Number\n')
      .expect(200);
    await request(api.app)
      .post(`/api/reports/${reportId}/render`)
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'application/yaml')
      .send('columns: 3')
      .expect(400);
    await request(api.app)
      .post(`/api/reports/${reportId}/render`)
      .set('Authorization', api.fullAccess)
      .send({})
      .expect(415);

    expect(response.body).toEqual({
      title: 'Intake',
      headings: ['Number'],
      rows: [['HR-2024-000002'], ['HR-2024-000001'], ['HR-2024-000003']],
    });
  });

  it('remaps report rows through a column mapping', async () => {
    const response = await request(api.app)
      .post(`/api/reports/${reportId}/remap`)
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'text/yaml')
      .send('source: partner-a\ncolumns:\n  title: name\n')
      .expect(200);
    await request(api.app)
      .post(`/api/reports/${reportId}/remap`)
      .set('Authorization', api.fullAccess)
      .set('Content-Type', 'text/yaml')
      .send('source: [')
      .expect(400);

    expect(response.body.rows[0]).toEqual({ name: 'Pay scales' });
  });

  it('answers 404 for an unknown report and 400 for a malformed id', async () => {
    await request(api.app)
      .post('/api/reports/00000000-0000-4000-8000-000000000000/preview')
      .set('Authorization', api.fullAccess)
      .send({})
      .expect(404);
    await request(api.app)
      .post('/api/reports/x/preview')
      .set('Authorization', api.fullAccess)
      .send({})
      .expect(400);
  });
});
