import { pino, type DestinationStream, type Logger } from 'pino';
import type { LogLevel } from './config.js';

/** Structured JSON logs to stdout (or `destination`); credentials in request headers are redacted. */
export function createLogger(level: LogLevel, destination?: DestinationStream): Logger {
  const options = {
    level,
    base: { service: 'parcel-rates-service' },
    redact: ['req.headers.authorization', 'req.headers.cookie'],
  };
  return destination ? pino(options, destination) : pino(options);
}
