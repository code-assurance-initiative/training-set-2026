import { setTimeout as delay } from 'timers/promises';
import type { Logger } from 'pino';
import { loadConfig } from './config.js';
import { createLogger } from './logger.js';
import { startService, type RunningService } from './server.js';

/** Where shutdown signals come from: the process itself, or an event emitter in tests. */
export interface SignalSource {
  once(signal: NodeJS.Signals, listener: (signal: NodeJS.Signals) => void): unknown;
}

/**
 * Starts the service from environment configuration and stops it on the first SIGTERM or SIGINT.
 * Resolves to undefined, after logging why, when the service cannot start.
 */
export async function run(
  environment: NodeJS.ProcessEnv,
  signals: SignalSource,
  bootLogger: Logger = createLogger('info'),
): Promise<RunningService | undefined> {
  try {
    const config = loadConfig(environment);
    const logger = createLogger(config.logLevel);
    const service = await startService(config, logger);
    stopOnSignals(service, logger, signals, config.shutdownGraceMs);
    return service;
  } catch (error) {
    bootLogger.fatal({ err: error }, 'Depot dispatch failed to start');
    return undefined;
  }
}

/**
 * On the first signal, stops accepting connections and waits for in-flight requests (label
 * downloads take seconds) — but never longer than the grace period, after which the remaining
 * connections are dropped.
 */
export function stopOnSignals(
  service: RunningService,
  logger: Logger,
  signals: SignalSource,
  graceMs: number,
): void {
  let stopping = false;
  const stop = (signal: NodeJS.Signals): void => {
    if (stopping) {
      return;
    }
    stopping = true;
    logger.info({ signal, graceMs }, 'Shutdown requested');
    const deadline = new AbortController();
    void Promise.race([
      service.close().then(() => 'closed' as const),
      delay(graceMs, 'timed-out' as const, { signal: deadline.signal, ref: false }),
    ])
      .then((outcome) => {
        if (outcome === 'timed-out') {
          logger.warn({ graceMs }, 'In-flight requests did not finish within the grace period');
          service.terminate();
        }
      })
      .catch((error: unknown) => {
        logger.error({ err: error }, 'Shutdown failed');
        process.exitCode = 1;
      })
      .finally(() => {
        deadline.abort();
      });
  };
  signals.once('SIGTERM', stop);
  signals.once('SIGINT', stop);
}
