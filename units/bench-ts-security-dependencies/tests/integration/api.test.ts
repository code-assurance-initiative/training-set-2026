import JSZip from 'jszip';
import request from 'supertest';
import { beforeEach, describe, expect, it } from 'vitest';
import { LinehaulError } from '../../src/carriers/linehaul-client.js';
import type { DispatchRun } from '../../src/dispatch/dispatch-run.js';
import { parcel } from '../support/parcels.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

const allScopes = ['dispatch:read', 'dispatch:write', 'labels:print'];
const newRun = {
  depotId: 'AAR',
  serviceDate: '2026-11-02',
  parcels: [
    parcel('AB00000001', 'Aarhus C'),
    parcel('AB00000002', 'Viby J', 'Skanderborgvej 10', '8260'),
  ],
};

let t: TestApp;
let bearer: string;

beforeEach(() => {
  t = createTestApp();
  bearer = `Bearer ${t.tokens.issue({ scopes: allScopes })}`;
});

async function createRun(): Promise<DispatchRun> {
  const response = await request(t.app)
    .post('/api/runs')
    .set('Authorization', bearer)
    .send(newRun)
    .expect(201);
  return response.body as DispatchRun;
}

describe('health', () => {
  it('answers without a token, with security headers', async () => {
    const response = await request(t.app).get('/health').expect(200, { status: 'ok' });
    expect(response.headers['content-security-policy']).toBe(
      "default-src 'none';frame-ancestors 'none'",
    );
    expect(response.headers['x-content-type-options']).toBe('nosniff');
    expect(response.headers['strict-transport-security']).toMatch(/max-age=/);
    expect(response.headers['x-powered-by']).toBeUndefined();
  });
});

describe('dispatch runs', () => {
  it('needs a terminal token', async () => {
    const response = await request(t.app).post('/api/runs').send(newRun).expect(401);
    expect(response.headers['www-authenticate']).toBe('Bearer');
    await request(t.app).get('/api/runs/x').set('Authorization', 'Bearer nonsense').expect(401);
    await request(t.app).get('/api/runs/x').set('Authorization', 'Basic abc').expect(401);
  });

  it('needs the write scope to plan a run', async () => {
    const readOnly = `Bearer ${t.tokens.issue({ scopes: ['dispatch:read'] })}`;
    const response = await request(t.app)
      .post('/api/runs')
      .set('Authorization', readOnly)
      .send(newRun);
    expect(response.status).toBe(403);
    expect(response.body).toMatchObject({
      detail: 'This operation requires the dispatch:write scope.',
    });
  });

  it('plans, returns and exports a run of the terminal’s depot', async () => {
    const response = await request(t.app)
      .post('/api/runs')
      .set('Authorization', bearer)
      .send(newRun)
      .expect(201);
    const run = response.body as DispatchRun;
    expect(response.headers.location).toBe(`/api/runs/${run.id}`);
    expect(run.routes.map((route) => route.city)).toEqual(['Aarhus C', 'Viby J']);

    await request(t.app).get(`/api/runs/${run.id}`).set('Authorization', bearer).expect(200, run);
    const exported = await request(t.app)
      .get(`/api/runs/${run.id}/export`)
      .set('Authorization', bearer)
      .expect(200);
    expect(exported.headers['content-disposition']).toBe(
      'attachment; filename="run-2026-11-02-AAR.json"',
    );
    expect(exported.body).toEqual(run);
  });

  it('refuses a run for another depot', async () => {
    await request(t.app)
      .post('/api/runs')
      .set('Authorization', bearer)
      .send({ ...newRun, depotId: 'ODE' })
      .expect(403);
  });

  it('hides another depot’s run', async () => {
    const run = await createRun();
    const odense = `Bearer ${t.tokens.issue({ depot: 'ODE', scopes: allScopes })}`;
    await request(t.app).get(`/api/runs/${run.id}`).set('Authorization', odense).expect(404);
    await request(t.app).get('/api/runs/not-a-uuid').set('Authorization', bearer).expect(404);
  });

  it.each([
    [{ ...newRun, parcels: [] }, /^parcels: /],
    [{ ...newRun, parcels: [newRun.parcels[0], newRun.parcels[0]] }, /unique/],
    [{ ...newRun, serviceDate: '2/11/2026' }, /^serviceDate: /],
  ])('validates the request body', async (body, detail) => {
    const response = await request(t.app)
      .post('/api/runs')
      .set('Authorization', bearer)
      .send(body)
      .expect(400);
    expect((response.body as { detail: string }).detail).toMatch(detail);
  });

  it('refuses a weekend', async () => {
    const response = await request(t.app)
      .post('/api/runs')
      .set('Authorization', bearer)
      .send({ ...newRun, serviceDate: '2026-11-07' })
      .expect(400);
    expect((response.body as { detail: string }).detail).toMatch(/weekend/);
  });

  it('names parcels outside the service area', async () => {
    const response = await request(t.app)
      .post('/api/runs')
      .set('Authorization', bearer)
      .send({ ...newRun, parcels: [parcel('AB00000009', 'Skagen')] })
      .expect(422);
    expect(response.body).toMatchObject({ unmatched: ['AB00000009'] });
  });

  it('refuses a run the depot has closed', async () => {
    const late = createTestApp('2026-11-02T07:00:00Z');
    const token = `Bearer ${late.tokens.issue({ scopes: allScopes })}`;
    const response = await request(late.app)
      .post('/api/runs')
      .set('Authorization', token)
      .send(newRun);
    expect(response.status).toBe(422);
    expect(response.body).not.toHaveProperty('unmatched');
  });

  it('quotes the linehaul for the run', async () => {
    const run = await createRun();
    const response = await request(t.app)
      .post(`/api/runs/${run.id}/quote`)
      .set('Authorization', bearer)
      .expect(200);
    expect(response.body).toMatchObject({ quoteId: 'Q-1001', currency: 'DKK' });
    expect(t.linehaul.quotes).toEqual([
      { depotId: 'AAR', serviceDate: '2026-11-02', parcels: 2, totalWeightKg: 5 },
    ]);
  });

  it('answers 404 for a quote of an unknown run', async () => {
    await request(t.app)
      .post('/api/runs/7d1e6c3a-2f0b-4e57-9a51-0c6f8a2b9e10/quote')
      .set('Authorization', bearer)
      .expect(404);
  });

  it('answers a malformed body and an unknown path with problem documents', async () => {
    const malformed = await request(t.app)
      .post('/api/runs')
      .set('Authorization', bearer)
      .set('Content-Type', 'application/json')
      .send('{"depotId":');
    expect(malformed.status).toBe(400);
    expect(malformed.headers['content-type']).toMatch(/^application\/problem\+json/);
    await request(t.app).get('/nowhere').expect(404);
  });
});

