import type { DeliveryStats, NewParcel, Parcel, Redirect, TrackingEvent } from "./parcel.js";
import type { ParcelStore } from "./parcel-store.js";
import { statusForCarrierCode } from "./status-mapping.js";

export type Clock = () => Date;

export const systemClock: Clock = () => new Date();

/** A carrier event as reported, before it is mapped to a status. */
export interface CarrierEvent {
  code: string;
  occurredAt: Date;
}

export interface ParcelWithEvents extends Parcel {
  events: TrackingEvent[];
}

export class ParcelNotFoundError extends Error {
  constructor(trackingNumber: string) {
    super(`No parcel with tracking number ${trackingNumber}`);
    this.name = "ParcelNotFoundError";
  }
}

export class RedirectNotAllowedError extends Error {
  constructor(reason: string) {
    super(reason);
    this.name = "RedirectNotAllowedError";
  }
}

export class TrackingService {
  constructor(
    private readonly store: ParcelStore,
    private readonly clock: Clock = systemClock,
  ) {}

  register(parcel: NewParcel): Promise<Parcel> {
    return this.store.create(parcel, this.clock());
  }

  async get(merchantId: string, trackingNumber: string): Promise<ParcelWithEvents> {
    const parcel = await this.require(merchantId, trackingNumber);
    return { ...parcel, events: await this.store.events(parcel.id) };
  }

  async publicStatus(trackingNumber: string): Promise<ParcelWithEvents | undefined> {
    const parcel = await this.store.findByTrackingNumber(trackingNumber);
    return parcel ? { ...parcel, events: await this.store.events(parcel.id) } : undefined;
  }

  /** Maps and records carrier events; unknown codes are skipped. Returns how many events were new. */
  async applyCarrierEvents(parcel: Parcel, reported: readonly CarrierEvent[]): Promise<number> {
    const events: TrackingEvent[] = [];
    for (const event of reported) {
      const status = statusForCarrierCode(event.code);
      if (status !== undefined) {
        events.push({ carrierCode: event.code, status, occurredAt: event.occurredAt });
      }
    }
    const now = this.clock();
    const recorded = await this.store.recordEvents(parcel, events, now);
    if (recorded === 0) {
      await this.store.touch(parcel.id, now);
    }
    return recorded;
  }

  async redirect(merchantId: string, trackingNumber: string, redirect: Redirect): Promise<void> {
    const parcel = await this.require(merchantId, trackingNumber);
    if (parcel.status === "delivered" || parcel.status === "returned") {
      throw new RedirectNotAllowedError(`A ${parcel.status} parcel cannot be redirected`);
    }
    await this.store.redirect(parcel.id, redirect, this.clock());
  }

  deliveryStats(merchantId: string, days: number): Promise<DeliveryStats> {
    const since = new Date(this.clock().getTime() - days * 86_400_000);
    return this.store.deliveryStats(merchantId, since);
  }

  private async require(merchantId: string, trackingNumber: string): Promise<Parcel> {
    const parcel = await this.store.findForMerchant(merchantId, trackingNumber);
    if (!parcel) {
      throw new ParcelNotFoundError(trackingNumber);
    }
    return parcel;
  }
}
