import { afterEach, describe, expect, it, vi } from 'vitest';
import { RateCardCache } from '../../src/infrastructure/rates/rate-card-cache.js';
import { RefreshTimer } from '../../src/infrastructure/rates/refresh-timer.js';
import { rateCard } from '../support/builders.js';
import { silentLogger } from '../support/silent-logger.js';

afterEach(() => {
  vi.unstubAllGlobals();
  vi.useRealTimers();
});

describe('RateCardCache', () => {
  it('downloads, validates and serves rate cards', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.resolve(Response.json([rateCard(), rateCard({ carrier: 'corvid' })]))),
    );
    const cache = new RateCardCache('https://rates.test/cards', silentLogger);
    expect(cache.isStale()).toBe(true);
    await cache.refresh();
    expect(cache.isStale()).toBe(false);
    expect(cache.carriers()).toEqual(['alder', 'corvid']);
    expect(cache.find('corvid', 'standard')?.carrier).toBe('corvid');
    await cache.refresh();
    cache.dispose();
    expect(cache.find('corvid', 'standard')).toBeUndefined();
  });

  it('keeps the cached cards when a download fails or is malformed', async () => {
    let now = 0;
    const cache = new RateCardCache('https://rates.test/cards', silentLogger, {}, () => now);
    cache.load([rateCard()]);
    now = 16 * 60_000;
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.resolve(new Response('busy', { status: 503 }))),
    );
    await cache.refresh();
    vi.stubGlobal(
      'fetch',
      vi.fn(() => Promise.resolve(Response.json([{ carrier: 'alder' }]))),
    );
    await cache.refresh();
    expect(cache.carriers()).toEqual(['alder']);
  });
});

describe('RefreshTimer', () => {
  it('ticks until disposed', () => {
    vi.useFakeTimers();
    const tick = vi.fn();
    const timer = new RefreshTimer(1_000, tick);
    vi.advanceTimersByTime(2_500);
    timer.dispose();
    vi.advanceTimersByTime(5_000);
    expect(tick).toHaveBeenCalledTimes(2);
  });
});
