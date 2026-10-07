import { once } from 'node:events';
import { createServer } from 'node:http';
import type { Logger } from 'pino';
import { createApp } from './app.js';
import { systemClock, type Clock } from './application/clock.js';
import { composeServices } from './composition.js';
import type { AppConfig } from './config.js';
import { createAccessTokenVerifier } from './http/authentication.js';
import { startReservationExpiry } from './infrastructure/reservation-expiry-job.js';

export interface RunningService {
  readonly port: number;
  close(): Promise<void>;
}

/** Starts the HTTP server and the reservation expiry job; resolves once the server is listening. */
export async function startService(
  config: AppConfig,
  logger: Logger,
  clock: Clock = systemClock,
): Promise<RunningService> {
  const verifier = await createAccessTokenVerifier(config.jwt);
  const services = composeServices(config.reservations, clock, logger);
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
  const expiry = startReservationExpiry(
    services.reservations,
    config.reservations.sweepIntervalMs,
    logger.child({ component: 'reservation-expiry' }),
  );
  logger.info({ host: config.host, port }, 'Warehouse stock API listening');

  return {
    port,
    close: async () => {
      expiry.stop();
      server.close();
      server.closeIdleConnections();
      await once(server, 'close');
      logger.info('Warehouse stock API stopped');
    },
  };
}
