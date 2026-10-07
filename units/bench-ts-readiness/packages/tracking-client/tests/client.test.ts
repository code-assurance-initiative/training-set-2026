import { describe, expect, it } from "vitest";
import { TrackingApiError, TrackingClient } from "../src/index.js";

interface Call {
  url: string;
  init: RequestInit | undefined;
}

function fakeFetch(...responses: Response[]): { fetch: typeof fetch; calls: Call[] } {
  const calls: Call[] = [];
  return {
    calls,
    fetch: (input, init) => {
      calls.push({ url: input instanceof Request ? input.url : String(input), init });
      return Promise.resolve(responses.shift() ?? new Response(null, { status: 500 }));
    },
  };
}

const parcel = {
  trackingNumber: "NPX12345678",
  carrier: "NPX",
  destinationCountry: "DK",
  status: "in_transit",
  pickupPointId: null,
  holdUntil: null,
  createdAt: "2026-10-01T08:00:00.000Z",
  events: [],
};

describe("tracking client", () => {
  it("sends a fresh bearer token with every request", async () => {
    let issued = 0;
    const { fetch, calls } = fakeFetch(Response.json(parcel), Response.json(parcel));
    const client = new TrackingClient({
      baseUrl: "https://tracking.example.com/",
      token: () => `token-${++issued}`,
      fetch,
    });

    await client.getParcel("NPX12345678");
    await client.getParcel("NPX12345678");

    const auth = calls.map((c) => new Headers(c.init?.headers).get("authorization"));
    expect(auth).toEqual(["Bearer token-1", "Bearer token-2"]);
    expect(calls[0]?.url).toBe("https://tracking.example.com/v1/parcels/NPX12345678");
  });

  it("posts JSON bodies and returns the created parcel", async () => {
    const { fetch, calls } = fakeFetch(Response.json(parcel, { status: 201 }));
    const client = new TrackingClient({
      baseUrl: "https://tracking.example.com/",
      token: () => "t",
      fetch,
    });

    const created = await client.createParcel({
      trackingNumber: "NPX12345678",
      carrier: "NPX",
      destinationCountry: "DK",
    });

    expect(created.trackingNumber).toBe("NPX12345678");
    expect(calls[0]?.init?.method).toBe("POST");
    expect(new Headers(calls[0]?.init?.headers).get("content-type")).toBe("application/json");
    const body = calls[0]?.init?.body;
    expect(JSON.parse(typeof body === "string" ? body : "null")).toMatchObject({ carrier: "NPX" });
  });

  it("passes the caller's signal and does not retry", async () => {
    const { fetch, calls } = fakeFetch(
      new Response(null, { status: 503, statusText: "Service Unavailable" }),
    );
    const client = new TrackingClient({
      baseUrl: "https://tracking.example.com/",
      token: () => "t",
      fetch,
    });
    const signal = AbortSignal.timeout(5_000);

    await expect(client.getParcel("NPX12345678", { signal })).rejects.toMatchObject({
      status: 503,
      title: "Service Unavailable",
    });
    expect(calls).toHaveLength(1);
    expect(calls[0]?.init?.signal).toBe(signal);
  });

  it("turns a problem answer into a TrackingApiError", async () => {
    const problem = {
      title: "Conflict",
      status: 409,
      detail: "A delivered parcel cannot be redirected",
    };
    const { fetch } = fakeFetch(Response.json(problem, { status: 409 }));
    const client = new TrackingClient({
      baseUrl: "https://tracking.example.com/",
      token: () => "t",
      fetch,
    });

    const error = await client
      .redirect("NPX12345678", { pickupPointId: "PP-1", holdUntil: "2026-10-20T16:00:00Z" })
      .catch((e: unknown) => e);

    expect(error).toBeInstanceOf(TrackingApiError);
    expect(error).toMatchObject({ status: 409, detail: "A delivered parcel cannot be redirected" });
  });

  it("returns nothing for a 204 and reads share links and statistics", async () => {
    const link = {
      url: "https://tracking.example.com/track/NPX12345678?expires=1&sig=x",
      expiresAt: 1,
    };
    const stats = { merchantId: "m", days: 7, stats: {} };
    const { fetch, calls } = fakeFetch(
      new Response(null, { status: 204 }),
      Response.json(link, { status: 201 }),
      Response.json(stats),
    );
    const client = new TrackingClient({
      baseUrl: "https://tracking.example.com/",
      token: () => "t",
      fetch,
    });

    await expect(
      client.redirect("NPX12345678", { pickupPointId: "PP-1", holdUntil: "2026-10-20T16:00:00Z" }),
    ).resolves.toBeUndefined();
    await expect(client.createShareLink("NPX12345678")).resolves.toEqual(link);
    await expect(client.deliveryStats("m", 7)).resolves.toEqual(stats);
    expect(calls[2]?.url).toBe("https://tracking.example.com/v1/merchants/m/delivery-stats?days=7");
  });
});
