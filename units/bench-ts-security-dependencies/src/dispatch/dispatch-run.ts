/** A delivery address as the shipper entered it. */
export interface Address {
  readonly street: string;
  readonly postcode: string;
  readonly city: string;
}

export interface Parcel {
  readonly trackingNumber: string;
  readonly recipient: string;
  readonly address: Address;
  readonly weightKg: number;
}

export interface Coordinates {
  readonly lat: number;
  readonly lng: number;
}

/** The two-hour window promised to the recipient, in depot-local time (`YYYY-MM-DD HH:mm`). */
export interface DeliveryWindow {
  readonly from: string;
  readonly to: string;
}

export type StopStatus = 'planned' | 'out-for-delivery' | 'delivered' | 'failed';

export interface Stop {
  readonly trackingNumber: string;
  readonly recipient: string;
  readonly street: string;
  readonly postcode: string;
  /** The service-area city the shipper's spelling was matched to. */
  readonly city: string;
  readonly weightKg: number;
  readonly location: Coordinates | null;
  readonly window: DeliveryWindow;
  readonly status: StopStatus;
}

export interface Route {
  readonly vehicle: number;
  readonly city: string;
  readonly trackingNumbers: readonly string[];
  readonly totalWeightKg: number;
}

export interface DispatchRun {
  readonly id: string;
  readonly depotId: string;
  readonly serviceDate: string;
  /** The latest moment parcels can be added before the first departure (ISO 8601). */
  readonly cutoff: string;
  readonly stops: readonly Stop[];
  readonly routes: readonly Route[];
  readonly createdAt: string;
}
