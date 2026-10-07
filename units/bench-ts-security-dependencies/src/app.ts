import { randomUUID } from 'node:crypto';
import express, { type Express } from 'express';
import helmet from 'helmet';
import type { Logger } from 'pino';
import { pinoHttp } from 'pino-http';
import type { Services } from './composition.js';
import { errorHandler, notFoundHandler } from './http/errors.js';
import { authenticateTerminal } from './http/require-terminal.js';
import { healthRoutes } from './http/routes/health-routes.js';
import { labelRoutes } from './http/routes/label-routes.js';
import { runRoutes } from './http/routes/run-routes.js';
import { webhookRoutes } from './http/routes/webhook-routes.js';

export function createApp(services: Services, logger: Logger): Express {
  const app = express();
  app.disable('x-powered-by');

  app.use(
    pinoHttp({
      logger,
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
  app.use(webhookRoutes(services.store, services.signatures));
  app.use(
    '/api',
    express.json({ limit: '512kb' }),
    authenticateTerminal(services.tokens),
    runRoutes(services.dispatch, services.linehaul),
    labelRoutes(services.dispatch, services.linehaul, services.rasteriser),
  );

  app.use(notFoundHandler);
  app.use(errorHandler);
  return app;
}
