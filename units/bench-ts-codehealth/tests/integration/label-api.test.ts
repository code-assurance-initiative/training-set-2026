import { createHmac } from 'node:crypto';
import request from 'supertest';
import { describe, expect, it } from 'vitest';
import { CarrierError } from '../../src/infrastructure/carriers/carrier-error.js';
import { addressBody, parcelBody, senderBody } from '../support/builders.js';
import { createTestApp, webhookSecret } from '../support/test-app.js';

const body = {
  serviceLevel: 'standard',
  sender: senderBody,
  recipient: addressBody,
  parcels: [parcelBody],
};

describe('labels over HTTP', () => {
  it('creates, fetches and voids a label', async () => {
    const t = await createTestApp();
    const created = await request(t.app)
      .post('/api/labels')
      .set('Authorization', t.fullAccess)
      .send(body);
    expect(created.status).toBe(201);
    const { shipmentId, label } = created.body as { shipmentId: string; label: string };
    expect(created.headers.location).toBe(`/api/labels/${shipmentId}`);
    const zpl = await request(t.app).get(label).set('Authorization', t.fullAccess);
    expect(zpl.status).toBe(200);
    expect(zpl.text).toContain('^XA');
    expect((await request(t.app).delete(label).set('Authorization', t.fullAccess)).status).toBe(
      204,
    );
    const stats = await request(t.app)
      .get('/api/statistics/labels')
      .set('Authorization', t.fullAccess);
    expect(stats.body).toMatchObject({ created: 1, voided: 1 });
  });

  it('answers 400 for an invalid request and 502 when the carrier fails', async () => {
    const t = await createTestApp();
    const invalid = await request(t.app)
      .post('/api/labels')
      .set('Authorization', t.fullAccess)
      .send({ ...body, parcels: [] });
    expect(invalid.status).toBe(400);
    expect((invalid.body as { errors: string[] }).errors).toEqual([
      'parcels must be a non-empty list',
    ]);
    t.carriers.corvid.createLabel = () => Promise.reject(new CarrierError('corvid', 503, 'DOWN'));
    const failed = await request(t.app)
      .post('/api/labels')
      .set('Authorization', t.fullAccess)
      .send(body);
    expect(failed.status).toBe(502);
    expect(
      (await request(t.app).get('/api/labels/not-a-uuid').set('Authorization', t.fullAccess))
        .status,
    ).toBe(400);
  });

  it('applies signed tracking webhooks only', async () => {
    const t = await createTestApp();
    const created = await request(t.app)
      .post('/api/labels')
      .set('Authorization', t.fullAccess)
      .send(body);
    const payload = JSON.stringify({
      trackingNumber: (created.body as { trackingNumber: string }).trackingNumber,
      status: 'delivered',
    });
    const signature = `v1=${createHmac('sha256', webhookSecret).update(payload).digest('hex')}`;
    const post = (data: string, sig: string) =>
      request(t.app)
        .post('/webhooks/tracking')
        .set('Content-Type', 'application/json')
        .set('X-Signature', sig)
        .send(data);
    expect((await post(payload, signature)).status).toBe(204);
    expect((await post(payload, 'v1=00')).status).toBe(401);
    const unknown = JSON.stringify({ trackingNumber: 'X', status: 'delivered' });
    expect(
      (
        await post(
          unknown,
          `v1=${createHmac('sha256', webhookSecret).update(unknown).digest('hex')}`,
        )
      ).status,
    ).toBe(202);
    const broken = '{"trackingNumber":';
    expect(
      (await post(broken, `v1=${createHmac('sha256', webhookSecret).update(broken).digest('hex')}`))
        .status,
    ).toBe(400);
    expect(
      (await post('7', `v1=${createHmac('sha256', webhookSecret).update('7').digest('hex')}`))
        .status,
    ).toBe(400);
  });
});
