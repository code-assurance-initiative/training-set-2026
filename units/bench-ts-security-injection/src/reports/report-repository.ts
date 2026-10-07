import type { Knex } from 'knex';
import type { ReportDefinition, ReportRow, ReportSummary } from './report-model.js';

const maximumRows = 5_000;

export class ReportRepository {
  constructor(private readonly db: Knex) {}

  /** The reports a user owns, most recently changed first. */
  async listByOwner(owner: string): Promise<ReportSummary[]> {
    const result = await this.db.raw<{ rows: ReportSummary[] }>(
      `SELECT id, name, owner, updated_at AS "updatedAt" FROM reports WHERE owner = '${owner}' ORDER BY updated_at DESC`,
    );
    return result.rows;
  }

  async get(id: string): Promise<ReportDefinition | undefined> {
    const row = await this.db<ReportDefinition>('reports')
      .select('id', 'name', 'owner', 'collection', { updatedAt: 'updated_at' })
      .where({ id })
      .first();
    return row;
  }

  async rows(report: ReportDefinition): Promise<ReportRow[]> {
    return this.db<ReportRow>('documents')
      .select('number', 'title', 'collection', { pageCount: 'page_count', createdAt: 'created_at' })
      .where({ collection: report.collection })
      .whereNull('deleted_at')
      .orderBy('number')
      .limit(maximumRows);
  }
}
