import type { SqlClient } from '../db/sql-client.js';

export interface DocumentRecord {
  readonly id: string;
  readonly number: string;
  readonly title: string;
  readonly collection: string;
  readonly pageCount: number | null;
  readonly createdAt: Date;
  /** Path of the stored original, relative to the storage root; written only by the ingest pipeline. */
  readonly storagePath: string;
}

const columns = `d.id, d.number, d.title, d.collection, d.page_count AS "pageCount",
  d.created_at AS "createdAt", d.storage_path AS "storagePath"`;

export class DocumentRepository {
  constructor(private readonly db: SqlClient) {}

  async getById(id: string): Promise<DocumentRecord | undefined> {
    const result = await this.db.query<DocumentRecord>(
      `SELECT ${columns} FROM documents d WHERE d.id = $1 AND d.deleted_at IS NULL`,
      [id],
    );
    return result.rows[0];
  }

  async getByNumber(number: string): Promise<DocumentRecord | undefined> {
    const result = await this.db.query<DocumentRecord>(
      `SELECT ${columns} FROM documents d WHERE d.number = $1 AND d.deleted_at IS NULL`,
      [number],
    );
    return result.rows[0];
  }

  /** The extracted full text of a document, or undefined when it has none. */
  async getText(id: string): Promise<string | undefined> {
    const result = await this.db.query<{ body: string }>(
      'SELECT t.body FROM document_texts t WHERE t.document_id = $1',
      [id],
    );
    return result.rows[0]?.body;
  }
}
