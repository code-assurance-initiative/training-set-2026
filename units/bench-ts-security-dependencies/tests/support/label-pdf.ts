import * as mupdf from 'mupdf';

/** A 4×6 inch label PDF (288×432 pt) with a black box and a tracking number, like a carrier's. */
export function labelPdf(trackingNumber: string, extraPages = 0): Uint8Array {
  const document = new mupdf.PDFDocument();
  const font = document.addSimpleFont(new mupdf.Font('Helvetica'));
  const resources = document.addObject({ Font: { F1: font } });
  const content = `0 g 18 300 252 110 re f 1 g BT /F1 24 Tf 30 340 Td (${trackingNumber}) Tj ET`;
  for (let i = 0; i <= extraPages; i += 1) {
    document.insertPage(-1, document.addPage([0, 0, 288, 432], 0, resources, content));
  }
  return document.saveToBuffer('compress').asUint8Array();
}
