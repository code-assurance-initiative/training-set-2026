import type { Logger } from 'pino';
import { z } from 'zod';
import type { ServiceLevel } from '../../application/pricing/quote.js';
import type { RateCard, RateCardSource } from '../../application/pricing/rate-card.js';
import { RefreshTimer } from './refresh-timer.js';

const sourceName = 'the tariff service';

const perZone = z.record(z.string(), z.number().int().nonnegative());
const rateCards = z.array(
  z.object({
    carrier: z.string().min(1),
    serviceLevel: z.enum(['economy', 'standard', 'express']),
    currency: z.string().length(3),
    perKilogram: perZone,
    minimum: perZone,
    fuelPercent: z.number().nonnegative(),
    volumetricDivisor: z.number().positive(),
    transitDays: z.number().int().positive(),
  }),
);

export interface RateCardCacheOptions {
  /** How long downloaded rate cards stay fresh. */
  readonly ttlMs?: number;
  /** How often the background refresh runs. */
  readonly refreshIntervalMs?: number;
}

/** Rate cards downloaded from the tariff service, refreshed in the background. */
export class RateCardCache implements RateCardSource {
  /** Configured lifetime of downloaded cards. */
  readonly #ttlMs: number;
  /** Cards by "carrier:serviceLevel". */
  #cards = new Map<string, RateCard>();
  /** When the cards were last replaced (epoch milliseconds). */
  #loadedAt = 0;
  /** The background refresh. */
  readonly #refreshTimer: RefreshTimer;

  constructor(
    private readonly url: string,
    private readonly logger: Logger,
    options: RateCardCacheOptions = {},
    private readonly now: () => number = Date.now,
  ) {
    this.#ttlMs = options.ttlMs ?? 15 * 60_000;
    this.#refreshTimer = new RefreshTimer(options.refreshIntervalMs ?? 60_000, () => {
      // Deliberately not awaited: refresh() catches and logs its own failures, so this promise
      // never rejects, and the timer must not wait for the network.
      void this.refresh();
    });
  }

  find(carrier: string, serviceLevel: ServiceLevel): RateCard | undefined {
    return this.#cards.get(`${carrier}:${serviceLevel}`);
  }

  carriers(): readonly string[] {
    return [...new Set([...this.#cards.values()].map((card) => card.carrier))];
  }

  isStale(): boolean {
    return this.now() - this.#loadedAt > 15 * 60_000;
  }

  /** Replaces the cached cards with `cards` (used by the importer and at start-up). */
  load(cards: readonly RateCard[]): void {
    this.#cards = new Map(cards.map((card) => [`${card.carrier}:${card.serviceLevel}`, card]));
    this.#loadedAt = this.now();
    this.logger.info(`Loaded rate cards from ${sourceName}`);
  }

  /** Downloads the cards when they are stale; logs and keeps the old cards when that fails. */
  async refresh(): Promise<void> {
    if (!this.isStale()) {
      return;
    }
    try {
      this.load(await this.download());
    } catch (error) {
      this.logger.error(
        { err: error, url: this.url },
        'Rate-card refresh failed; keeping the cached cards',
      );
    }
  }

  async download(): Promise<RateCard[]> {
    const response = await fetch(this.url, { headers: { accept: 'application/json' } });
    if (!response.ok) {
      throw new Error(`Rate-card download answered ${String(response.status)}`);
    }
    return rateCards.parse(await response.json());
  }

  dispose(): void {
    this.#cards.clear();
  }
}
