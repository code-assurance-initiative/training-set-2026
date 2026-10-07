import JSZip from 'jszip';

export interface PrintableLabel {
  readonly trackingNumber: string;
  readonly route: number;
  readonly png: Uint8Array;
}

/**
 * Packs a run's labels into one ZIP for the depot print station: a folder per vehicle, labels in
 * loading order, and an index the station prints as the cover sheet.
 */
export async function bundleLabels(
  runId: string,
  labels: readonly PrintableLabel[],
): Promise<Buffer> {
  const zip = new JSZip();
  const index = ['position;vehicle;tracking_number'];
  labels.forEach((label, position) => {
    const name = `${String(position + 1).padStart(3, '0')}-${label.trackingNumber}.png`;
    zip.file(`vehicle-${label.route}/${name}`, label.png, { binary: true });
    index.push(`${position + 1};${label.route};${label.trackingNumber}`);
  });
  zip.file('index.csv', `${index.join('\n')}\n`);
  zip.file('run.txt', `${runId}\n`);
  return zip.generateAsync({ type: 'nodebuffer', compression: 'DEFLATE', platform: 'UNIX' });
}
