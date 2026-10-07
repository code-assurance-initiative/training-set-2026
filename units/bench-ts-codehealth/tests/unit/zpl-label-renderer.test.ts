import { describe, expect, it } from 'vitest';
import type { LabelData } from '../../src/application/labels/label-data.js';
import { ZplLabelRenderer } from '../../src/application/labels/zpl-label-renderer.js';
import { aarhus, copenhagen } from '../support/builders.js';

const label: LabelData = {
  carrier: 'alder',
  carrierName: 'Alder Parcel',
  serviceCode: 'ALD-STD',
  serviceName: 'Standard',
  trackingNumber: 'ALD0000000042',
  sender: copenhagen,
  recipient: aarhus,
  weightKg: 2,
  pieceNumber: 1,
  pieceCount: 1,
  zone: 'Z1',
  shipDate: new Date('2026-10-07T00:00:00.000Z'),
  reference: 'PO-1234',
  instructions: ['Leave at the back door'],
  customs: false,
};

describe('ZplLabelRenderer', () => {
  const renderer = new ZplLabelRenderer();

  it('renders a complete label', () => {
    const zpl = renderer.render(label);
    expect(zpl.startsWith('^XA')).toBe(true);
    expect(zpl.endsWith('^XZ')).toBe(true);
    expect(zpl).toContain('^FD>;ALD0000000042^FS');
    expect(zpl).toContain('^FDALD0 0000 0004 2^FS');
    expect(zpl).toContain('^FDDK-8000^FS');
    expect(zpl).toContain('^FDPO-1234^FS');
    expect(zpl).toContain('^FDNONE^FS');
  });

  it('strips ZPL command and control characters from field data', () => {
    const zpl = renderer.render({ ...label, reference: 'A^B~C\u0007D' });
    expect(zpl).toContain('^FDA B CD^FS');
  });

  it('shortens text to the field width', () => {
    expect(renderer.fitToWidth('Klostergade 3', 120, 10)).toBe('Kloster');
    expect(renderer.truncateReference('PO-123456', 40, 10)).toBe('PO-1');
    expect(renderer.truncateReference('PO-1', 0, 10)).toBe('');
  });

  it.skip('prints the customs box for a CN22 shipment', () => {
    expect(renderer.render({ ...label, customs: true })).toContain('^FDCN23^FS');
  });
});
