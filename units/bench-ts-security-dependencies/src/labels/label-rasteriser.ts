import * as mupdf from 'mupdf';

export class LabelFormatError extends Error {
  constructor(message: string, options?: ErrorOptions) {
    super(message, options);
    this.name = 'LabelFormatError';
  }
}

/**
 * Turns a carrier's label PDF into the 1-bit-friendly greyscale PNG the depots' thermal printers
 * take, at the printer's resolution. Only the first page is a label; carriers append customs pages.
 */
export class LabelRasteriser {
  constructor(private readonly dpi: number) {}

  rasterise(pdf: Uint8Array): Uint8Array {
    if (!startsLikePdf(pdf)) {
      throw new LabelFormatError('The label is not a PDF.');
    }
    let document: mupdf.Document;
    try {
      document = mupdf.Document.openDocument(pdf, 'application/pdf');
    } catch (error) {
      throw new LabelFormatError('The label is not a readable PDF.', { cause: error });
    }
    try {
      if (document.countPages() === 0) {
        throw new LabelFormatError('The label PDF has no pages.');
      }
      const page = document.loadPage(0);
      const scale = this.dpi / 72;
      const pixmap = page.toPixmap(
        mupdf.Matrix.scale(scale, scale),
        mupdf.ColorSpace.DeviceGray,
        false,
        true,
      );
      try {
        return pixmap.asPNG();
      } finally {
        pixmap.destroy();
        page.destroy();
      }
    } finally {
      document.destroy();
    }
  }
}

/** A PDF file carries its `%PDF-` header within the first 1024 bytes (ISO 32000-2, 7.5.2). */
function startsLikePdf(bytes: Uint8Array): boolean {
  return Buffer.from(bytes.subarray(0, 1024)).includes('%PDF-');
}
