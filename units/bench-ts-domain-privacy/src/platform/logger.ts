import { pino, type DestinationStream, type Logger } from 'pino';
import type { LogLevel } from '../config.js';

/**
 * Structured JSON logs to stdout (or `destination`). Credentials in request headers are redacted;
 * personal data is never logged in clear (docs/privacy/data-inventory.md) — log a pseudonym instead.
 */
export function createLogger(level: LogLevel, destination?: DestinationStream): Logger {
  const options = {
    level,
    base: { service: 'club-membership-service' },
    redact: ['req.headers.authorization', 'req.headers.cookie'],
  };
  return destination ? pino(options, destination) : pino(options);
}
