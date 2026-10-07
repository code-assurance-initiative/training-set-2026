export interface Clock {
  now(): Date;
}

export const systemClock: Clock = { now: () => new Date() };

/** The UTC calendar day of an instant, as midnight UTC. */
export function utcDay(instant: Date): Date {
  return new Date(Date.UTC(instant.getUTCFullYear(), instant.getUTCMonth(), instant.getUTCDate()));
}

export function addMinutes(instant: Date, minutes: number): Date {
  return new Date(instant.getTime() + minutes * 60_000);
}

export function addUtcMonths(day: Date, months: number): Date {
  return new Date(Date.UTC(day.getUTCFullYear(), day.getUTCMonth() + months, day.getUTCDate()));
}

/** `YYYY-MM-DD` of a UTC day. */
export function isoDay(day: Date): string {
  return day.toISOString().slice(0, 10);
}

/** A DATE column as a UTC day: the driver returns `YYYY-MM-DD` text (see db/database.ts). */
export function dayFrom(value: Date | string): Date {
  return typeof value === 'string' ? new Date(`${value}T00:00:00Z`) : value;
}
