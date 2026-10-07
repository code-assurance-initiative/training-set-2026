import { AggregateRoot } from '../../../shared-kernel/aggregate-root.js';
import type { ClassSessionId } from './class-session-id.js';

/** One scheduled class (a spinning class on Tuesday at 18:00) that members book places in. */
export class ClassSession extends AggregateRoot<ClassSessionId> {
  readonly title: string;
  readonly startsAt: Date;
  readonly durationMinutes: number;
  readonly capacity: number;

  constructor(
    id: ClassSessionId,
    title: string,
    startsAt: Date,
    durationMinutes: number,
    capacity: number,
  ) {
    super(id);
    this.title = title;
    this.startsAt = startsAt;
    this.durationMinutes = durationMinutes;
    this.capacity = capacity;
  }

  hasStarted(now: Date): boolean {
    return now.getTime() >= this.startsAt.getTime();
  }

  hasRoomFor(bookedPlaces: number): boolean {
    return bookedPlaces < this.capacity;
  }
}
