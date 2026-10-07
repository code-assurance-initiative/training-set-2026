import { pino, type Logger } from "pino";

export type { Logger };

export function createLogger(level: string, name: string): Logger {
  return pino({
    name,
    level,
    redact: {
      paths: ["req.headers.authorization", "req.headers.cookie", "*.apiKey", "*.secret"],
      censor: "[redacted]",
    },
  });
}
