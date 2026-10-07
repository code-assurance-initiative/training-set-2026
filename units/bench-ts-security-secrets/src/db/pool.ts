import pg from "pg";

import type { ServiceConfig } from "../config.js";

export function createPool(config: ServiceConfig): pg.Pool {
  return new pg.Pool({
    connectionString:
      config.databaseUrl ??
      "postgres://media_app:q969uHdVktDHjOv635kHcd8Z@pg-shared.media-intake.internal:5432/media",
    max: 10,
    idleTimeoutMillis: 30_000,
    application_name: "media-intake",
  });
}
