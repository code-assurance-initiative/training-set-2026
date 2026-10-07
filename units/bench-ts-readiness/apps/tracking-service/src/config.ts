import { z } from "zod";

const logLevel = z
  .enum(["fatal", "error", "warn", "info", "debug", "trace", "silent"])
  .default("info");

// 0 asks the operating system for any free port (tests and local runs of several instances).
const port = (fallback: number) => z.coerce.number().int().min(0).max(65535).default(fallback);

const database = {
  DATABASE_URL: z.url({ protocol: /^postgres(ql)?$/ }),
  DATABASE_POOL_MAX: z.coerce.number().int().min(1).max(50).default(10),
  LOG_LEVEL: logLevel,
};

const apiSchema = z.object({
  ...database,
  PORT: port(8080),
  PUBLIC_BASE_URL: z.url({ protocol: /^https$/ }),
  JWT_PUBLIC_KEY: z.string().includes("BEGIN PUBLIC KEY"),
  JWT_ISSUER: z.url(),
  JWT_AUDIENCE: z.string().min(1),
  LINK_SIGNING_KEY: z.string().min(32),
});

const workerSchema = z.object({
  ...database,
  HEALTH_PORT: port(8081),
  CARRIER_API_URL: z.url({ protocol: /^https$/ }),
  CARRIER_API_KEY: z.string().min(16),
  CARRIER_TIMEOUT_MS: z.coerce.number().int().min(100).max(30_000).default(5_000),
  POLL_INTERVAL_MS: z.coerce.number().int().min(1_000).default(60_000),
  POLL_BATCH_SIZE: z.coerce.number().int().min(1).max(500).default(100),
  WEBHOOK_SIGNING_SECRET: z.string().min(32),
});

const migrationSchema = z.object(database);

export type ApiConfig = z.infer<typeof apiSchema>;
export type WorkerConfig = z.infer<typeof workerSchema>;
export type MigrationConfig = z.infer<typeof migrationSchema>;

export class ConfigError extends Error {
  constructor(readonly issues: string[]) {
    super(`Invalid configuration: ${issues.join("; ")}`);
    this.name = "ConfigError";
  }
}

function parse<T>(schema: z.ZodType<T>, env: NodeJS.ProcessEnv): T {
  const result = schema.safeParse(env);
  if (!result.success) {
    // Name the variables only: values may be secrets.
    throw new ConfigError(
      result.error.issues.map((issue) => `${issue.path.join(".")}: ${issue.message}`),
    );
  }
  return result.data;
}

export const loadApiConfig = (env: NodeJS.ProcessEnv): ApiConfig => parse(apiSchema, env);

export const loadWorkerConfig = (env: NodeJS.ProcessEnv): WorkerConfig => parse(workerSchema, env);

export const loadMigrationConfig = (env: NodeJS.ProcessEnv): MigrationConfig =>
  parse(migrationSchema, env);
