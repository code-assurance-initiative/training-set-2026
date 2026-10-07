import { z } from 'zod';

const logLevels = ['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent'] as const;

const environmentSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('production'),
  HOST: z.string().min(1).default('127.0.0.1'),
  PORT: z.coerce.number().int().min(0).max(65_535).default(8080),
  LOG_LEVEL: z.enum(logLevels).default('info'),
  TRUST_PROXY: z.string().min(1).default('loopback'),
  JWT_ISSUER: z.url({ protocol: /^https$/ }),
  JWT_AUDIENCE: z.string().min(1).default('warehouse-stock-api'),
  JWT_PUBLIC_KEY: z.string().min(1),
  RESERVATION_DEFAULT_HOLD_MINUTES: z.coerce.number().int().positive().default(15),
  RESERVATION_MAX_HOLD_MINUTES: z.coerce.number().int().positive().default(1_440),
  RESERVATION_SWEEP_SECONDS: z.coerce.number().int().positive().default(30),
});

export type LogLevel = (typeof logLevels)[number];

export interface AppConfig {
  readonly environment: 'development' | 'test' | 'production';
  readonly host: string;
  readonly port: number;
  readonly logLevel: LogLevel;
  /** Express `trust proxy` setting: which proxy addresses may assert the client's protocol and address. */
  readonly trustProxy: string;
  readonly jwt: {
    readonly issuer: string;
    readonly audience: string;
    /** The issuer's ES256 public key (SPKI, PEM). It verifies tokens; it cannot sign them. */
    readonly publicKeyPem: string;
  };
  readonly reservations: {
    readonly defaultHoldMinutes: number;
    readonly maximumHoldMinutes: number;
    readonly sweepIntervalMs: number;
  };
}

export class ConfigurationError extends Error {
  constructor(readonly problems: readonly string[]) {
    super(`Invalid configuration: ${problems.join('; ')}`);
    this.name = 'ConfigurationError';
  }
}

/** Reads and validates the service configuration. Values are never echoed back in errors. */
export function loadConfig(environment: NodeJS.ProcessEnv): AppConfig {
  const parsed = environmentSchema.safeParse(environment);
  if (!parsed.success) {
    throw new ConfigurationError(
      parsed.error.issues.map((issue) => `${issue.path.join('.')}: ${issue.message}`),
    );
  }
  const env = parsed.data;
  if (env.RESERVATION_DEFAULT_HOLD_MINUTES > env.RESERVATION_MAX_HOLD_MINUTES) {
    throw new ConfigurationError([
      'RESERVATION_DEFAULT_HOLD_MINUTES: must not exceed RESERVATION_MAX_HOLD_MINUTES',
    ]);
  }
  return {
    environment: env.NODE_ENV,
    host: env.HOST,
    port: env.PORT,
    logLevel: env.LOG_LEVEL,
    trustProxy: env.TRUST_PROXY,
    jwt: { issuer: env.JWT_ISSUER, audience: env.JWT_AUDIENCE, publicKeyPem: env.JWT_PUBLIC_KEY },
    reservations: {
      defaultHoldMinutes: env.RESERVATION_DEFAULT_HOLD_MINUTES,
      maximumHoldMinutes: env.RESERVATION_MAX_HOLD_MINUTES,
      sweepIntervalMs: env.RESERVATION_SWEEP_SECONDS * 1_000,
    },
  };
}
