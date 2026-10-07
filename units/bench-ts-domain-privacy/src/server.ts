import { once } from 'node:events';
import { createServer } from 'node:http';
import type { Logger } from 'pino';
import { createApp } from './app.js';
import { composeServices } from './composition.js';
import type { AppConfig } from './config.js';
import { startJobs } from './jobs.js';
import { systemClock, type Clock } from './platform/clock.js';
import { createDatabase } from './platform/db/database.js';
import { createAccessTokenVerifier } from './platform/http/authentication.js';

export interface RunningService {
  readonly port: number;
  close(): Promise<void>;
}

/** Starts the HTTP server and the background jobs; resolves once the server is listening. */
export async function startService(
  config: AppConfig,
  logger: Logger,
  clock: Clock = systemClock,
): Promise<RunningService> {
  const verifier = await createAccessTokenVerifier(config.jwt);
  const db = createDatabase(config.databaseUrl);
  const services = composeServices({ config, db, logger, clock });
  const app = createApp({
    services,
    verifier,
    logger,
    requireHttps: config.environment === 'production',
    trustProxy: config.trustProxy,
  });

  const server = createServer(app);
  server.listen(config.port, config.host);
  await once(server, 'listening');
  const address = server.address();
  const port = typeof address === 'object' && address !== null ? address.port : config.port;
  const jobs = startJobs(services.jobs, config.jobIntervalMs, logger.child({ component: 'jobs' }));
  logger.info({ host: config.host, port }, 'Club membership service listening');

  return {
    port,
    close: async () => {
      await jobs.stop();
      server.close();
      server.closeIdleConnections();
      await once(server, 'close');
      await db.destroy();
      logger.info('Club membership service stopped');
    },
  };
}
