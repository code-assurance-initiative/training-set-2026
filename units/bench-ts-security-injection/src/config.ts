import { z } from 'zod';

const logLevels = ['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent'] as const;

const hostList = z
  .string()
  .default('')
  .transform((value) =>
    value
      .split(',')
      .map((host) => host.trim().toLowerCase())
      .filter((host) => host.length > 0),
  );

const environmentSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('production'),
  HOST: z.string().min(1).default('127.0.0.1'),
  PORT: z.coerce.number().int().min(0).max(65_535).default(8080),
  LOG_LEVEL: z.enum(logLevels).default('info'),
  TRUST_PROXY: z.string().min(1).default('loopback'),
  JWT_ISSUER: z.url({ protocol: /^https$/ }),
  JWT_AUDIENCE: z.string().min(1).default('archive-search-api'),
  JWT_PUBLIC_KEY: z.string().min(1),
  DATABASE_URL: z.url({ protocol: /^postgres(ql)?$/ }),
  MONGODB_URL: z.url({ protocol: /^mongodb(\+srv)?$/ }),
  STORAGE_ROOT: z.string().min(1).default('/var/lib/archive'),
  SOFFICE_PATH: z.string().min(1).default('/usr/bin/soffice'),
  MAGICK_PATH: z.string().min(1).default('/usr/bin/magick'),
  CONVERSION_TIMEOUT_SECONDS: z.coerce.number().int().positive().default(120),
  PARTNER_FEED_HOSTS: hostList,
  CRM_BASE_URL: z.url({ protocol: /^https$/ }),
  MAIL_RELAY_URL: z.url({ protocol: /^https$/ }),
  PSEUDONYM_KEY: z.string().min(32),
  PUBLIC_BASE_URL: z.url({ protocol: /^https$/ }),
});

export type LogLevel = (typeof logLevels)[number];

export interface StoragePaths {
  readonly originals: string;
  readonly attachments: string;
  readonly templates: string;
  readonly exports: string;
  readonly auditLog: string;
}

export interface ConversionSettings {
  readonly sofficePath: string;
  readonly magickPath: string;
  readonly timeoutMs: number;
}

export interface AppConfig {
  readonly environment: 'development' | 'test' | 'production';
  readonly host: string;
  readonly port: number;
  readonly logLevel: LogLevel;
  readonly trustProxy: string;
  readonly jwt: {
    readonly issuer: string;
    readonly audience: string;
    readonly publicKeyPem: string;
  };
  readonly databaseUrl: string;
  readonly mongoUrl: string;
  readonly storage: StoragePaths;
  readonly conversion: ConversionSettings;
  readonly partnerFeedHosts: readonly string[];
  readonly crmBaseUrl: string;
  readonly mailRelayUrl: string;
  readonly pseudonymKey: string;
  readonly publicBaseUrl: string;
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
  const root = env.STORAGE_ROOT.replace(/\/+$/, '');
  return {
    environment: env.NODE_ENV,
    host: env.HOST,
    port: env.PORT,
    logLevel: env.LOG_LEVEL,
    trustProxy: env.TRUST_PROXY,
    jwt: { issuer: env.JWT_ISSUER, audience: env.JWT_AUDIENCE, publicKeyPem: env.JWT_PUBLIC_KEY },
    databaseUrl: env.DATABASE_URL,
    mongoUrl: env.MONGODB_URL,
    storage: {
      originals: `${root}/originals`,
      attachments: `${root}/attachments`,
      templates: `${root}/templates`,
      exports: `${root}/exports`,
      auditLog: `${root}/audit/exports.log`,
    },
    conversion: {
      sofficePath: env.SOFFICE_PATH,
      magickPath: env.MAGICK_PATH,
      timeoutMs: env.CONVERSION_TIMEOUT_SECONDS * 1_000,
    },
    partnerFeedHosts: env.PARTNER_FEED_HOSTS,
    crmBaseUrl: env.CRM_BASE_URL,
    mailRelayUrl: env.MAIL_RELAY_URL,
    pseudonymKey: env.PSEUDONYM_KEY,
    publicBaseUrl: env.PUBLIC_BASE_URL,
  };
}
