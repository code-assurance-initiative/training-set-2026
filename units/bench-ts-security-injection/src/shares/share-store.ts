import type { SqlClient } from '../db/sql-client.js';

export interface ShareLink {
  readonly token: string;
  readonly documentId: string;
  readonly createdBy: string;
  readonly passwordHash: string;
  readonly expiresAt: Date;
}

export class ShareStore {
  constructor(private readonly db: SqlClient) {}

  async create(link: ShareLink): Promise<void> {
    await this.db.query(
      `INSERT INTO share_links (token, document_id, created_by, password_hash, expires_at)
       VALUES ($1, $2, $3, $4, $5)`,
      [link.token, link.documentId, link.createdBy, link.passwordHash, link.expiresAt],
    );
  }

  /** The link, unless it does not exist or has expired. */
  async findActive(token: string, now: Date): Promise<ShareLink | undefined> {
    const result = await this.db.query<ShareLink>(
      `SELECT l.token, l.document_id AS "documentId", l.created_by AS "createdBy",
         l.password_hash AS "passwordHash", l.expires_at AS "expiresAt"
       FROM share_links l WHERE l.token = $1 AND l.expires_at > $2`,
      [token, now],
    );
    return result.rows[0];
  }

  /** Deletes expired links; returns how many were removed. */
  async purgeExpired(now: Date): Promise<number> {
    const result = await this.db.query('DELETE FROM share_links WHERE expires_at <= $1', [now]);
    return result.rowCount ?? 0;
  }
}
