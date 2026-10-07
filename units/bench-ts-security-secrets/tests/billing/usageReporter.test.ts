import { describe, expect, it, vi } from "vitest";

import { UsageReporter } from "../../src/billing/usageReporter.js";
import { bodyText } from "../support/requestBody.js";

describe("UsageReporter", () => {
  it("posts a meter event rounded up to whole gigabyte-hours", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ object: "billing.meter_event" }));
    const reporter = new UsageReporter({ stripeRestrictedKey: "key-from-env" }, fetchStub);

    await reporter.report({ stripeCustomerId: "cus_Qx81", gigabyteHours: 12.2, at: new Date("2026-03-01T23:59:59Z") });

    const [url, init] = fetchStub.mock.calls[0] ?? [];
    expect(url).toBe("https://api.stripe.com/v1/billing/meter_events");
    expect(new Headers(init?.headers).get("authorization")).toBe("Bearer key-from-env");
    const body = new URLSearchParams(bodyText(init));
    expect(body.get("event_name")).toBe("storage_gb_hours");
    expect(body.get("payload[value]")).toBe("13");
    expect(body.get("payload[stripe_customer_id]")).toBe("cus_Qx81");
  });

  it("throws when billing rejects the event", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(new Response("{}", { status: 400 }));
    const reporter = new UsageReporter({ stripeRestrictedKey: "key-from-env" }, fetchStub);

    await expect(reporter.report({ stripeCustomerId: "cus_1", gigabyteHours: 1, at: new Date() })).rejects.toThrow(
      "HTTP 400",
    );
  });
});
