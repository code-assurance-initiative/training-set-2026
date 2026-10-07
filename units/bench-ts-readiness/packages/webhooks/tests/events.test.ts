import { describe, expect, it } from "vitest";
import { parseWebhookEvent, WebhookPayloadError } from "../src/index.js";

describe("webhook event parsing", () => {
  it("parses a status change", () => {
    const event = parseWebhookEvent(
      '{"type":"parcel.status_changed","trackingNumber":"NPX12345678","status":"delivered","occurredAt":"2026-10-02T12:00:00.000Z"}',
    );

    expect(event).toEqual({
      type: "parcel.status_changed",
      trackingNumber: "NPX12345678",
      status: "delivered",
      occurredAt: "2026-10-02T12:00:00.000Z",
    });
  });

  it.each([
    ["not JSON", "{"],
    ["not an object", "42"],
    ["an unknown type", '{"type":"parcel.deleted"}'],
    [
      "an unknown status",
      '{"type":"parcel.status_changed","trackingNumber":"X","status":"lost","occurredAt":"2026-10-02T12:00:00Z"}',
    ],
    [
      "a bad timestamp",
      '{"type":"parcel.status_changed","trackingNumber":"X","status":"delivered","occurredAt":"soon"}',
    ],
  ])("rejects %s", (_name, body) => {
    expect(() => parseWebhookEvent(body)).toThrow(WebhookPayloadError);
  });
});
