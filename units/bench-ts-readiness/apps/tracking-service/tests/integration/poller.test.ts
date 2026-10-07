import type { Knex } from "knex";
import { beforeEach, describe, expect, it } from "vitest";
import { ParcelStore } from "../../src/parcels/parcel-store.js";
import { TrackingService } from "../../src/parcels/tracking-service.js";
import { CarrierClient } from "../../src/worker/carrier-client.js";
import { CarrierPoller } from "../../src/worker/poller.js";
import { sleep } from "../../src/worker/retry.js";
import { silentLogger } from "../support/app.js";
import { createTestDatabase } from "../support/database.js";
import { FakeCarrier } from "../support/fake-carrier.js";

describe("carrier poller", () => {
  let db: Knex;
  let store: ParcelStore;
  let carrier: FakeCarrier;
  let poller: (intervalMs: number) => CarrierPoller;

  beforeEach(async () => {
    db = await createTestDatabase();
    store = new ParcelStore(db);
    carrier = new FakeCarrier();
    const client = new CarrierClient({
      carrierCode: "NPX",
      baseUrl: "https://carrier.test/npx/",
      apiKey: "carrier-test-key",
      timeoutMs: 1_000,
      retry: { attempts: 1, baseDelayMs: 0, maxDelayMs: 0 },
      fetch: carrier.fetch,
    });
    poller = (intervalMs) =>
      new CarrierPoller(store, new TrackingService(store), [client], silentLogger, {
        intervalMs,
        batchSize: 10,
      });
    await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "NPX12345678",
        carrier: "NPX",
        destinationCountry: "DK",
      },
      new Date("2026-10-01T08:00:00Z"),
    );
  });

  it("keeps polling at its interval until it is stopped", async () => {
    const stop = new AbortController();

    const running = poller(20).run(stop.signal);
    await sleep(300);
    stop.abort();
    await running;

    expect(carrier.requests.length).toBeGreaterThanOrEqual(5);
  });

  it("records what the carrier reports and moves the parcel on", async () => {
    carrier.report("NPX12345678", { code: "OD", occurredAt: new Date("2026-10-02T07:30:00Z") });

    const recorded = await poller(60_000).pollOnce(new AbortController().signal);

    expect(recorded).toBe(1);
    expect((await store.findByTrackingNumber("NPX12345678"))?.status).toBe("out_for_delivery");
  });

  it("carries on with the next parcel when the carrier answers with an error", async () => {
    await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "NPX87654321",
        carrier: "NPX",
        destinationCountry: "DK",
      },
      new Date("2026-10-01T09:00:00Z"),
    );
    carrier.failWith(500);
    carrier.report("NPX87654321", { code: "AR", occurredAt: new Date("2026-10-02T06:00:00Z") });

    expect(await poller(60_000).pollOnce(new AbortController().signal)).toBe(1);
    expect(carrier.requests).toHaveLength(2);
  });

  it("skips parcels of a carrier it has no client for", async () => {
    await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "XYZ12345678",
        carrier: "XYZ",
        destinationCountry: "DK",
      },
      new Date("2026-10-01T07:00:00Z"),
    );

    await poller(60_000).pollOnce(new AbortController().signal);

    expect(carrier.requests.map((r) => r.url)).toEqual([
      "https://carrier.test/npx/v2/shipments/NPX12345678/events",
    ]);
  });

  it("stops between parcels once the signal aborts", async () => {
    const stop = new AbortController();
    stop.abort();

    expect(await poller(60_000).pollOnce(stop.signal)).toBe(0);
    expect(carrier.requests).toHaveLength(0);
  });
});
