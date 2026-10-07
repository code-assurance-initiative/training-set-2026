import { once } from 'node:events';
import { createServer } from 'node:http';
import type { Logger } from 'pino';
import { createAccessTokenVerifier } from './api/authentication.js';
import { createApp } from './app.js';
import { composeServices, productionDependencies } from './composition.js';
import type { AppConfig } from './config.js';

export interface RunningService {
  readonly port: number;
  close(): Promise<void>;
}

/** Starts the HTTP server; resolves once the rate cards are loaded and the server is listening. */
export async function startService(config: AppConfig, logger: Logger): Promise<RunningService> {
  const verifier = await createAccessTokenVerifier(config.jwt);
  const dependencies = productionDependencies(config, logger);
  await dependencies.rateCards.refresh?.();
  const services = composeServices(config, dependencies, logger);
  const app = createApp({
    services,
    verifier,
    logger,
    clock: dependencies.clock,
    requireHttps: config.environment === 'production',
    trustProxy: config.trustProxy,
  });

  const server = createServer(app);
  server.listen(config.port, config.host);
  await once(server, 'listening');
  const address = server.address();
  const port = typeof address === 'object' && address !== null ? address.port : config.port;
  const purge = setInterval(() => {
    services.labels.purgeExpired().catch((error: unknown) => {
      logger.error({ err: error }, 'Label purge failed');
    });
  }, 3_600_000);
  purge.unref();
  logger.info({ host: config.host, port }, 'Parcel rates service listening');

  return {
    port,
    close: async () => {
      clearInterval(purge);
      services.dispose();
      server.close();
      server.closeIdleConnections();
      await once(server, 'close');
      logger.info('Parcel rates service stopped');
    },
  };
}
