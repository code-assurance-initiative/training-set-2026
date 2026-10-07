import { randomUUID } from "node:crypto";
import type { Knex } from "knex";
import {
  finalStatuses,
  parcelStatuses,
  type DeliveryStats,
  type NewParcel,
  type Parcel,
  type ParcelStatus,
  type Redirect,
  type TrackingEvent,
} from "./parcel.js";

interface ParcelRow {
  id: string;
  merchant_id: string;
  tracking_number: string;
  carrier: string;
  destination_country: string;
  status: ParcelStatus;
  pickup_point_id: string | null;
  hold_until: Date | null;
  created_at: Date;
  updated_at: Date;
}

interface EventRow {
  parcel_id: string;
  carrier_code: string;
  status: ParcelStatus;
  occurred_at: Date;
}

const toParcel = (row: ParcelRow): Parcel => ({
  id: row.id,
  merchantId: row.merchant_id,
  trackingNumber: row.tracking_number,
  carrier: row.carrier,
  destinationCountry: row.destination_country,
  status: row.status,
  pickupPointId: row.pickup_point_id,
  holdUntil: row.hold_until,
  createdAt: row.created_at,
  updatedAt: row.updated_at,
});

export class DuplicateTrackingNumberError extends Error {
  constructor(trackingNumber: string) {
    super(`Tracking number ${trackingNumber} is already registered`);
    this.name = "DuplicateTrackingNumberError";
  }
}

export class ParcelStore {
  constructor(private readonly db: Knex) {}

  async create(parcel: NewParcel, now: Date): Promise<Parcel> {
    const existing = await this.db<ParcelRow>("parcels")
      .where({ tracking_number: parcel.trackingNumber })
      .first("id");
    if (existing) {
      throw new DuplicateTrackingNumberError(parcel.trackingNumber);
    }
    const row: ParcelRow = {
      id: randomUUID(),
      merchant_id: parcel.merchantId,
      tracking_number: parcel.trackingNumber,
      carrier: parcel.carrier,
      destination_country: parcel.destinationCountry,
      status: "created",
      pickup_point_id: null,
      hold_until: null,
      created_at: now,
      updated_at: now,
    };
    await this.db<ParcelRow>("parcels").insert(row);
    return toParcel(row);
  }

  async findForMerchant(merchantId: string, trackingNumber: string): Promise<Parcel | undefined> {
    const row = await this.db<ParcelRow>("parcels")
      .where({ merchant_id: merchantId, tracking_number: trackingNumber })
      .first();
    return row ? toParcel(row) : undefined;
  }

  async findByTrackingNumber(trackingNumber: string): Promise<Parcel | undefined> {
    const row = await this.db<ParcelRow>("parcels")
      .where({ tracking_number: trackingNumber })
      .first();
    return row ? toParcel(row) : undefined;
  }

  async events(parcelId: string): Promise<TrackingEvent[]> {
    const rows = await this.db<EventRow>("tracking_events")
      .where({ parcel_id: parcelId })
      .orderBy("occurred_at", "asc")
      .select("carrier_code", "status", "occurred_at");
    return rows.map((row) => ({
      carrierCode: row.carrier_code,
      status: row.status,
      occurredAt: row.occurred_at,
    }));
  }

  /** Parcels the worker still has to poll, least recently updated first. */
  async listActive(limit: number): Promise<Parcel[]> {
    const rows = await this.db<ParcelRow>("parcels")
      .whereIn(
        "status",
        parcelStatuses.filter((s) => !finalStatuses.includes(s)),
      )
      .orderBy("updated_at", "asc")
      .limit(limit);
    return rows.map(toParcel);
  }

  /**
   * Records carrier events the parcel does not have yet, moves the parcel to the status of the latest one and
   * queues a webhook for its merchant, in one transaction. Returns how many events were new.
   */
  async recordEvents(parcel: Parcel, events: readonly TrackingEvent[], now: Date): Promise<number> {
    return this.db.transaction(async (trx) => {
      const known = await trx<EventRow>("tracking_events")
        .where({ parcel_id: parcel.id })
        .select("carrier_code", "occurred_at");
      const seen = new Set(known.map((e) => `${e.carrier_code}@${e.occurred_at.toISOString()}`));
      const fresh = events.filter(
        (e) => !seen.has(`${e.carrierCode}@${e.occurredAt.toISOString()}`),
      );
      if (fresh.length === 0) {
        return 0;
      }

      await trx("tracking_events").insert(
        fresh.map((e) => ({
          parcel_id: parcel.id,
          carrier_code: e.carrierCode,
          status: e.status,
          occurred_at: e.occurredAt,
          recorded_at: now,
        })),
      );
      const latest = fresh.reduce((a, b) => (b.occurredAt > a.occurredAt ? b : a));
      await trx<ParcelRow>("parcels")
        .where({ id: parcel.id })
        .update({ status: latest.status, updated_at: now });
      await trx("webhook_outbox").insert({
        merchant_id: parcel.merchantId,
        parcel_id: parcel.id,
        event_type: "parcel.status_changed",
        payload: JSON.stringify({
          type: "parcel.status_changed",
          trackingNumber: parcel.trackingNumber,
          status: latest.status,
          occurredAt: latest.occurredAt.toISOString(),
        }),
        created_at: now,
        next_attempt_at: now,
      });
      return fresh.length;
    });
  }

  async touch(parcelId: string, now: Date): Promise<void> {
    await this.db<ParcelRow>("parcels").where({ id: parcelId }).update({ updated_at: now });
  }

  async redirect(parcelId: string, redirect: Redirect, now: Date): Promise<void> {
    await this.db<ParcelRow>("parcels").where({ id: parcelId }).update({
      pickup_point_id: redirect.pickupPointId,
      hold_until: redirect.holdUntil,
      updated_at: now,
    });
  }

  async deliveryStats(merchantId: string, since: Date): Promise<DeliveryStats> {
    const rows = await this.db<ParcelRow>("parcels")
      .where({ merchant_id: merchantId })
      .andWhere("created_at", ">=", since)
      .groupBy("status")
      .select("status")
      .count<{ status: ParcelStatus; count: string | number }[]>({ count: "*" });
    const stats = Object.fromEntries(parcelStatuses.map((s) => [s, 0])) as DeliveryStats;
    for (const row of rows) {
      stats[row.status] = Number(row.count);
    }
    return stats;
  }
}
