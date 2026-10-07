import type { SqlClient } from '../db/sql-client.js';

export interface ExportRecord {
  readonly id: string;
  readonly documentId: string;
  readonly caller: string;
  readonly filePath: string;
  readonly fileName: string;
  readonly passwordHash: string | null;
  readonly createdAt: Date;
}

export type NewExport = Omit<ExportRecord, 'createdAt'>;

export class ExportStore {
  constructor(private readonly db: SqlClient) {}

  async create(record: NewExport): Promise<void> {
    await this.db.query(
      `INSERT INTO exports (id, document_id, caller, file_path, file_name, password_hash)
       VALUES ($1, $2, $3, $4, $5, $6)`,
      [
        record.id,
        record.documentId,
        record.caller,
        record.filePath,
        record.fileName,
        record.passwordHash,
      ],
    );
  }

  async get(id: string): Promise<ExportRecord | undefined> {
    const result = await this.db.query<ExportRecord>(
      `SELECT e.id, e.document_id AS "documentId", e.caller, e.file_path AS "filePath",
         e.file_name AS "fileName", e.password_hash AS "passwordHash", e.created_at AS "createdAt"
       FROM exports e WHERE e.id = $1`,
      [id],
    );
    return result.rows[0];
  }
}
