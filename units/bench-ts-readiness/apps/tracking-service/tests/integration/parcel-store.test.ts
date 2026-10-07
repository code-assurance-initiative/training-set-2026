import type { Knex } from "knex";
import { beforeEach, describe, expect, it } from "vitest";
import { ParcelStore } from "../../src/parcels/parcel-store.js";
import type { Parcel } from "../../src/parcels/parcel.js";
import { createTestDatabase } from "../support/database.js";

const t0 = new Date("2026-10-01T08:00:00Z");
const at = (minutes: number) => new Date(t0.getTime() + minutes * 60_000);

describe("parcel store", () => {
  let db: Knex;
  let store: ParcelStore;
  let parcel: Parcel;

  beforeEach(async () => {
    db = await createTestDatabase();
    store = new ParcelStore(db);
    parcel = await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "NPX12345678",
        carrier: "NPX",
        destinationCountry: "DK",
      },
      t0,
    );
  });

  it("records new events, moves the parcel to the latest status and queues one webhook", async () => {
    const recorded = await store.recordEvents(
      parcel,
      [
        { carrierCode: "OD", status: "out_for_delivery", occurredAt: at(30) },
        { carrierCode: "AR", status: "in_transit", occurredAt: at(10) },
      ],
      at(31),
    );

    expect(recorded).toBe(2);
    expect((await store.findByTrackingNumber("NPX12345678"))?.status).toBe("out_for_delivery");
    expect((await store.events(parcel.id)).map((e) => e.carrierCode)).toEqual(["AR", "OD"]);
    const outbox = await db("webhook_outbox").select("merchant_id", "event_type");
    expect(outbox).toEqual([{ merchant_id: "merchant-a", event_type: "parcel.status_changed" }]);
  });

  it("ignores events it already has", async () => {
    const event = { carrierCode: "AR", status: "in_transit" as const, occurredAt: at(10) };
    await store.recordEvents(parcel, [event], at(11));

    expect(await store.recordEvents(parcel, [event], at(20))).toBe(0);
    expect(await db("webhook_outbox").count({ n: "*" })).toEqual([{ n: 1 }]);
  });

  it("lists only parcels that are not final, least recently updated first", async () => {
    const second = await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "BLC12345678",
        carrier: "BLC",
        destinationCountry: "SE",
      },
      at(-5),
    );
    const delivered = await store.create(
      {
        merchantId: "merchant-a",
        trackingNumber: "NPX87654321",
        carrier: "NPX",
        destinationCountry: "DK",
      },
      at(-10),
    );
    await store.recordEvents(
      delivered,
      [{ carrierCode: "DL", status: "delivered", occurredAt: at(1) }],
      at(2),
    );

    const active = await store.listActive(10);

    expect(active.map((p) => p.id)).toEqual([second.id, parcel.id]);
  });

  it("finds a parcel only for the merchant that owns it", async () => {
    expect(await store.findForMerchant("merchant-a", "NPX12345678")).toMatchObject({
      id: parcel.id,
    });
    expect(await store.findForMerchant("merchant-b", "NPX12345678")).toBeUndefined();
  });

  it("counts a merchant's parcels by status since a date", async () => {
    const stats = await store.deliveryStats("merchant-a", at(-60));

    expect(stats).toEqual({
      created: 1,
      in_transit: 0,
      out_for_delivery: 0,
      delivered: 0,
      exception: 0,
      returned: 0,
    });
    expect((await store.deliveryStats("merchant-a", at(60))).created).toBe(0);
  });
});
