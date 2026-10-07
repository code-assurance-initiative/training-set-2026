import { randomUUID } from 'node:crypto';
import express, { type Express } from 'express';
import helmet from 'helmet';
import type { Logger } from 'pino';
import { pinoHttp } from 'pino-http';
import { authenticate, type AccessTokenVerifier } from './api/authentication.js';
import { errorHandler, notFoundHandler } from './api/errors.js';
import { healthRoutes } from './api/routes/health-routes.js';
import { labelRoutes } from './api/routes/label-routes.js';
import { quoteRoutes } from './api/routes/quote-routes.js';
import { webhookRoutes } from './api/routes/webhook-routes.js';
import { requireHttps } from './api/transport-security.js';
import type { Services } from './composition.js';

export interface AppOptions {
  readonly services: Services;
  readonly verifier: AccessTokenVerifier;
  readonly logger: Logger;
  readonly clock: () => Date;
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

  app.use(healthRoutes());
  app.use(
    requireHttps(options.requireHttps),
    webhookRoutes(options.services.labels, options.services.accounts),
  );
  app.use(
    '/api',
    requireHttps(options.requireHttps),
    express.json({ limit: '64kb' }),
    authenticate(options.verifier),
    quoteRoutes(options.services.quotes, options.clock),
    labelRoutes(options.services.labels),
  );

  app.use(notFoundHandler);
  app.use(errorHandler);
  return app;
}
