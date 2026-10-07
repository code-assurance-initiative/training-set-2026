import { randomUUID } from 'node:crypto';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';
import type { Logger } from 'pino';
import type { DocumentConverter } from '../conversion/document-converter.js';
import type { DocumentRepository } from '../documents/document-repository.js';
import { hashDownloadPassword, verifyDownloadPassword } from './download-password.js';
import type { ExportAuditLog } from './export-audit-log.js';
import type { ExportRecord, ExportStore } from './export-store.js';

export interface ExportRequest {
  readonly format: string;
  readonly fileName: string;
  readonly password?: string | undefined;
}

export interface ExportDirectories {
  readonly originals: string;
  readonly exports: string;
}

export type DownloadResult =
  | { readonly kind: 'ok'; readonly record: ExportRecord }
  | { readonly kind: 'not-found' }
  | { readonly kind: 'password-required' };

export class ExportService {
  constructor(
    private readonly documents: Pick<DocumentRepository, 'getById'>,
    private readonly converter: Pick<DocumentConverter, 'convert'>,
    private readonly store: Pick<ExportStore, 'create' | 'get'>,
    private readonly audit: Pick<ExportAuditLog, 'record'>,
    private readonly directories: ExportDirectories,
    private readonly logger: Logger,
  ) {}

  /** Converts a document for download; undefined when the document does not exist. */
  async create(
    caller: string,
    documentId: string,
    request: ExportRequest,
  ): Promise<string | undefined> {
    const document = await this.documents.getById(documentId);
    if (!document) {
      return undefined;
    }
    const exportId = randomUUID();
    const workDirectory = path.join(this.directories.exports, exportId);
    await mkdir(workDirectory, { recursive: true });
    const source = path.join(this.directories.originals, document.storagePath);
    const filePath = await this.converter.convert(source, request.format, workDirectory);
    const passwordHash =
      request.password === undefined ? null : await hashDownloadPassword(request.password);
    await this.store.create({
      id: exportId,
      documentId,
      caller,
      filePath,
      fileName: request.fileName,
      passwordHash,
    });
    await this.audit.record({
      caller,
      documentId,
      format: request.format,
      fileName: request.fileName,
    });
    this.logger.info({ documentId, exportId, fileName: request.fileName }, 'Export created');
    return exportId;
  }

  /** The export, if it exists, belongs to the caller and the password (when one was set) matches. */
  async download(
    caller: string,
    exportId: string,
    password: string | undefined,
  ): Promise<DownloadResult> {
    const record = await this.store.get(exportId);
    if (record?.caller !== caller) {
      return { kind: 'not-found' };
    }
    if (record.passwordHash !== null) {
      const matches =
        password !== undefined && (await verifyDownloadPassword(password, record.passwordHash));
      if (!matches) {
        return { kind: 'password-required' };
      }
    }
    return { kind: 'ok', record };
  }
}
