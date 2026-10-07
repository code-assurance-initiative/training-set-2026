import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import {
  hashDownloadPassword,
  verifyDownloadPassword,
} from '../../src/exports/download-password.js';
import { ExportAuditLog } from '../../src/exports/export-audit-log.js';
import { ExportService } from '../../src/exports/export-service.js';
import { ExportStore, type ExportRecord } from '../../src/exports/export-store.js';
import { documentId, sampleDocument } from '../support/documents.js';
import { FakeSqlClient } from '../support/fake-sql.js';
import { silentLogger } from '../support/silent-logger.js';
import { tempDirectory } from '../support/temp-dir.js';

let directory: { path: string; remove: () => Promise<void> };

beforeEach(async () => {
  directory = await tempDirectory();
});

afterEach(async () => {
  await directory.remove();
});

describe('download passwords', () => {
  it('verifies the password it hashed and nothing else', async () => {
    const hash = await hashDownloadPassword('correct horse battery');

    expect(hash).toMatch(/^\$2[ab]\$12\$/);
    await expect(verifyDownloadPassword('correct horse battery', hash)).resolves.toBe(true);
    await expect(verifyDownloadPassword('correct horse', hash)).resolves.toBe(false);
  });
});

describe('export audit log', () => {
  it('appends one line per export', async () => {
    const file = path.join(directory.path, 'audit', 'exports.log');
    const log = new ExportAuditLog(file, () => new Date('2026-10-07T08:00:00.000Z'));

    await log.record({
      caller: 'archivist-4',
      documentId,
      format: 'pdf',
      fileName: 'handbook.pdf',
    });
    await log.record({
      caller: 'archivist-9',
      documentId,
      format: 'odt',
      fileName: 'handbook.odt',
    });

    const lines = (await readFile(file, 'utf8')).trimEnd().split('\n');
    expect(lines).toEqual([
      `2026-10-07T08:00:00.000Z export caller=archivist-4 document=${documentId} format=pdf file="handbook.pdf"`,
      `2026-10-07T08:00:00.000Z export caller=archivist-9 document=${documentId} format=odt file="handbook.odt"`,
    ]);
  });
});

describe('export store', () => {
  it('inserts and reads exports with bound parameters', async () => {
    const db = new FakeSqlClient().willReturn().willReturn({ id: 'e1' });
    const store = new ExportStore(db);
    const record = {
      id: 'e1',
      documentId,
      caller: 'archivist-4',
      filePath: '/x/e1/handbook.pdf',
      fileName: 'handbook.pdf',
      passwordHash: null,
    };

    await store.create(record);
    await expect(store.get('e1')).resolves.toEqual({ id: 'e1' });
    expect(db.queries[0]?.values).toEqual([
      'e1',
      documentId,
      'archivist-4',
      '/x/e1/handbook.pdf',
      'handbook.pdf',
      null,
    ]);
    expect(db.queries[1]?.values).toEqual(['e1']);
  });
});

describe('export service', () => {
  function service(records: Map<string, ExportRecord>, audit: unknown[]) {
    return new ExportService(
      { getById: (id) => Promise.resolve(id === documentId ? sampleDocument : undefined) },
      {
        convert: (input, format, out) =>
          Promise.resolve(path.join(out, `${path.parse(input).name}.${format}`)),
      },
      {
        create: (record) => {
          records.set(record.id, { ...record, createdAt: new Date() });
          return Promise.resolve();
        },
        get: (id) => Promise.resolve(records.get(id)),
      },
      {
        record: (entry) => {
          audit.push(entry);
          return Promise.resolve();
        },
      },
      {
        originals: path.join(directory.path, 'originals'),
        exports: path.join(directory.path, 'exports'),
      },
      silentLogger,
    );
  }

  it('converts the original, records the export and audits it', async () => {
    const records = new Map<string, ExportRecord>();
    const audit: unknown[] = [];

    const id = await service(records, audit).create('archivist-4', documentId, {
      format: 'pdf',
      fileName: 'h.pdf',
    });

    const record = records.get(id ?? '');
    expect(record?.filePath).toBe(path.join(directory.path, 'exports', id ?? '', 'handbook.pdf'));
    expect(record?.passwordHash).toBeNull();
    expect(audit).toEqual([
      { caller: 'archivist-4', documentId, format: 'pdf', fileName: 'h.pdf' },
    ]);
  });

  it('answers undefined for an unknown document', async () => {
    await expect(
      service(new Map(), []).create('archivist-4', '00000000-0000-4000-8000-000000000000', {
        format: 'pdf',
        fileName: 'x.pdf',
      }),
    ).resolves.toBeUndefined();
  });

  it('releases a protected export only to its owner with the password', async () => {
    const records = new Map<string, ExportRecord>();
    const exports = service(records, []);
    const id = await exports.create('archivist-4', documentId, {
      format: 'pdf',
      fileName: 'h.pdf',
      password: 'a long download password',
    });
    if (id === undefined) {
      throw new Error('export not created');
    }

    await expect(exports.download('archivist-9', id, 'a long download password')).resolves.toEqual({
      kind: 'not-found',
    });
    await expect(exports.download('archivist-4', id, undefined)).resolves.toEqual({
      kind: 'password-required',
    });
    await expect(exports.download('archivist-4', id, 'wrong')).resolves.toEqual({
      kind: 'password-required',
    });
    await expect(
      exports.download('archivist-4', id, 'a long download password'),
    ).resolves.toMatchObject({ kind: 'ok' });
  });
});
