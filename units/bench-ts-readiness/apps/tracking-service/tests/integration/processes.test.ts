import { EventEmitter } from "node:events";
import type { AddressInfo } from "node:net";
import { exportSPKI, generateKeyPair } from "jose";
import { describe, expect, it } from "vitest";
import { runApi } from "../../src/api/server.js";
import { runWorker } from "../../src/worker/worker.js";
import { createTestDatabase } from "../support/database.js";
import { FakeCarrier } from "../support/fake-carrier.js";

const database = { DATABASE_URL: "postgres://tracking@db.test:5432/tracking", LOG_LEVEL: "silent" };

function listeningOnce(): { address: Promise<AddressInfo>; listening: (a: AddressInfo) => void } {
  let listening!: (a: AddressInfo) => void;
  const address = new Promise<AddressInfo>((resolve) => {
    listening = resolve;
  });
  return { address, listening };
}

describe("api process", () => {
  it("serves until SIGTERM, then drains and closes the database", async () => {
    const db = await createTestDatabase();
    const { publicKey } = await generateKeyPair("ES256");
    const signals = new EventEmitter();
    const { address, listening } = listeningOnce();

    const running = runApi(
      {
        ...database,
        PORT: "0",
        PUBLIC_BASE_URL: "https://tracking.example.com",
        JWT_PUBLIC_KEY: await exportSPKI(publicKey),
        JWT_ISSUER: "https://login.example.com/",
        JWT_AUDIENCE: "parcel-tracking",
        LINK_SIGNING_KEY: "l".repeat(32),
      },
      signals,
      { openDatabase: () => db, listening },
    );
    const { port } = await address;

    const health = await fetch(`http://127.0.0.1:${port}/healthz`);
    const ready = await fetch(`http://127.0.0.1:${port}/readyz`);
    expect([health.status, ready.status]).toEqual([200, 200]);

    signals.emit("SIGTERM");
    await running;
    await expect(db.raw("select 1")).rejects.toThrow();
  });
});

describe("worker process", () => {
  it("serves its health endpoints until SIGTERM, then stops its loops", async () => {
    const db = await createTestDatabase();
    const carrier = new FakeCarrier();
    const signals = new EventEmitter();
    const { address, listening } = listeningOnce();

    const running = runWorker(
      {
        ...database,
        HEALTH_PORT: "0",
        CARRIER_API_URL: "https://carriers.test/",
        CARRIER_API_KEY: "carrier-test-key",
        WEBHOOK_SIGNING_SECRET: "w".repeat(32),
      },
      signals,
      { openDatabase: () => db, fetch: carrier.fetch, listening },
    );
    const base = `http://127.0.0.1:${(await address).port}`;

    expect((await fetch(`${base}/healthz`)).status).toBe(200);
    expect((await fetch(`${base}/readyz`)).status).toBe(200);
    expect((await fetch(`${base}/healthz`, { method: "POST" })).status).toBe(405);
    expect((await fetch(`${base}/metrics`)).status).toBe(404);
    expect(carrier.requests.map((r) => r.url)).toEqual([
      "https://carriers.test/npx/v2/status",
      "https://carriers.test/blc/v2/status",
    ]);

    signals.emit("SIGINT");
    await running;
    await expect(db.raw("select 1")).rejects.toThrow();
  });
});
