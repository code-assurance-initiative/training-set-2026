import { z } from 'zod';

const logLevels = ['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent'] as const;

const environmentSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('production'),
  HOST: z.string().min(1).default('127.0.0.1'),
  PORT: z.coerce.number().int().min(0).max(65_535).default(8080),
  LOG_LEVEL: z.enum(logLevels).default('info'),
  TRUST_PROXY: z.string().min(1).default('loopback'),
  JWT_ISSUER: z.url({ protocol: /^https$/ }),
  JWT_AUDIENCE: z.string().min(1).default('parcel-rates-service'),
  JWT_PUBLIC_KEY: z.string().min(1),
  ALDER_BASE_URL: z.url({ protocol: /^https$/ }).default('https://api.alder-parcel.test/v2'),
  CORVID_BASE_URL: z.url({ protocol: /^https$/ }).default('https://gateway.corvid-courier.test'),
  RATE_CARD_URL: z.url({ protocol: /^https$/ }).default('https://rates.parcel-rates.test/cards'),
  LABEL_ARCHIVE_DIR: z.string().min(1).default('./var/labels'),
  LABEL_FROM_ADDRESS: z.email().optional(),
  LABEL_RETENTION_DAYS: z.coerce.number().int().positive().default(90),
  MAIL_PICKUP_DIR: z.string().min(1).default('./var/outbox'),
  ALDER_API_KEY: z.string().min(16),
  CORVID_ACCOUNT: z.string().min(1),
  CORVID_TOKEN: z.string().min(16),
  WEBHOOK_SECRET: z.string().min(32),
  CUTOFF_ALDER: z.string().default('16:00'),
  CUTOFF_CORVID: z.string().default('15:30'),
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
  readonly carriers: {
    readonly alder: {
      readonly baseUrl: string;
      readonly apiKey: string;
      readonly timeoutMs: number;
    };
    readonly corvid: {
      readonly baseUrl: string;
      readonly accountNumber: string;
      readonly token: string;
    };
    readonly cutoffs: Readonly<Record<string, string>>;
  };
  readonly rateCardUrl: string;
  readonly labels: {
    readonly archiveDir: string;
    readonly fromAddress: string | undefined;
    readonly retentionDays: number;
  };
  readonly mailPickupDir: string;
  /** Shared secret the carriers sign tracking webhooks with. */
  readonly webhookSecret: string;
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
    carriers: {
      alder: { baseUrl: env.ALDER_BASE_URL, apiKey: env.ALDER_API_KEY, timeoutMs: 10_000 },
      corvid: {
        baseUrl: env.CORVID_BASE_URL,
        accountNumber: env.CORVID_ACCOUNT,
        token: env.CORVID_TOKEN,
      },
      cutoffs: { alder: env.CUTOFF_ALDER, corvid: env.CUTOFF_CORVID },
    },
    rateCardUrl: env.RATE_CARD_URL,
    labels: {
      archiveDir: env.LABEL_ARCHIVE_DIR,
      fromAddress: env.LABEL_FROM_ADDRESS,
      retentionDays: env.LABEL_RETENTION_DAYS,
    },
    mailPickupDir: env.MAIL_PICKUP_DIR,
    webhookSecret: env.WEBHOOK_SECRET,
  };
}
