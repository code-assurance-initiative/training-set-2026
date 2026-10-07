import express, { type Express } from "express";
import helmet from "helmet";
import { pinoHttp } from "pino-http";
import type { Logger } from "pino";
import { authenticate, type TokenVerifier } from "./auth.js";
import { errorHandler, sendProblem } from "./errors.js";
import { healthRouter, type HealthChecks } from "./routes/health.js";
import { merchantsRouter } from "./routes/merchants.js";
import { parcelsRouter } from "./routes/parcels.js";
import { publicTrackingRouter } from "./routes/public-tracking.js";
import type { ShareLinks } from "../links/share-links.js";
import type { TrackingService } from "../parcels/tracking-service.js";

export interface AppDependencies {
  service: TrackingService;
  links: ShareLinks;
  verifier: TokenVerifier;
  health: HealthChecks;
  logger: Logger;
}

export function createApp(deps: AppDependencies): Express {
  const app = express();
  app.disable("x-powered-by");
  app.set("trust proxy", "loopback, uniquelocal");
  app.use(helmet());
  app.use(
    pinoHttp({ logger: deps.logger, autoLogging: { ignore: (req) => req.url === "/healthz" } }),
  );
  app.use(express.json({ limit: "16kb" }));

  app.use(healthRouter(deps.health));
  app.use(publicTrackingRouter(deps.service, deps.links));
  app.use("/v1", merchantsRouter(deps.service));
  app.use("/v1", authenticate(deps.verifier), parcelsRouter(deps.service, deps.links));

  app.use((_req, res) => {
    sendProblem(res, 404, "Not found");
  });
  app.use(errorHandler(deps.logger));
  return app;
}
