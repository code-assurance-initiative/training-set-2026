import "dotenv/config";

import pino from "pino";

import { buildApp } from "./app.js";
import { LogOnlyAuditLog, MongoAuditLog, type AuditLog } from "./audit/auditLog.js";
import { UploadSignatureVerifier } from "./auth/uploadSignature.js";
import { PlanUpgrades } from "./billing/planUpgrades.js";
import { loadConfig } from "./config.js";
import { PostgresMediaRepository } from "./db/mediaRepository.js";
import { createPool } from "./db/pool.js";
import { EmailNotifier } from "./notify/email.js";
import { PushNotifier } from "./notify/push.js";
import { SlackAlerts } from "./notify/slack.js";
import { UploadEvents } from "./notify/uploadEvents.js";
import { Thumbnailer } from "./media/thumbnailer.js";
import { UploadService } from "./media/uploadService.js";
import { createS3Client } from "./storage/s3Client.js";
import { S3ObjectStore, objectStoreConfigFromEnv } from "./storage/objectStore.js";
import { TranscoderClient, createTranscoderHttp } from "./transcode/transcoderClient.js";
import { WebhookSigner } from "./webhooks/signer.js";

const config = loadConfig();
const log = pino({ name: "media-intake" });

if (config.uploadSigningSecret === undefined || config.stripeRestrictedKey === undefined) {
  log.fatal("UPLOAD_SIGNING_SECRET and STRIPE_RESTRICTED_KEY must be set");
  process.exit(1);
}

const storeConfig = objectStoreConfigFromEnv();
const store = new S3ObjectStore(createS3Client(storeConfig), storeConfig.bucket);
const pool = createPool(config);
const audit: AuditLog =
  config.auditMongoUri === undefined
    ? new LogOnlyAuditLog((event) => log.info({ audit: event }, "audit event"))
    : await MongoAuditLog.connect(config.auditMongoUri);

const events = new UploadEvents(
  {
    push: new PushNotifier(config),
    alerts: new SlackAlerts(log),
    signer: await WebhookSigner.fromPemFile(config.webhookSigningKeyPath),
    webhookSubscriberUrl: config.webhookSubscriberUrl,
  },
  log,
);

const app = await buildApp(
  {
    uploads: new UploadService(store, new PostgresMediaRepository(pool), audit),
    signatures: new UploadSignatureVerifier(config.uploadSigningSecret),
    thumbnailer: new Thumbnailer(new TranscoderClient(createTranscoderHttp(config)), config.publicBaseUrl, log),
    events,
    email: new EmailNotifier(config),
    plans: new PlanUpgrades(config.stripeRestrictedKey),
    audit,
    publicBaseUrl: config.publicBaseUrl,
  },
  { maxUploadBytes: config.maxUploadBytes, logger: { name: "media-intake" } },
);

const shutdown = async (signal: string): Promise<void> => {
  log.info({ signal }, "shutting down");
  await app.close();
  await audit.close();
  await pool.end();
  process.exit(0);
};
process.once("SIGTERM", () => void shutdown("SIGTERM"));
process.once("SIGINT", () => void shutdown("SIGINT"));

await app.listen({ host: "0.0.0.0", port: config.port });
