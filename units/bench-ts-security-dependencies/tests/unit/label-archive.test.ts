import { readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import JSZip from 'jszip';
import tmp from 'tmp';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { bundleLabels } from '../../src/labels/label-archive.js';

describe('label archive', () => {
  let directory: tmp.DirResult;

  beforeEach(() => {
    directory = tmp.dirSync({ prefix: 'labels-', unsafeCleanup: true });
  });
  afterEach(() => {
    directory.removeCallback();
  });

  it('writes a folder per vehicle in loading order, with an index and the run id', async () => {
    const png = (n: number) => new Uint8Array([0x89, 0x50, 0x4e, 0x47, n]);
    const archive = await bundleLabels('7d1e6c3a-2f0b-4e57-9a51-0c6f8a2b9e10', [
      { trackingNumber: 'AB12345678', route: 1, png: png(1) },
      { trackingNumber: 'AB12345679', route: 1, png: png(2) },
      { trackingNumber: 'CD87654321', route: 2, png: png(3) },
    ]);
    const path = join(directory.name, 'labels.zip');
    writeFileSync(path, archive);

    const zip = await JSZip.loadAsync(readFileSync(path));

    expect(
      Object.keys(zip.files)
        .filter((name) => !zip.files[name]?.dir)
        .sort(),
    ).toEqual([
      'index.csv',
      'run.txt',
      'vehicle-1/001-AB12345678.png',
      'vehicle-1/002-AB12345679.png',
      'vehicle-2/003-CD87654321.png',
    ]);
    await expect(zip.file('index.csv')?.async('string')).resolves.toBe(
      'position;vehicle;tracking_number\n1;1;AB12345678\n2;1;AB12345679\n3;2;CD87654321\n',
    );
    await expect(zip.file('run.txt')?.async('string')).resolves.toBe(
      '7d1e6c3a-2f0b-4e57-9a51-0c6f8a2b9e10\n',
    );
    const third = await zip.file('vehicle-2/003-CD87654321.png')?.async('uint8array');
    expect(Array.from(third ?? [])).toEqual([0x89, 0x50, 0x4e, 0x47, 3]);
  });

  it('bundles an empty run as an index without labels', async () => {
    const zip = await JSZip.loadAsync(await bundleLabels('run', []));
    await expect(zip.file('index.csv')?.async('string')).resolves.toBe(
      'position;vehicle;tracking_number\n',
    );
  });
});
