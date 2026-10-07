export type ParcelStatus =
  "created" | "in_transit" | "out_for_delivery" | "delivered" | "exception" | "returned";

export interface ParcelStatusChanged {
  type: "parcel.status_changed";
  trackingNumber: string;
  status: ParcelStatus;
  occurredAt: string;
}

export type WebhookEvent = ParcelStatusChanged;

const statuses = new Set<string>([
  "created",
  "in_transit",
  "out_for_delivery",
  "delivered",
  "exception",
  "returned",
]);

export class WebhookPayloadError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "WebhookPayloadError";
  }
}

/** Parses a verified webhook body; throws WebhookPayloadError for anything this version does not know. */
export function parseWebhookEvent(body: string): WebhookEvent {
  let value: unknown;
  try {
    value = JSON.parse(body);
  } catch {
    throw new WebhookPayloadError("The body is not JSON");
  }
  if (typeof value !== "object" || value === null) {
    throw new WebhookPayloadError("The body is not an object");
  }
  const event = value as Record<string, unknown>;
  if (event.type !== "parcel.status_changed") {
    throw new WebhookPayloadError(`Unknown event type ${String(event.type)}`);
  }
  const { trackingNumber, status, occurredAt } = event;
  if (
    typeof trackingNumber !== "string" ||
    typeof status !== "string" ||
    !statuses.has(status) ||
    typeof occurredAt !== "string" ||
    Number.isNaN(Date.parse(occurredAt))
  ) {
    throw new WebhookPayloadError("Malformed parcel.status_changed event");
  }
  return {
    type: "parcel.status_changed",
    trackingNumber,
    status: status as ParcelStatus,
    occurredAt,
  };
}
