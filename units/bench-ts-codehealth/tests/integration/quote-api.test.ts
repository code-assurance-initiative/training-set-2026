import request, { type Response } from 'supertest';
import { beforeAll, describe, expect, it } from 'vitest';
import { addressBody, parcelBody, senderBody } from '../support/builders.js';
import { createTestApp, type TestApp } from '../support/test-app.js';

let t: TestApp;

beforeAll(async () => {
  t = await createTestApp();
});

const body = {
  serviceLevel: 'standard',
  sender: senderBody,
  recipient: addressBody,
  parcels: [parcelBody],
};

function expectProblem(res: Response, status: number, path?: string): void {
  expect(res.status).toBe(status);
  expect(res.type).toBe('application/problem+json');
  if (path !== undefined) {
    expect((res.body as { errors: { path: string }[] }).errors.map((e) => e.path)).toContain(path);
  }
}

const quote = (payload: unknown) =>
  request(t.app)
    .post('/api/quotes')
    .set('Authorization', t.fullAccess)
    .send(payload as object);

describe('POST /api/quotes', () => {
  it('quotes both carriers and marks the cheapest and the fastest', async () => {
    const res = await quote(body);
    expect(res.status).toBe(200);
    const quotes = (
      res.body as {
        quotes: { carrier: string; cheapest: boolean; fastest: boolean; total: string }[];
      }
    ).quotes;
    expect(quotes.map((q) => q.carrier)).toEqual(['alder', 'corvid']);
    expect(quotes.find((q) => q.cheapest)?.carrier).toBe('corvid');
    expect(quotes.find((q) => q.fastest)?.carrier).toBe('corvid');
    expect((res.body as { handling: unknown[] }).handling).toHaveLength(1);
  });

  it('quotes one carrier with a company name and a second street line', async () => {
    const res = await quote({
      ...body,
      carrier: 'alder',
      recipient: { ...addressBody, company: 'Holm Design', street2: '2. tv' },
      parcels: [{ ...parcelBody, declaredValue: 1_000 }],
    });
    expect(res.status).toBe(200);
    expect(
      (res.body as { quotes: { surcharges: { code: string }[] }[] }).quotes[0]?.surcharges,
    ).toEqual([{ code: 'INSURANCE', amount: '15.00 DKK' }]);
  });

  it('rejects a parcel without dimensions', async () => {
    expectProblem(
      await quote({ ...body, parcels: [{ weightGrams: 1_000 }] }),
      400,
      'parcels.0.lengthCm',
    );
  });

  it('requires a token with the rates scope', async () => {
    expectProblem(await request(t.app).post('/api/quotes').send(body), 401);
    const token = await t.tokens.issue({ scopes: ['labels.write'] });
    expectProblem(
      await request(t.app).post('/api/quotes').set('Authorization', `Bearer ${token}`).send(body),
      403,
    );
  });
});

describe('the rest of the surface', () => {
  it('answers health anonymously and unknown paths with a problem', async () => {
    expect((await request(t.app).get('/health')).body).toEqual({ status: 'ok' });
    expectProblem(await request(t.app).get('/nowhere'), 404);
  });

  it('rejects a body over the size limit', async () => {
    const res = await request(t.app)
      .post('/api/quotes')
      .set('Authorization', t.fullAccess)
      .send({ ...body, padding: 'x'.repeat(70_000) });
    expectProblem(res, 413);
  });

  it('rejects a token for another audience and a malformed header', async () => {
    const token = await t.tokens.issue({ audience: 'someone-else', scopes: ['rates.read'] });
    expectProblem(
      await request(t.app).post('/api/quotes').set('Authorization', `Bearer ${token}`).send(body),
      401,
    );
    expectProblem(
      await request(t.app).post('/api/quotes').set('Authorization', 'Basic abc').send(body),
      401,
    );
  });

  it('rejects malformed JSON', async () => {
    expectProblem(
      await request(t.app)
        .post('/api/quotes')
        .set('Authorization', t.fullAccess)
        .set('Content-Type', 'application/json')
        .send('{"serviceLevel":'),
      400,
    );
  });
});

describe('transport security', () => {
  it('refuses plain HTTP when HTTPS is required', async () => {
    const { requireHttps } = await import('../../src/api/transport-security.js');
    const express = (await import('express')).default;
    const app = express().use(requireHttps(true), (_req, res) => {
      res.json({});
    });
    expectProblem(await request(app).get('/'), 403);
  });
});
