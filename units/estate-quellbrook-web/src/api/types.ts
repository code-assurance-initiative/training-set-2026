export interface OrderSummary {
  readonly orderId: string;
  readonly customerAccountId: string;
  readonly serviceLevel: string;
  readonly status: string;
  readonly consigneeName: string;
  readonly destinationCity: string;
  readonly parcelCount: number;
  readonly placedAt: string;
}

export interface OrderPage {
  readonly items: readonly OrderSummary[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}

export interface Order {
  readonly id: string;
  readonly customerAccountId: string;
  readonly serviceLevel: string;
  readonly status: string;
  readonly consignee: {
    readonly name: string;
    readonly line1: string;
    readonly line2?: string | null;
    readonly postalCode: string;
    readonly city: string;
    readonly countryCode: string;
  };
  readonly parcels: readonly { readonly number: number; readonly weightGrams: number }[];
  readonly totalWeightGrams: number;
  readonly placedBy: string;
  readonly placedAt: string;
}

export interface Delivery {
  readonly consignmentId: string;
  readonly status: string;
  readonly outForDeliveryAt?: string | null;
  readonly deliveredAt?: string | null;
  readonly proof?: string | null;
}

export interface Shipment {
  readonly order: Order;
  readonly delivery: Delivery | null;
}

export interface RouteStop {
  readonly sequence: number;
  readonly consignmentId: string;
  readonly orderId: string;
  readonly postalCode: string;
  readonly weightGrams: number;
  readonly status: string;
}

export interface BoardRoute {
  readonly routeId: string;
  readonly depot: string;
  readonly zone: string;
  readonly express: boolean;
  readonly status: string;
  readonly driverName: string;
  readonly vehicleRegistration: string;
  readonly vehicleKind: string;
  readonly capacityGrams: number;
  readonly loadGrams: number;
  readonly stops: readonly RouteStop[];
}
