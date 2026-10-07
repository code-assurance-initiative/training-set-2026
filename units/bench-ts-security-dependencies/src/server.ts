import { once } from 'node:events';
import { createServer } from 'node:http';
import type { Logger } from 'pino';
import { createApp } from './app.js';
import { composeServices, type Gateways } from './composition.js';
import type { AppConfig } from './config.js';

export interface RunningService {
  readonly port: number;
  /** Stops accepting connections and resolves once in-flight requests have finished. */
  close(): Promise<void>;
  /** Drops every open connection, finished or not. */
  terminate(): void;
}

/** Starts the HTTP server; resolves once it is listening. */
export async function startService(
  config: AppConfig,
  logger: Logger,
  gateways: Gateways = {},
): Promise<RunningService> {
  const app = createApp(composeServices(config, gateways), logger);
  const server = createServer(app);
  server.listen(config.port, config.host);
  await once(server, 'listening');
  const address = server.address();
  const port = typeof address === 'object' && address !== null ? address.port : config.port;
  logger.info({ host: config.host, port }, 'Depot dispatch listening');

  return {
    port,
    close: async () => {
      server.close();
      server.closeIdleConnections();
      await once(server, 'close');
      logger.info('Depot dispatch stopped');
    },
    terminate: () => {
      server.closeAllConnections();
    },
  };
}
