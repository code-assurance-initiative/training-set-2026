import { randomBytes } from "node:crypto";
import { once } from "node:events";
import { createServer, type Server } from "node:http";
import type { AddressInfo } from "node:net";
import type { Knex } from "knex";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { parseWebhookEvent, verifyWebhookSignature } from "@parcel-tracking/webhooks";
import { ParcelStore } from "../../src/parcels/parcel-store.js";
import { DeliveryLog, ensureDeliveryLog } from "../../src/worker/delivery-log.js";
import { WebhookDispatcher } from "../../src/worker/webhook-dispatcher.js";
import { WebhookOutbox } from "../../src/worker/webhook-outbox.js";
import { createTestDatabase, registerWebhook } from "../support/database.js";

interface Received {
  signature: string;
  body: string;
}

/** A merchant endpoint on the loopback interface that answers with the next scripted status (200 by default). */
async function startMerchant(
  statuses: number[] = [],
): Promise<{ server: Server; url: string; received: Received[] }> {
  const received: Received[] = [];
  const server = createServer((req, res) => {
    const chunks: Buffer[] = [];
    req.on("data", (chunk: Buffer) => chunks.push(chunk));
    req.on("end", () => {
      received.push({
        signature: String(req.headers["parcel-signature"]),
        body: Buffer.concat(chunks).toString("utf8"),
      });
      res.writeHead(statuses.shift() ?? 200).end();
    });
  });
  server.listen(0, "127.0.0.1");
  await once(server, "listening");
  const { port } = server.address() as AddressInfo;
  return { server, url: `http://127.0.0.1:${port}/hooks/parcels`, received };
}

describe("webhook dispatch", () => {
  const secret = randomBytes(32).toString("hex");
  let db: Knex;
  let merchant: Awaited<ReturnType<typeof startMerchant>>;
  let stop: AbortController;

  const dispatcherWith = (clock: () => Date) =>
    new WebhookDispatcher(new WebhookOutbox(db), new DeliveryLog(db), {
      signingSecret: secret,
      batchSize: 10,
      clock,
    });

  const queueStatusChange = async () => {
    const store = new ParcelStore(db);
    const parcel = await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "NPX12345678",
        carrier: "NPX",
        destinationCountry: "DK",
      },
      new Date(),
    );
    await store.recordEvents(
      parcel,
      [{ carrierCode: "DL", status: "delivered", occurredAt: new Date("2026-10-02T12:00:00Z") }],
      new Date(),
    );
  };

  beforeEach(async () => {
    db = await createTestDatabase();
    await ensureDeliveryLog(db);
    stop = new AbortController();
  });

  afterEach(() => {
    stop.abort();
    merchant.server.close();
  });

  it("delivers a signed status change the merchant can verify with the published package", async () => {
    merchant = await startMerchant();
    await registerWebhook(db, "merchant-a", merchant.url);
    await queueStatusChange();

    const running = dispatcherWith(() => new Date()).run(stop.signal, 10);
    await vi.waitFor(
      () => {
        expect(merchant.received).toHaveLength(1);
      },
      { timeout: 5_000, interval: 10 },
    );
    stop.abort();
    await running;

    const [delivery] = merchant.received;
    expect(
      verifyWebhookSignature({
        secret,
        header: delivery?.signature ?? "",
        body: delivery?.body ?? "",
      }),
    ).toEqual({
      valid: true,
    });
    expect(parseWebhookEvent(delivery?.body ?? "")).toEqual({
      type: "parcel.status_changed",
      trackingNumber: "NPX12345678",
      status: "delivered",
      occurredAt: "2026-10-02T12:00:00.000Z",
    });
    await vi.waitFor(async () => {
      expect(await db("webhook_outbox").whereNotNull("delivered_at").count({ n: "*" })).toEqual([
        { n: 1 },
      ]);
    });
  });

  it("schedules a retry and logs the attempt when the merchant rejects a delivery", async () => {
    merchant = await startMerchant([500]);
    await registerWebhook(db, "merchant-a", merchant.url);
    await queueStatusChange();
    const now = new Date("2030-01-01T00:00:00Z");

    expect(await dispatcherWith(() => now).dispatchDue(stop.signal)).toBe(0);

    const [row] = await db("webhook_outbox").select<
      { id: number; attempts: number; next_attempt_at: Date }[]
    >("id", "attempts", "next_attempt_at");
    expect(row?.attempts).toBe(1);
    expect(row?.next_attempt_at).toEqual(new Date("2030-01-01T00:01:00Z"));
    expect(await new DeliveryLog(db).attempts(Number(row?.id))).toEqual([
      { outboxId: Number(row?.id), statusCode: 500, error: null, attemptedAt: now },
    ]);
    expect(await dispatcherWith(() => now).dispatchDue(stop.signal)).toBe(0);
    expect(merchant.received).toHaveLength(1);
  });

  it("records a merchant that cannot be reached as a failed attempt", async () => {
    merchant = await startMerchant();
    merchant.server.close();
    await registerWebhook(db, "merchant-a", merchant.url);
    await queueStatusChange();

    expect(
      await dispatcherWith(() => new Date("2030-01-01T00:00:00Z")).dispatchDue(stop.signal),
    ).toBe(0);

    const attempts = await db("webhook_delivery_attempts").select<
      { status_code: number | null; error: string }[]
    >("status_code", "error");
    expect(attempts).toHaveLength(1);
    expect(attempts[0]?.status_code).toBeNull();
    expect(attempts[0]?.error).toBeTruthy();
  });
});
