import type { Logger } from 'pino';
import type { ReservationService } from '../application/reservations/reservation-service.js';

export interface BackgroundJob {
  stop(): void;
}

/**
 * Expires lapsed reservations on a fixed interval. New reservations also expire lapsed holds first,
 * so the sweep only keeps reads of reservation status current; a failed sweep is logged and the
 * next one runs as usual.
 */
export function startReservationExpiry(
  reservations: ReservationService,
  intervalMs: number,
  logger: Logger,
): BackgroundJob {
  const timer = setInterval(() => {
    try {
      reservations.expireDue();
    } catch (error) {
      logger.error({ err: error }, 'Reservation expiry sweep failed');
    }
  }, intervalMs);
  timer.unref();
  return {
    stop: () => {
      clearInterval(timer);
    },
  };
}
