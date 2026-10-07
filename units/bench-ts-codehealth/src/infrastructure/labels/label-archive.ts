import { readFileSync } from 'node:fs';
import { mkdir, readdir, rm, stat, writeFile } from 'node:fs/promises';
import { join } from 'node:path';
import type { Logger } from 'pino';
import type { LabelStore } from '../../domain/ports/label-store.js';

const labelIdPattern = /^[A-Za-z0-9-]{1,64}$/;

/** Stores rendered labels as files, one per label id, under one directory. */
export class LabelArchive implements LabelStore {
  constructor(
    private readonly directory: string,
    private readonly logger: Logger,
  ) {}

  async save(labelId: string, zpl: string): Promise<string> {
    const path = this.pathOf(labelId);
    try {
      await mkdir(this.directory, { recursive: true });
      await writeFile(path, zpl, 'utf8');
      return path;
    } catch (error) {
      this.logger.error({ err: error, labelId, path }, 'Could not archive the label');
      throw error;
    }
  }

  async load(labelId: string): Promise<string | undefined> {
    const path = this.pathOf(labelId);
    if (!(await this.exists(path))) {
      return undefined;
    }
    return readFileSync(path, 'utf8');
  }

  /** Deletes labels whose file is older than `cutoff`; returns how many were deleted. */
  async purgeOlderThan(cutoff: Date): Promise<number> {
    const names = await readdir(this.directory).catch((error: unknown): string[] => {
      if (isMissing(error)) {
        return [];
      }
      throw error;
    });
    let purged = 0;
    for (const name of names) {
      const path = join(this.directory, name);
      const info = await stat(path);
      if (info.mtime < cutoff) {
        await rm(path, { force: true });
        purged += 1;
      }
    }
    return purged;
  }

  private async exists(path: string): Promise<boolean> {
    try {
      await stat(path);
      return true;
    } catch (error) {
      if (isMissing(error)) {
        return false;
      }
      throw error;
    }
  }

  private pathOf(labelId: string): string {
    if (!labelIdPattern.test(labelId)) {
      throw new RangeError('Not a label id');
    }
    return join(this.directory, `${labelId}.zpl`);
  }
}

function isMissing(error: unknown): boolean {
  return error instanceof Error && 'code' in error && error.code === 'ENOENT';
}
