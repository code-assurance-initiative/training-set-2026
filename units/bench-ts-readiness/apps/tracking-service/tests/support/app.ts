import { randomBytes } from "node:crypto";
import type { Knex } from "knex";
import { pino } from "pino";
import { createApp } from "../../src/http/app.js";
import { ShareLinks } from "../../src/links/share-links.js";
import { ParcelStore } from "../../src/parcels/parcel-store.js";
import { TrackingService } from "../../src/parcels/tracking-service.js";
import { createTestDatabase } from "./database.js";
import { createTestTokens, type TestTokens } from "./tokens.js";

export const silentLogger = pino({ level: "silent" });

export interface TestApi {
  app: ReturnType<typeof createApp>;
  db: Knex;
  service: TrackingService;
  tokens: TestTokens;
  ready: { value: boolean };
}

export async function createTestApi(): Promise<TestApi> {
  const db = await createTestDatabase();
  const tokens = await createTestTokens();
  const service = new TrackingService(new ParcelStore(db));
  const ready = { value: true };
  const app = createApp({
    service,
    links: new ShareLinks(randomBytes(32).toString("base64url"), "https://tracking.example.com/"),
    verifier: tokens.verifier,
    health: { ready: () => Promise.resolve(ready.value) },
    logger: silentLogger,
  });
  return { app, db, service, tokens, ready };
}
