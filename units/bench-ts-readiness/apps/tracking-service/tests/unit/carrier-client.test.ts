import { describe, expect, it } from "vitest";
import { CarrierClient, CarrierHttpError } from "../../src/worker/carrier-client.js";
import { FakeCarrier } from "../support/fake-carrier.js";

const clientFor = (carrier: FakeCarrier) =>
  new CarrierClient({
    carrierCode: "NPX",
    baseUrl: "https://carrier.test/npx/",
    apiKey: "carrier-test-key",
    timeoutMs: 1_000,
    retry: { attempts: 3, baseDelayMs: 0, maxDelayMs: 0 },
    fetch: carrier.fetch,
  });

describe("carrier client", () => {
  it("reads the shipment's events with the API key", async () => {
    const carrier = new FakeCarrier();
    carrier.report("NPX12345678", { code: "AR", occurredAt: new Date("2026-10-01T08:00:00Z") });

    const events = await clientFor(carrier).fetchEvents("NPX12345678");

    expect(events).toEqual([{ code: "AR", occurredAt: new Date("2026-10-01T08:00:00Z") }]);
    expect(carrier.requests[0]?.url).toBe(
      "https://carrier.test/npx/v2/shipments/NPX12345678/events",
    );
    expect(carrier.requests[0]?.headers.get("x-api-key")).toBe("carrier-test-key");
  });

  it("retries a 503 and returns the next answer", async () => {
    const carrier = new FakeCarrier();
    carrier.failWith(503);

    await expect(clientFor(carrier).fetchEvents("NPX12345678")).resolves.toEqual([]);
    expect(carrier.requests).toHaveLength(2);
  });

  it("gives up after three attempts", async () => {
    const carrier = new FakeCarrier();
    carrier.failWith(503, 502, 429);

    await expect(clientFor(carrier).fetchEvents("NPX12345678")).rejects.toBeInstanceOf(
      CarrierHttpError,
    );
    expect(carrier.requests).toHaveLength(3);
  });

  it("does not retry a client error", async () => {
    const carrier = new FakeCarrier();
    carrier.failWith(404);

    await expect(clientFor(carrier).fetchEvents("NPX12345678")).rejects.toMatchObject({
      status: 404,
      carrierCode: "NPX",
      transient: false,
    });
    expect(carrier.requests).toHaveLength(1);
  });

  it("passes the caller's abort to the request and does not retry it", async () => {
    const carrier = new FakeCarrier();

    await expect(
      clientFor(carrier).fetchEvents("NPX12345678", AbortSignal.abort(new Error("stopping"))),
    ).rejects.toThrow("stopping");
    expect(carrier.requests).toHaveLength(1);
    expect(carrier.requests[0]?.aborted).toBe(true);
  });

  it("pings the status endpoint once, without retrying", async () => {
    const carrier = new FakeCarrier();
    carrier.failWith(503);

    await expect(clientFor(carrier).ping()).rejects.toBeInstanceOf(CarrierHttpError);
    await expect(clientFor(carrier).ping()).resolves.toBeUndefined();
    expect(carrier.requests.map((r) => r.url)).toEqual([
      "https://carrier.test/npx/v2/status",
      "https://carrier.test/npx/v2/status",
    ]);
  });
});
