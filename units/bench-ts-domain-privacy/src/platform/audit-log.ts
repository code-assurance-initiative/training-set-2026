import { randomUUID } from 'node:crypto';
import type { Knex } from 'knex';

export interface AuditEntry {
  readonly occurredAt: Date;
  /** Who made the change: the subject of the access token, or the name of a scheduled job. */
  readonly actor: string;
  readonly action: string;
  readonly subjectType: string;
  readonly subjectId: string;
  /** Names of the fields that changed — never their values, which may be personal data. */
  readonly changedFields: readonly string[];
}

/** Append-only record of changes to personal data, written in the transaction that makes the change. */
export class AuditLog {
  constructor(private readonly db: Knex) {}

  async append(entry: AuditEntry): Promise<void> {
    await this.db('audit_log').insert({
      id: randomUUID(),
      occurred_at: entry.occurredAt,
      actor: entry.actor,
      action: entry.action,
      subject_type: entry.subjectType,
      subject_id: entry.subjectId,
      changed_fields: JSON.stringify(entry.changedFields),
    });
  }

  async forSubject(subjectType: string, subjectId: string): Promise<AuditEntry[]> {
    const rows = await this.db('audit_log')
      .where({ subject_type: subjectType, subject_id: subjectId })
      .orderBy('occurred_at');
    return rows.map((row: AuditRow) => ({
      occurredAt: new Date(row.occurred_at),
      actor: row.actor,
      action: row.action,
      subjectType: row.subject_type,
      subjectId: row.subject_id,
      changedFields: parseFields(row.changed_fields),
    }));
  }
}

interface AuditRow {
  occurred_at: Date | string;
  actor: string;
  action: string;
  subject_type: string;
  subject_id: string;
  changed_fields: string | string[];
}

function parseFields(value: string | string[]): string[] {
  return typeof value === 'string' ? (JSON.parse(value) as string[]) : value;
}
