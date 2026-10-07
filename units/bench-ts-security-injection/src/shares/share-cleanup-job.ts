import type { Logger } from 'pino';
import type { ShareStore } from './share-store.js';

export interface Job {
  stop(): void;
}

/** Deletes expired share links (and with them their password hashes) every `intervalMs`. */
export function startShareCleanup(
  shares: Pick<ShareStore, 'purgeExpired'>,
  intervalMs: number,
  logger: Logger,
  now: () => Date = () => new Date(),
): Job {
  const timer = setInterval(() => {
    shares.purgeExpired(now()).then(
      (removed) => {
        if (removed > 0) {
          logger.info({ removed }, 'Expired share links deleted');
        }
      },
      (error: unknown) => {
        logger.error({ err: error }, 'Share link cleanup failed');
      },
    );
  }, intervalMs);
  timer.unref();
  return {
    stop: () => {
      clearInterval(timer);
    },
  };
}
