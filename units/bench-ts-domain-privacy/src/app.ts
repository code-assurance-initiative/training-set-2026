import { randomUUID } from 'node:crypto';
import express, { type Express } from 'express';
import helmet from 'helmet';
import type { Logger } from 'pino';
import { pinoHttp } from 'pino-http';
import { billingRoutes } from './billing/routes.js';
import type { Services } from './composition.js';
import { membershipRoutes } from './membership/routes.js';
import { authenticate, type AccessTokenVerifier } from './platform/http/authentication.js';
import { errorHandler, notFoundHandler } from './platform/http/errors.js';
import { healthRoutes } from './platform/http/health.js';
import { requireHttps } from './platform/http/transport-security.js';

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

  app.use(healthRoutes(options.services.db));
  app.use(
    '/api',
    requireHttps(options.requireHttps),
    authenticate(options.verifier),
    membershipRoutes(options.services),
    billingRoutes(options.services),
  );

  app.use(notFoundHandler);
  app.use(errorHandler);
  return app;
}
