import { loadMigrationConfig } from "../config.js";
import { createLogger } from "../logger.js";
import { createDatabase, type Database } from "./connection.js";
import { migrateToLatest } from "./migrations/index.js";

/** Applies the pending migrations; returns false (after logging why) when they could not be applied. */
export async function runMigrations(
  env: NodeJS.ProcessEnv,
  openDatabase: (url: string, poolMax: number) => Database = createDatabase,
): Promise<boolean> {
  const config = loadMigrationConfig(env);
  const logger = createLogger(config.LOG_LEVEL, "migrate");
  const db = openDatabase(config.DATABASE_URL, 1);
  try {
    const applied = await migrateToLatest(db);
    logger.info({ applied }, applied.length > 0 ? "migrations applied" : "schema already current");
    return true;
  } catch (error) {
    logger.fatal({ err: error }, "migration failed");
    return false;
  } finally {
    await db.destroy();
  }
}
