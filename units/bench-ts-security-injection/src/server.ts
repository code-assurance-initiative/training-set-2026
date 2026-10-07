import { once } from 'node:events';
import { createServer } from 'node:http';
import type { Logger } from 'pino';
import { createApp } from './app.js';
import { compose } from './composition.js';
import type { AppConfig } from './config.js';
import { createAccessTokenVerifier } from './http/authentication.js';
import { startShareCleanup } from './shares/share-cleanup-job.js';

export interface RunningService {
  readonly port: number;
  close(): Promise<void>;
}

const shareCleanupIntervalMs = 6 * 60 * 60 * 1_000;

/** Starts the HTTP server and the share-link cleanup; resolves once the server is listening. */
export async function startService(config: AppConfig, logger: Logger): Promise<RunningService> {
  const verifier = await createAccessTokenVerifier(config.jwt);
  const infrastructure = compose(config, logger);
  const app = createApp({
    dependencies: infrastructure.dependencies,
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
  const cleanup = startShareCleanup(
    infrastructure.shareStore,
    shareCleanupIntervalMs,
    logger.child({ component: 'share-cleanup' }),
  );
  logger.info({ host: config.host, port }, 'Archive search API listening');

  return {
    port,
    close: async () => {
      cleanup.stop();
      server.close();
      server.closeIdleConnections();
      await once(server, 'close');
      await infrastructure.close();
      logger.info('Archive search API stopped');
    },
  };
}
