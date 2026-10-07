import dateFns from 'date-fns';
import type { DeliveryWindow } from '../dispatch/dispatch-run.js';

const { addMinutes, format, isValid, isWeekend, parse, startOfHour } = dateFns;

export interface WindowPolicy {
  /** First departure from the depot, `HH:mm` local time. */
  readonly departure: string;
  /** Planned driving and handover time per stop. */
  readonly minutesPerStop: number;
  /** Width of the window promised to the recipient. */
  readonly windowMinutes: number;
}

export const standardWindowPolicy: WindowPolicy = {
  departure: '08:00',
  minutesPerStop: 12,
  windowMinutes: 120,
};

const dateFormat = 'YYYY-MM-DD';
const timeFormat = 'YYYY-MM-DD HH:mm';

export class InvalidServiceDateError extends Error {
  constructor(
    readonly serviceDate: string,
    reason: string,
  ) {
    super(`Service date ${serviceDate} ${reason}.`);
    this.name = 'InvalidServiceDateError';
  }
}

/** The moment the first vehicle leaves on `serviceDate` (a weekday, `YYYY-MM-DD`). */
export function departureOn(
  serviceDate: string,
  policy: WindowPolicy = standardWindowPolicy,
): Date {
  const day = parse(serviceDate);
  if (
    !/^\d{4}-\d{2}-\d{2}$/.test(serviceDate) ||
    !isValid(day) ||
    format(day, dateFormat) !== serviceDate
  ) {
    throw new InvalidServiceDateError(serviceDate, 'is not a calendar date');
  }
  if (isWeekend(day)) {
    throw new InvalidServiceDateError(serviceDate, 'is a weekend; depots deliver Monday to Friday');
  }
  return parse(`${serviceDate}T${policy.departure}:00`);
}

/**
 * The window promised for the stop at `position` (0-based) of a route leaving at `departure`. The
 * expected arrival is rounded down to the hour, so a window always starts on the hour it opens.
 */
export function windowForStop(
  departure: Date,
  position: number,
  policy: WindowPolicy = standardWindowPolicy,
): DeliveryWindow {
  const expected = addMinutes(departure, (position + 1) * policy.minutesPerStop);
  const from = startOfHour(expected);
  return {
    from: format(from, timeFormat),
    to: format(addMinutes(from, policy.windowMinutes), timeFormat),
  };
}
