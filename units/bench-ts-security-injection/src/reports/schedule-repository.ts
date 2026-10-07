import type { Knex } from 'knex';

export const scheduleStates = ['active', 'paused', 'all'] as const;

export type ScheduleState = (typeof scheduleStates)[number];

export interface ReportSchedule {
  readonly id: string;
  readonly reportId: string;
  readonly cron: string;
  readonly timezone: string;
  readonly paused: boolean;
  readonly nextRunAt: Date | null;
}

export class ScheduleRepository {
  constructor(private readonly db: Knex) {}

  /** A report's delivery schedules, soonest first; `timezone` narrows them to one zone. */
  async forReport(
    reportId: string,
    state: ScheduleState,
    timezone?: string,
  ): Promise<ReportSchedule[]> {
    const result = await this.db.raw<{ rows: ReportSchedule[] }>(
      `SELECT s.id, s.report_id AS "reportId", s.cron, s.timezone, s.paused, s.next_run_at AS "nextRunAt"
       FROM report_schedules s
       WHERE s.report_id = ? AND (? = 'all' OR s.paused = (? = 'paused')) AND (?::text IS NULL OR s.timezone = ?)
       ORDER BY s.next_run_at NULLS LAST`,
      [reportId, state, state, timezone ?? null, timezone ?? null],
    );
    return result.rows;
  }
}
