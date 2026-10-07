import type { Logger } from 'pino';
import type { Services } from './composition.js';

export interface RunningJobs {
  stop(): Promise<void>;
}

/**
 * Runs the background work on a fixed interval, one round at a time: outbox dispatch, class
 * reminders and the retention purge. A failing job is logged and retried on the next round.
 */
export function startJobs(jobs: Services['jobs'], intervalMs: number, logger: Logger): RunningJobs {
  let round: Promise<void> = Promise.resolve();
  const runRound = async (): Promise<void> => {
    await attempt('outbox', () => jobs.outbox.dispatchPending(), logger);
    await attempt('class reminders', () => jobs.reminders.run(), logger);
    await attempt('retention purge', () => jobs.purgeLapsedMembers.run(), logger);
  };
  const timer = setInterval(() => {
    round = round.then(runRound);
  }, intervalMs);
  return {
    stop: async () => {
      clearInterval(timer);
      await round;
    },
  };
}

async function attempt(name: string, job: () => Promise<unknown>, logger: Logger): Promise<void> {
  try {
    await job();
  } catch (error) {
    logger.error({ err: error, job: name }, 'Background job failed');
  }
}
