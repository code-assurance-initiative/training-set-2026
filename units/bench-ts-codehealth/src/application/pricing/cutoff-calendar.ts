import type { Logger } from 'pino';

const defaultCutoff = { hour: 15, minute: 0 } as const;

export interface Cutoff {
  readonly hour: number;
  readonly minute: number;
}

/** Parses a carrier's daily collection cut-off ("HH:MM", UTC: the depot systems run on UTC). */
export function parseCutoff(value: string): Cutoff {
  const match = /^([01]\d|2[0-3]):([0-5]\d)$/.exec(value.trim());
  if (!match) {
    throw new RangeError(`Not a cut-off time: ${value}`);
  }
  return { hour: Number(match[1]), minute: Number(match[2]) };
}

/** Collection cut-offs per carrier; the label service uses them to promise a ship date. */
export class CutoffCalendar {
  readonly #cutoffs = new Map<string, Cutoff>();

  constructor(
    configured: Readonly<Record<string, string>>,
    private readonly logger: Logger,
  ) {
    for (const [carrier, value] of Object.entries(configured)) {
      this.#cutoffs.set(carrier, this.read(carrier, value));
    }
  }

  cutoffFor(carrier: string): Cutoff {
    return this.#cutoffs.get(carrier) ?? defaultCutoff;
  }

  /** The day a parcel handed over at `at` leaves the depot: today before the cut-off, else tomorrow. */
  shipDate(carrier: string, at: Date): Date {
    const cutoff = this.cutoffFor(carrier);
    const ship = new Date(at);
    if (at.getUTCHours() * 60 + at.getUTCMinutes() >= cutoff.hour * 60 + cutoff.minute) {
      ship.setUTCDate(ship.getUTCDate() + 1);
    }
    ship.setUTCHours(0, 0, 0, 0);
    return ship;
  }

  private read(carrier: string, value: string): Cutoff {
    try {
      return parseCutoff(value);
    } catch (error) {
      this.logger.warn(
        { carrier, value, err: error },
        'Unparsable collection cut-off; using the default 15:00',
      );
      return defaultCutoff;
    }
  }
}
