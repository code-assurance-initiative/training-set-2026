import { randomUUID } from 'node:crypto';
import express, { type Express } from 'express';
import helmet from 'helmet';
import type { Logger } from 'pino';
import { pinoHttp } from 'pino-http';
import type { Services } from './composition.js';
import { authenticate, type AccessTokenVerifier } from './http/authentication.js';
import { errorHandler, notFoundHandler } from './http/errors.js';
import { catalogRoutes } from './http/routes/catalog-routes.js';
import { healthRoutes } from './http/routes/health-routes.js';
import { reservationRoutes } from './http/routes/reservation-routes.js';
import { stockRoutes } from './http/routes/stock-routes.js';
import { requireHttps } from './http/transport-security.js';

export interface AppOptions {
  readonly services: Services;
  readonly verifier: AccessTokenVerifier;
  readonly logger: Logger;
  /** Refuse plain-HTTP requests (everywhere except local development and tests). */
  readonly requireHttps: boolean;
  readonly trustProxy: string;
}

export function createApp(options: AppOptions): Express {
  const app = express();
  app.disable('x-powered-by');
  app.set('trust proxy', options.trustProxy);

  app.use(
    pinoHttp({
      logger: options.logger,
      genReqId: (_req, res) => {
        const id = randomUUID();
        res.setHeader('X-Request-Id', id);
        return id;
      },
    }),
  );
  app.use(
    helmet({
      contentSecurityPolicy: {
        useDefaults: false,
        directives: { defaultSrc: ["'none'"], frameAncestors: ["'none'"] },
      },
    }),
  );
  app.use(express.json({ limit: '16kb' }));

  app.use(healthRoutes());
  app.use(
    '/api',
    requireHttps(options.requireHttps),
    authenticate(options.verifier),
    catalogRoutes(options.services.catalog),
    stockRoutes(options.services.stock),
    reservationRoutes(options.services.reservations),
  );

  app.use(notFoundHandler);
  app.use(errorHandler);
  return app;
}
