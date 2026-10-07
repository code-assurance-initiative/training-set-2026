import { loadConfig } from './config.js';
import { createDatabase } from './platform/db/database.js';
import { migrateToLatest } from './platform/db/migrations.js';
import { createLogger } from './platform/logger.js';

/** Applies the pending database migrations (run by the deployment before the new version starts). */
const config = loadConfig(process.env);
const logger = createLogger(config.logLevel);
const db = createDatabase(config.databaseUrl);
try {
  const applied = await migrateToLatest(db);
  logger.info({ applied }, 'Database migrated');
} catch (error) {
  logger.fatal({ err: error }, 'Database migration failed');
  process.exitCode = 1;
} finally {
  await db.destroy();
}
