import { pino, type DestinationStream, type Logger } from 'pino';
import type { LogLevel } from './config.js';

/** Structured JSON logs to stdout (or `destination`); credentials in request headers are redacted. */
export function createLogger(level: LogLevel, destination?: DestinationStream): Logger {
  const options = {
    level,
    base: { service: 'depot-dispatch' },
    redact: ['req.headers.authorization', 'req.headers.cookie', 'req.headers["x-signature"]'],
  };
  return destination ? pino(options, destination) : pino(options);
}
