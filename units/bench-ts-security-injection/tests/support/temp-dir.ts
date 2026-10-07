import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';

/** A fresh directory under the OS temp directory, and a function that removes it. */
export async function tempDirectory(): Promise<{ path: string; remove: () => Promise<void> }> {
  const directory = await mkdtemp(path.join(tmpdir(), 'archive-test-'));
  return { path: directory, remove: () => rm(directory, { recursive: true, force: true }) };
}
