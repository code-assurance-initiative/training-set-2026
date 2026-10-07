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
    stopOnSignals(service, logger, signals);
    return service;
  } catch (error) {
    bootLogger.fatal({ err: error }, 'Archive search API failed to start');
    return undefined;
  }
}

function stopOnSignals(service: RunningService, logger: Logger, signals: SignalSource): void {
  let stopping = false;
  const stop = (signal: NodeJS.Signals): void => {
    if (stopping) {
      return;
    }
    stopping = true;
    logger.info({ signal }, 'Shutdown requested');
    service.close().catch((error: unknown) => {
      logger.error({ err: error }, 'Shutdown failed');
      process.exitCode = 1;
    });
  };
  signals.once('SIGTERM', stop);
  signals.once('SIGINT', stop);
}
