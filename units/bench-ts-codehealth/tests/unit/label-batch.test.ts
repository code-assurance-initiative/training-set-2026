import { describe, expect, it } from 'vitest';
import { LabelBatch } from '../../src/domain/entities/label-batch.js';
import { Shipment } from '../../src/domain/entities/shipment.js';
import { aarhus, copenhagen, parcel } from '../support/builders.js';

const at = new Date('2026-10-07T10:00:00.000Z');

describe('LabelBatch', () => {
  it('collects labels once each and raises BatchClosed when closed', () => {
    const batch = new LabelBatch('b1', 'zebra-dock-1', 2);
    batch.add({ shipmentId: 's1', trackingNumber: 'ALD0000000001' });
    batch.add({ shipmentId: 's1', trackingNumber: 'ALD0000000001' });
    batch.close(at);
    batch.close(at);
    expect(batch.labels).toHaveLength(1);
    expect(batch.isClosed).toBe(true);
    expect(batch.pullEvents()).toEqual([
      { type: 'batch-closed', batchId: 'b1', labelCount: 1, occurredAt: at },
    ]);
    expect(batch.pullEvents()).toEqual([]);
  });

  it('refuses labels when full or closed, and refuses to close empty', () => {
    const batch = new LabelBatch('b2', 'zebra-dock-1', 1);
    expect(() => {
      batch.close(at);
    }).toThrow(/empty/);
    batch.add({ shipmentId: 's1', trackingNumber: 'ALD0000000001' });
    expect(batch.isFull).toBe(true);
    expect(() => {
      batch.add({ shipmentId: 's2', trackingNumber: 'ALD0000000002' });
    }).toThrow(/full/);
    batch.close(at);
    expect(() => {
      batch.add({ shipmentId: 's3', trackingNumber: 'ALD0000000003' });
    }).toThrow(/closed/);
  });

  it('hands out a copy of its labels', () => {
    const batch = new LabelBatch('b3', 'zebra-dock-1', 5);
    batch.add({ shipmentId: 's1', trackingNumber: 'ALD0000000001' });
    (batch.labels as unknown as unknown[]).length = 0;
    expect(batch.labels).toHaveLength(1);
    expect(() => new LabelBatch('b4', 'p', 0)).toThrow(RangeError);
  });
});

describe('Shipment', () => {
  it('knows whether it crosses a border', () => {
    const shipment = new Shipment('s1', 'alder', 'standard', copenhagen, aarhus, [parcel()], at);
    expect(shipment.isInternational).toBe(false);
    expect(shipment.status).toBe('draft');
  });
});
