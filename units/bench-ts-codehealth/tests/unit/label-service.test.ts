import { describe, expect, it } from 'vitest';
import { LabelValidationError } from '../../src/application/labels/label-service.js';
import { addressBody, parcelBody, senderBody } from '../support/builders.js';
import { createTestApp } from '../support/test-app.js';

const request = {
  serviceLevel: 'standard',
  sender: senderBody,
  recipient: addressBody,
  parcels: [parcelBody],
  contact: { email: 'mette@example.test' },
};

describe('LabelService', () => {
  it('creates a label for a domestic parcel', async () => {
    const { services } = await createTestApp();
    await services.labels.createLabel(request);
  });

  it('picks the cheapest carrier, renders, stores and e-mails the label', async () => {
    const { services, store, mailer, carriers } = await createTestApp();
    const created = await services.labels.createLabel(request);
    expect(created.carrier).toBe('corvid');
    expect(created.trackingNumber).toMatch(/^CVD\d{10}$/);
    expect(created.shipDate).toBe('2026-10-07');
    expect(carriers.corvid.labels).toHaveLength(1);
    expect(store.files.get(created.shipmentId)).toContain('^POI');
    expect(mailer.sent[0]?.to).toBe('mette@example.test');
    expect(mailer.sent[0]?.message).toContain('Content-Type: application/zpl');
    expect(services.labels.statistics()).toMatchObject({ created: 1, emailed: 1 });
  });

  it('renders its own label when the carrier sends none', async () => {
    const { services, store } = await createTestApp();
    const created = await services.labels.createLabel({
      ...request,
      carrier: 'alder',
      reference: 'PO-77',
    });
    const zpl = store.files.get(created.shipmentId) ?? '';
    expect(zpl).toContain('^FDALD-STD^FS');
    expect(zpl).toContain('PO-77');
    expect(await services.labels.reprint(created.shipmentId)).toBe(zpl);
  });

  it('reports every problem of an invalid request', async () => {
    const { services } = await createTestApp();
    const error = await services.labels
      .createLabel({ serviceLevel: 'overnight', sender: {}, parcels: [{ weightGrams: 0 }] })
      .catch((e: unknown) => e);
    expect(error).toBeInstanceOf(LabelValidationError);
    expect((error as LabelValidationError).problems).toEqual(
      expect.arrayContaining([
        'serviceLevel must be one of economy, standard, express',
        'sender.name is required',
        'recipient is required',
        'parcels[0].weightGrams must be 1-70000',
      ]),
    );
  });

  it('checks country postcode formats and an unknown carrier', async () => {
    const { services } = await createTestApp();
    const germany = { ...addressBody, country: 'DE', postcode: '123' };
    await expect(services.labels.createLabel({ ...request, recipient: germany })).rejects.toThrow(
      /five digits in Germany/,
    );
    await expect(services.labels.createLabel({ ...request, carrier: 'owl' })).rejects.toThrow(
      /unknown carrier/,
    );
  });

  it('voids a label once and archives it', async () => {
    const { services, carriers } = await createTestApp();
    const created = await services.labels.createLabel({ ...request, carrier: 'alder' });
    await services.labels.archive(created.shipmentId);
    await services.labels.voidLabel(created.shipmentId);
    await services.labels.voidLabel(created.shipmentId);
    expect(carriers.alder.voided).toEqual([created.trackingNumber]);
    await expect(services.labels.reprint(created.shipmentId)).rejects.toThrow(LabelValidationError);
    expect(services.labels.statistics().voided).toBe(1);
  });

  it('batches labels per printer and closes a batch', async () => {
    const { services } = await createTestApp();
    const first = await services.labels.createLabel(request);
    const second = await services.labels.createLabel(request);
    const batch = services.labels.addToBatch(first.shipmentId, 'zebra-dock-1');
    expect(services.labels.addToBatch(second.shipmentId, 'zebra-dock-1')).toBe(batch);
    expect(services.labels.closeBatch(batch.id).labels).toHaveLength(2);
    expect(() => services.labels.addToBatch('missing', 'zebra-dock-1')).toThrow(
      LabelValidationError,
    );
  });

  it('writes a customs declaration outside the EU and finds labels by tracking number', async () => {
    const { services } = await createTestApp();
    const norway = { ...addressBody, country: 'NO', postcode: '0150', city: 'Oslo' };
    const created = await services.labels.createLabel({
      ...request,
      recipient: norway,
      parcels: [{ ...parcelBody, declaredValue: 250 }],
    });
    expect(services.labels.customsDeclaration(created.shipmentId)).toContain(
      'Total value: 250.00 DKK',
    );
    const domestic = await services.labels.createLabel(request);
    expect(services.labels.customsDeclaration(domestic.shipmentId)).toEqual([]);
    expect((await services.labels.findByTrackingNumber(created.trackingNumber))?.shipmentId).toBe(
      created.shipmentId,
    );
  });

  it('applies tracking events and exports and purges shipments', async () => {
    const { services } = await createTestApp();
    const created = await services.labels.createLabel(request);
    expect(
      services.labels.applyTrackingEvent({
        trackingNumber: created.trackingNumber,
        status: 'delivered',
      }),
    ).toBe(true);
    expect(
      services.labels.applyTrackingEvent({ trackingNumber: 'nope', status: 'delivered' }),
    ).toBe(false);
    expect(services.labels.applyTrackingEvent({ status: 'delivered' })).toBe(false);
    expect(services.labels.shipmentsByStatus()).toMatchObject({ archived: 1 });
    const csv = services.labels.exportShipmentsCsv(new Date('2026-10-01'), new Date('2026-10-31'));
    expect(csv.split('\n')).toHaveLength(2);
    expect(services.labels.statisticsCsv()).toContain('carrier_corvid,1');
    services.labels.resetStatistics();
    expect(services.labels.statistics().created).toBe(0);
    expect(services.labels.openShipments()).toEqual([]);
    expect(await services.labels.purgeExpired()).toBe(1);
  });

  it('normalises postcodes per country', async () => {
    const { services } = await createTestApp();
    expect(services.labels.normalisePostcode('SE', '11122')).toBe('111 22');
    expect(services.labels.normalisePostcode('NL', '1012ab')).toBe('1012 AB');
    expect(services.labels.normalisePostcode('GB', 'sw1a1aa')).toBe('SW1A 1AA');
    expect(services.labels.normalisePostcode('PL', '00950')).toBe('00-950');
    expect(services.labels.normalisePostcode('DK', '8 000')).toBe('8000');
  });
});
