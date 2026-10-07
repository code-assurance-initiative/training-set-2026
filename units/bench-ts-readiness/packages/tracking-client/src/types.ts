export type ParcelStatus =
  "created" | "in_transit" | "out_for_delivery" | "delivered" | "exception" | "returned";

export interface TrackingEvent {
  code: string;
  status: ParcelStatus;
  occurredAt: string;
}

export interface Parcel {
  trackingNumber: string;
  carrier: string;
  destinationCountry: string;
  status: ParcelStatus;
  pickupPointId: string | null;
  holdUntil: string | null;
  createdAt: string;
  events: TrackingEvent[];
}

export interface CreateParcel {
  trackingNumber: string;
  carrier: string;
  destinationCountry: string;
}

export interface Redirect {
  pickupPointId: string;
  /** ISO 8601 instant until which the pickup point holds the parcel. */
  holdUntil: string;
}

export interface ShareLink {
  url: string;
  /** Unix seconds. */
  expiresAt: number;
}

export interface DeliveryStats {
  merchantId: string;
  days: number;
  stats: Record<ParcelStatus, number>;
}
