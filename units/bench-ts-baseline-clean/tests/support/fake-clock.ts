import type { Clock } from '../../src/application/clock.js';

/** A clock that only moves when a test moves it. */
export class FakeClock implements Clock {
  private current: Date;

  constructor(start = new Date('2026-10-07T08:00:00.000Z')) {
    this.current = start;
  }

  now(): Date {
    return new Date(this.current.getTime());
  }

  advanceMinutes(minutes: number): void {
    this.current = new Date(this.current.getTime() + minutes * 60_000);
  }
}
