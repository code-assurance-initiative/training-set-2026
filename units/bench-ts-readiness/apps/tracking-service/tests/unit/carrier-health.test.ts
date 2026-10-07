import { describe, expect, it } from "vitest";
import { checkCarriers } from "../../src/health/carrier-health.js";
import { CarrierClient } from "../../src/worker/carrier-client.js";
import { FakeCarrier } from "../support/fake-carrier.js";

describe("carrier readiness", () => {
  it("reports each carrier by code after pinging it", async () => {
    const carrier = new FakeCarrier();
    const clients = ["NPX", "BLC"].map(
      (carrierCode) =>
        new CarrierClient({
          carrierCode,
          baseUrl: `https://carrier.test/${carrierCode.toLowerCase()}/`,
          apiKey: "carrier-test-key",
          timeoutMs: 1_000,
          retry: { attempts: 1, baseDelayMs: 0, maxDelayMs: 0 },
          fetch: carrier.fetch,
        }),
    );

    const health = await checkCarriers(clients);

    expect(health).toEqual([
      { carrier: "NPX", reachable: true },
      { carrier: "BLC", reachable: true },
    ]);
    expect(carrier.requests).toHaveLength(2);
  });
});
