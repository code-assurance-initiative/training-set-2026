import type { SqlClient } from '../db/sql-client.js';
import type { FilterNode } from './filter-tree.js';

export interface SavedSearch {
  readonly id: string;
  readonly owner: string;
  readonly name: string;
  readonly term: string;
  readonly expression: string | null;
  readonly tree: FilterNode | null;
}

const columns = 's.id, s.owner, s.name, s.term, s.expression, s.tree';

export class SavedSearchStore {
  constructor(private readonly db: SqlClient) {}

  async list(owner: string): Promise<SavedSearch[]> {
    const result = await this.db.query<SavedSearch>(
      `SELECT ${columns} FROM saved_searches s WHERE s.owner = $1 ORDER BY s.name`,
      [owner],
    );
    return result.rows;
  }

  async get(id: string, owner: string): Promise<SavedSearch | undefined> {
    const result = await this.db.query<SavedSearch>(
      `SELECT ${columns} FROM saved_searches s WHERE s.id = $1 AND s.owner = $2`,
      [id, owner],
    );
    return result.rows[0];
  }

  async create(search: SavedSearch): Promise<void> {
    await this.db.query(
      `INSERT INTO saved_searches (id, owner, name, term, expression, tree) VALUES ($1, $2, $3, $4, $5, $6)`,
      [
        search.id,
        search.owner,
        search.name,
        search.term,
        search.expression,
        search.tree === null ? null : JSON.stringify(search.tree),
      ],
    );
  }

  async markOpened(id: string, owner: string): Promise<void> {
    await this.db.query(
      'UPDATE saved_searches SET last_opened_at = now() WHERE id = $1 AND owner = $2',
      [id, owner],
    );
  }
}
