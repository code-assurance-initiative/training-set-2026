import type { SqlClient } from '../db/sql-client.js';
import { orderByClause, type SortKey } from './sort-order.js';

export interface DocumentHit {
  readonly id: string;
  readonly number: string;
  readonly title: string;
  readonly collection: string;
  readonly createdAt: Date;
}

export interface SearchQuery {
  readonly term: string;
  readonly sort: SortKey;
  readonly limit: number;
  readonly offset: number;
}

export class DocumentSearchRepository {
  constructor(private readonly db: SqlClient) {}

  /** Documents whose title contains the term, case-insensitively, one page at a time. */
  async search(query: SearchQuery): Promise<DocumentHit[]> {
    const sql = `SELECT d.id, d.number, d.title, d.collection, d.created_at AS "createdAt"
      FROM documents d
      WHERE d.title ILIKE '%${query.term}%' AND d.deleted_at IS NULL
      ${orderByClause(query.sort)}
      LIMIT $1 OFFSET $2`;
    const result = await this.db.query<DocumentHit>(sql, [query.limit, query.offset]);
    return result.rows;
  }
}
