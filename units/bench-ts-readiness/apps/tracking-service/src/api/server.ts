import { once, type EventEmitter } from "node:events";
import { createServer } from "node:http";
import { loadApiConfig } from "../config.js";
import { databaseReachable } from "../db/connection.js";
import { createApp } from "../http/app.js";
import { TokenVerifier } from "../http/auth.js";
import { ShareLinks } from "../links/share-links.js";
import { createLogger } from "../logger.js";
import { ParcelStore } from "../parcels/parcel-store.js";
import { TrackingService } from "../parcels/tracking-service.js";
import { listen, openDatabase, stopRequested, type ProcessHooks } from "../process.js";

/** Runs the API until SIGTERM or SIGINT arrives on `signals`, then drains connections and closes the database. */
export async function runApi(
  env: NodeJS.ProcessEnv,
  signals: EventEmitter,
  hooks: ProcessHooks = {},
): Promise<void> {
  const config = loadApiConfig(env);
  const logger = createLogger(config.LOG_LEVEL, "api");
  const db = openDatabase(config, hooks);
  const stop = stopRequested(signals);

  const app = createApp({
    service: new TrackingService(new ParcelStore(db)),
    links: new ShareLinks(config.LINK_SIGNING_KEY, config.PUBLIC_BASE_URL),
    verifier: await TokenVerifier.create({
      publicKeyPem: config.JWT_PUBLIC_KEY,
      issuer: config.JWT_ISSUER,
      audience: config.JWT_AUDIENCE,
    }),
    health: { ready: () => databaseReachable(db) },
    logger,
  });
  const server = createServer(app);
  const { port } = await listen(server, config.PORT, hooks);
  logger.info({ port }, "api listening");

  await stop;
  logger.info("shutting down: draining connections");
  server.close();
  await once(server, "close");
  await db.destroy();
}
