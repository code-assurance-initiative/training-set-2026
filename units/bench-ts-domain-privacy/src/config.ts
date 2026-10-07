import { z } from 'zod';
import type { ProviderEndpoint } from './platform/provider-client.js';

const logLevels = ['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent'] as const;

const base64Key = (bytes: number, exact: boolean) =>
  z.base64().refine(
    (value) => {
      const length = Buffer.from(value, 'base64').length;
      return exact ? length === bytes : length >= bytes;
    },
    { message: `must be a base64-encoded key of ${exact ? '' : 'at least '}${bytes} bytes` },
  );

const environmentSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('production'),
  HOST: z.string().min(1).default('127.0.0.1'),
  PORT: z.coerce.number().int().min(0).max(65_535).default(8080),
  LOG_LEVEL: z.enum(logLevels).default('info'),
  TRUST_PROXY: z.string().min(1).default('loopback'),
  JWT_ISSUER: z.url({ protocol: /^https$/ }),
  JWT_AUDIENCE: z.string().min(1).default('club-membership-service'),
  JWT_PUBLIC_KEY: z.string().min(1),
  DATABASE_URL: z.url({ protocol: /^postgres(ql)?$/ }),
  FIELD_ENCRYPTION_KEY: base64Key(32, true),
  PSEUDONYM_KEY: base64Key(32, false),
  EMAIL_API_URL: z.url({ protocol: /^https$/ }),
  EMAIL_API_TOKEN: z.string().min(1),
  EMAIL_SENDER: z.email(),
  SMS_API_URL: z.url({ protocol: /^https$/ }),
  SMS_API_TOKEN: z.string().min(1),
  SMS_SENDER: z.string().min(1).max(11),
  REMINDER_LEAD_MINUTES: z.coerce.number().int().positive().default(1_440),
  MEMBER_RETENTION_MONTHS: z.coerce.number().int().min(1).default(24),
  CURRENCY: z
    .string()
    .regex(/^[A-Z]{3}$/)
    .default('EUR'),
  MONTHLY_FEE_MINOR: z.coerce.number().int().positive().default(3_900),
  LATE_FEE_MINOR: z.coerce.number().int().nonnegative().default(500),
  PAYMENT_TERM_DAYS: z.coerce.number().int().positive().default(14),
  JOB_INTERVAL_SECONDS: z.coerce.number().int().positive().default(60),
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
    readonly publicKeyPem: string;
  };
  readonly databaseUrl: string;
  /** AES-256 key for personal-data fields (ADR 0004). */
  readonly fieldEncryptionKey: Buffer;
  /** HMAC key for pseudonyms in logs. */
  readonly pseudonymKey: Buffer;
  readonly email: ProviderEndpoint;
  readonly sms: ProviderEndpoint;
  readonly reminderLeadMinutes: number;
  readonly memberRetentionMonths: number;
  readonly billing: {
    readonly currency: string;
    readonly monthlyFeeMinor: number;
    readonly lateFeeMinor: number;
    readonly paymentTermDays: number;
  };
  readonly jobIntervalMs: number;
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
  return {
    environment: env.NODE_ENV,
    host: env.HOST,
    port: env.PORT,
    logLevel: env.LOG_LEVEL,
    trustProxy: env.TRUST_PROXY,
    jwt: { issuer: env.JWT_ISSUER, audience: env.JWT_AUDIENCE, publicKeyPem: env.JWT_PUBLIC_KEY },
    databaseUrl: env.DATABASE_URL,
    fieldEncryptionKey: Buffer.from(env.FIELD_ENCRYPTION_KEY, 'base64'),
    pseudonymKey: Buffer.from(env.PSEUDONYM_KEY, 'base64'),
    email: {
      endpoint: new URL(env.EMAIL_API_URL),
      apiToken: env.EMAIL_API_TOKEN,
      sender: env.EMAIL_SENDER,
    },
    sms: {
      endpoint: new URL(env.SMS_API_URL),
      apiToken: env.SMS_API_TOKEN,
      sender: env.SMS_SENDER,
    },
    reminderLeadMinutes: env.REMINDER_LEAD_MINUTES,
    memberRetentionMonths: env.MEMBER_RETENTION_MONTHS,
    billing: {
      currency: env.CURRENCY,
      monthlyFeeMinor: env.MONTHLY_FEE_MINOR,
      lateFeeMinor: env.LATE_FEE_MINOR,
      paymentTermDays: env.PAYMENT_TERM_DAYS,
    },
    jobIntervalMs: env.JOB_INTERVAL_SECONDS * 1_000,
  };
}
