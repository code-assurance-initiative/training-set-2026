import type { EventEmitter } from "node:events";
import { loadWorkerConfig } from "../config.js";
import { databaseReachable } from "../db/connection.js";
import { checkCarriers } from "../health/carrier-health.js";
import { createLogger } from "../logger.js";
import { ParcelStore } from "../parcels/parcel-store.js";
import { systemClock, TrackingService } from "../parcels/tracking-service.js";
import { CarrierClient } from "./carrier-client.js";
import { DeliveryLog, ensureDeliveryLog } from "./delivery-log.js";
import { createHealthServer } from "./health-server.js";
import { CarrierPoller } from "./poller.js";
import { WebhookDispatcher } from "./webhook-dispatcher.js";
import { WebhookOutbox } from "./webhook-outbox.js";
import { listen, openDatabase, stopRequested, type ProcessHooks } from "../process.js";

export const carrierCodes = ["NPX", "BLC"] as const;

export interface WorkerHooks extends ProcessHooks {
  /** Replaces the fetch the carrier clients and the dispatcher use. */
  fetch?: typeof fetch;
}

/** Runs the poller and the dispatcher until SIGTERM or SIGINT arrives on `signals`, then finishes the round. */
export async function runWorker(
  env: NodeJS.ProcessEnv,
  signals: EventEmitter,
  hooks: WorkerHooks = {},
): Promise<void> {
  const config = loadWorkerConfig(env);
  const logger = createLogger(config.LOG_LEVEL, "worker");
  const db = openDatabase(config, hooks);
  await ensureDeliveryLog(db);

  const store = new ParcelStore(db);
  const carriers = carrierCodes.map(
    (carrierCode) =>
      new CarrierClient({
        carrierCode,
        baseUrl: new URL(`${carrierCode.toLowerCase()}/`, config.CARRIER_API_URL).toString(),
        apiKey: config.CARRIER_API_KEY,
        timeoutMs: config.CARRIER_TIMEOUT_MS,
        retry: { attempts: 3, baseDelayMs: 500, maxDelayMs: 4_000 },
        ...(hooks.fetch ? { fetch: hooks.fetch } : {}),
      }),
  );
  const poller = new CarrierPoller(store, new TrackingService(store), carriers, logger, {
    intervalMs: config.POLL_INTERVAL_MS,
    batchSize: config.POLL_BATCH_SIZE,
  });
  const dispatcher = new WebhookDispatcher(new WebhookOutbox(db), new DeliveryLog(db), {
    signingSecret: config.WEBHOOK_SIGNING_SECRET,
    batchSize: 50,
    clock: systemClock,
    ...(hooks.fetch ? { fetch: hooks.fetch } : {}),
  });

  const shutdown = new AbortController();
  const stop = stopRequested(signals);
  logger.info(
    { intervalMs: config.POLL_INTERVAL_MS, batchSize: config.POLL_BATCH_SIZE },
    "worker started: polling the carriers for every active parcel and " +
      "dispatching due merchant webhooks until SIGTERM",
  );
  const loops = Promise.all([poller.run(shutdown.signal), dispatcher.run(shutdown.signal, 5_000)]);

  const health = createHealthServer({
    live: () => !shutdown.signal.aborted,
    ready: async () =>
      (await databaseReachable(db)) && (await checkCarriers(carriers)).every((c) => c.reachable),
  });
  await listen(health, config.HEALTH_PORT, hooks);

  await stop;
  logger.info("shutting down: finishing the current round");
  shutdown.abort();
  await loops;
  health.close();
  await db.destroy();
}
