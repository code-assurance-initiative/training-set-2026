import type { Knex } from 'knex';
import { ClassSession } from '../domain/classes/class-session.js';
import { ClassSessionId } from '../domain/classes/class-session-id.js';
import type { ClassSessionRepository } from '../domain/classes/class-session-repository.js';

interface ClassSessionRow {
  id: string;
  title: string;
  starts_at: Date | string;
  duration_minutes: number;
  capacity: number;
}

export class KnexClassSessionRepository implements ClassSessionRepository {
  constructor(private readonly db: Knex) {}

  async get(id: ClassSessionId): Promise<ClassSession | undefined> {
    const row = await this.db<ClassSessionRow>('class_sessions').where({ id: id.value }).first();
    return row
      ? new ClassSession(
          ClassSessionId.of(row.id),
          row.title,
          new Date(row.starts_at),
          row.duration_minutes,
          row.capacity,
        )
      : undefined;
  }

  async save(session: ClassSession): Promise<void> {
    const row = {
      title: session.title,
      starts_at: session.startsAt,
      duration_minutes: session.durationMinutes,
      capacity: session.capacity,
    };
    const updated = await this.db('class_sessions').where({ id: session.id.value }).update(row);
    if (updated === 0) {
      await this.db('class_sessions').insert({ id: session.id.value, ...row });
    }
  }
}