describe('upstream failures', () => {
  it('answers 502 when the carrier fails and 500 for anything unexpected', async () => {
    const run = await createRun();
    t.linehaul.failWith(new LinehaulError('The carrier answered with HTTP 503.'));
    const quote = await request(t.app)
      .post(`/api/runs/${run.id}/quote`)
      .set('Authorization', bearer);
    expect(quote.status).toBe(502);
    expect(quote.body).toMatchObject({ title: 'Bad Gateway' });

    t.linehaul.failWith(new TypeError('bug'));
    const labels = await request(t.app)
      .get(`/api/runs/${run.id}/labels.zip`)
      .set('Authorization', bearer);
    expect(labels.status).toBe(500);
    expect(labels.body).toMatchObject({ detail: 'The request could not be processed.' });
  });
});

describe('labels', () => {
  it('bundles the rasterised labels of a run', async () => {
    const run = await createRun();
    const response = await request(t.app)
      .get(`/api/runs/${run.id}/labels.zip`)
      .set('Authorization', bearer)
      .buffer(true)
      .parse((res, callback) => {
        const chunks: Buffer[] = [];
        res.on('data', (chunk: Buffer) => chunks.push(chunk));
        res.on('end', () => {
          callback(null, Buffer.concat(chunks));
        });
      })
      .expect(200);

    expect(response.headers['content-type']).toBe('application/zip');
    const zip = await JSZip.loadAsync(response.body as Buffer);
    expect(Object.keys(zip.files).sort()).toEqual([
      'index.csv',
      'run.txt',
      'vehicle-1/',
      'vehicle-1/001-AB00000001.png',
      'vehicle-2/',
      'vehicle-2/002-AB00000002.png',
    ]);
    expect(t.linehaul.labelsFetched).toEqual(['AB00000001', 'AB00000002']);
  });

  it('needs the print scope', async () => {
    const run = await createRun();
    const desk = `Bearer ${t.tokens.issue({ scopes: ['dispatch:read'] })}`;
    await request(t.app)
      .get(`/api/runs/${run.id}/labels.zip`)
      .set('Authorization', desk)
      .expect(403);
  });

  it('answers 404 for an unknown run', async () => {
    await request(t.app)
      .get('/api/runs/7d1e6c3a-2f0b-4e57-9a51-0c6f8a2b9e10/labels.zip')
      .set('Authorization', bearer)
      .expect(404);
  });
});

describe('carrier status webhook', () => {
  const feed = (tracking: string) =>
    `<statusFeed><event tracking="${tracking}" code="DLV" at="2026-11-02T09:41:00+01:00"/></statusFeed>`;

  it('applies a signed feed to the run', async () => {
    const run = await createRun();
    const body = feed('AB00000002');

    await request(t.app)
      .post('/webhooks/carrier-status')
      .set('Content-Type', 'application/xml')
      .set('X-Signature', t.carrier.sign(body))
      .send(body)
      .expect(202, { received: 1, applied: 1 });

    const stored = t.services.store.find(run.id);
    expect(stored?.stops.find((stop) => stop.trackingNumber === 'AB00000002')?.status).toBe(
      'delivered',
    );
  });

  it('refuses an unsigned or re-signed feed', async () => {
    const body = feed('AB00000002');
    await request(t.app)
      .post('/webhooks/carrier-status')
      .set('Content-Type', 'application/xml')
      .send(body)
      .expect(401);
    await request(t.app)
      .post('/webhooks/carrier-status')
      .set('Content-Type', 'application/xml')
      .set('X-Signature', t.carrier.sign(feed('AB00000001')))
      .send(body)
      .expect(401);
  });

  it('refuses a feed that is not XML', async () => {
    await request(t.app)
      .post('/webhooks/carrier-status')
      .set('Content-Type', 'application/json')
      .send('{}')
      .expect(415);
  });

  it('refuses a signed feed it cannot read', async () => {
    const body = '<statusFeed><event';
    await request(t.app)
      .post('/webhooks/carrier-status')
      .set('Content-Type', 'text/xml')
      .set('X-Signature', t.carrier.sign(body))
      .send(body)
      .expect(400);
  });
});
