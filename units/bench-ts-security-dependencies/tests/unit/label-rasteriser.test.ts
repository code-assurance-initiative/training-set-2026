import { describe, expect, it } from 'vitest';
import { LabelFormatError, LabelRasteriser } from '../../src/labels/label-rasteriser.js';
import { labelPdf } from '../support/label-pdf.js';

const pngSignature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

function pngSize(png: Uint8Array): { width: number; height: number } {
  const view = new DataView(png.buffer, png.byteOffset, png.byteLength);
  return { width: view.getUint32(16), height: view.getUint32(20) };
}

describe('label rasteriser', () => {
  it('renders a 4×6 inch label at the printer resolution', () => {
    const png = new LabelRasteriser(203).rasterise(labelPdf('AB12345678'));

    expect(Array.from(png.subarray(0, 8))).toEqual(pngSignature);
    expect(pngSize(png)).toEqual({ width: 812, height: 1218 });
  });

  it('scales with the printer resolution', () => {
    expect(pngSize(new LabelRasteriser(300).rasterise(labelPdf('AB12345678')))).toEqual({
      width: 1200,
      height: 1800,
    });
  });

  it('renders only the first page', () => {
    const single = new LabelRasteriser(203).rasterise(labelPdf('AB12345678'));
    const withCustomsPages = new LabelRasteriser(203).rasterise(labelPdf('AB12345678', 2));

    expect(pngSize(withCustomsPages)).toEqual(pngSize(single));
  });

  it('refuses a damaged PDF', () => {
    expect(() =>
      new LabelRasteriser(203).rasterise(new TextEncoder().encode('%PDF-1.7\n%%EOF\n')),
    ).toThrow(LabelFormatError);
  });

  it('refuses bytes that are not a PDF', () => {
    expect(() => new LabelRasteriser(203).rasterise(new TextEncoder().encode('<html>'))).toThrow(
      LabelFormatError,
    );
  });
});
