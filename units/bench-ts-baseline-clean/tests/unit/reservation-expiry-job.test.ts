import { pino } from 'pino';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReservationService } from '../../src/application/reservations/reservation-service.js';
import { startReservationExpiry } from '../../src/infrastructure/reservation-expiry-job.js';
import { silentLogger } from '../support/silent-logger.js';

describe('reservation expiry job', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function reservationsExpiring(expireDue: () => number): ReservationService {
    const service: Pick<ReservationService, 'expireDue'> = { expireDue };
    return service as ReservationService;
  }

  it('sweeps on every interval until stopped', () => {
    let sweeps = 0;
    const job = startReservationExpiry(
      reservationsExpiring(() => ++sweeps),
      1_000,
      silentLogger,
    );

    vi.advanceTimersByTime(3_000);
    job.stop();
    vi.advanceTimersByTime(3_000);

    expect(sweeps).toBe(3);
  });

  it('keeps sweeping after a sweep fails, and logs the failure', () => {
    const logged: string[] = [];
    const logger = pino({ level: 'error' }, { write: (line: string) => logged.push(line) });
    let sweeps = 0;
    const job = startReservationExpiry(
      reservationsExpiring(() => {
        sweeps += 1;
        if (sweeps === 1) {
          throw new Error('store unavailable');
        }
        return 0;
      }),
      1_000,
      logger,
    );

    vi.advanceTimersByTime(2_000);
    job.stop();

    expect(sweeps).toBe(2);
    expect(logged).toHaveLength(1);
    expect(JSON.parse(logged[0] ?? '{}')).toMatchObject({
      msg: 'Reservation expiry sweep failed',
      err: { message: 'store unavailable' },
    });
  });
});
