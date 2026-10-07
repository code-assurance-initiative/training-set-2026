import type { ClassSession } from './class-session.js';
import type { ClassSessionId } from './class-session-id.js';

export interface ClassSessionRepository {
  get(id: ClassSessionId): Promise<ClassSession | undefined>;
  save(session: ClassSession): Promise<void>;
}
