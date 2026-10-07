import { z } from 'zod';

const logLevels = ['fatal', 'error', 'warn', 'info', 'debug', 'trace', 'silent'] as const;

const environmentSchema = z.object({
  NODE_ENV: z.enum(['development', 'test', 'production']).default('production'),
  HOST: z.string().min(1).default('127.0.0.1'),
  PORT: z.coerce.number().int().min(0).max(65_535).default(8080),
  LOG_LEVEL: z.enum(logLevels).default('info'),
  SHUTDOWN_GRACE_SECONDS: z.coerce.number().int().min(0).max(120).default(10),
  TERMINAL_TOKEN_ISSUER: z.string().min(1).default('depot-identity'),
  TERMINAL_TOKEN_AUDIENCE: z.string().min(1).default('depot-dispatch'),
  TERMINAL_TOKEN_PUBLIC_KEY: z.string().includes('BEGIN PUBLIC KEY'),
  LINEHAUL_BASE_URL: z.url({ protocol: /^https$/ }),
  LINEHAUL_API_KEY: z.string().min(16),
  GEOCODING_API_KEY: z.string().min(16),
  CARRIER_STATUS_PUBLIC_KEY: z
    .base64()
    .refine((value) => Buffer.from(value, 'base64').length === 32, {
      message: 'must be a base64 Ed25519 public key (32 bytes)',
    }),
  LABEL_PRINTER_DPI: z.coerce.number().int().min(150).max(600).default(203),
});

export type LogLevel = (typeof logLevels)[number];

export interface AppConfig {
  readonly environment: 'development' | 'test' | 'production';
  readonly host: string;
  readonly port: number;
  readonly logLevel: LogLevel;
  readonly shutdownGraceMs: number;
  readonly terminalTokens: {
    readonly issuer: string;
    readonly audience: string;
    /** The depot identity service's RS256 public key (SPKI, PEM). It verifies tokens; it cannot sign them. */
    readonly publicKeyPem: string;
  };
  readonly linehaul: { readonly baseUrl: string; readonly apiKey: string };
  readonly geocodingApiKey: string;
  /** The carrier's Ed25519 key for the signed status feed. */
  readonly carrierStatusPublicKey: Uint8Array;
  readonly labelPrinterDpi: number;
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
    shutdownGraceMs: env.SHUTDOWN_GRACE_SECONDS * 1_000,
    terminalTokens: {
      issuer: env.TERMINAL_TOKEN_ISSUER,
      audience: env.TERMINAL_TOKEN_AUDIENCE,
      publicKeyPem: env.TERMINAL_TOKEN_PUBLIC_KEY,
    },
    linehaul: { baseUrl: env.LINEHAUL_BASE_URL, apiKey: env.LINEHAUL_API_KEY },
    geocodingApiKey: env.GEOCODING_API_KEY,
    carrierStatusPublicKey: new Uint8Array(Buffer.from(env.CARRIER_STATUS_PUBLIC_KEY, 'base64')),
    labelPrinterDpi: env.LABEL_PRINTER_DPI,
  };
}
