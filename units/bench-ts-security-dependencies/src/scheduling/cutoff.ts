import ms from 'ms';

/** How far ahead of a departure a depot closes its runs, as its settings write it: '90m', '2h', '1.5h'. */
const leadTimePattern = /^\d+(?:\.\d+)?\s?(?:m|min|mins|h|hr|hrs)$/i;

export class InvalidLeadTimeError extends Error {
  constructor(readonly leadTime: string) {
    super(`'${leadTime}' is not a lead time in minutes or hours (for example '90m' or '2h').`);
    this.name = 'InvalidLeadTimeError';
  }
}

export function isLeadTime(value: string): value is ms.StringValue {
  return leadTimePattern.test(value);
}

/** The latest moment a parcel can join a run whose first vehicle leaves at `departure`. */
export function cutoffBefore(departure: Date, leadTime: string): Date {
  if (!isLeadTime(leadTime)) {
    throw new InvalidLeadTimeError(leadTime);
  }
  return new Date(departure.getTime() - ms(leadTime));
}
