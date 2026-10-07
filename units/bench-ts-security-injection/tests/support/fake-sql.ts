import type { SqlClient } from '../../src/db/sql-client.js';

export interface RecordedQuery {
  readonly text: string;
  readonly values: readonly unknown[];
}

/** Records every statement and answers each with the next queued set of rows (or none). */
export class FakeSqlClient implements SqlClient {
  readonly queries: RecordedQuery[] = [];
  private readonly answers: object[][] = [];

  willReturn(...rows: object[]): this {
    this.answers.push(rows);
    return this;
  }

  query<Row extends object>(text: string, values: readonly unknown[] = []) {
    this.queries.push({ text, values });
    const rows = (this.answers.shift() ?? []) as Row[];
    return Promise.resolve({ rows, rowCount: rows.length });
  }

  get last(): RecordedQuery {
    const query = this.queries.at(-1);
    if (!query) {
      throw new Error('no query was run');
    }
    return query;
  }
}
