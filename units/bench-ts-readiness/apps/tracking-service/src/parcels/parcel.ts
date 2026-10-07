export const parcelStatuses = [
  "created",
  "in_transit",
  "out_for_delivery",
  "delivered",
  "exception",
  "returned",
] as const;

export type ParcelStatus = (typeof parcelStatuses)[number];

/** Statuses after which carriers stop reporting and the worker stops polling. */
export const finalStatuses: readonly ParcelStatus[] = ["delivered", "returned"];

export interface Parcel {
  id: string;
  merchantId: string;
  trackingNumber: string;
  carrier: string;
  destinationCountry: string;
  status: ParcelStatus;
  pickupPointId: string | null;
  holdUntil: Date | null;
  createdAt: Date;
  updatedAt: Date;
}

export interface TrackingEvent {
  carrierCode: string;
  status: ParcelStatus;
  occurredAt: Date;
}

export interface NewParcel {
  merchantId: string;
  trackingNumber: string;
  carrier: string;
  destinationCountry: string;
}

export interface Redirect {
  pickupPointId: string;
  holdUntil: Date;
}

export type DeliveryStats = Record<ParcelStatus, number>;
