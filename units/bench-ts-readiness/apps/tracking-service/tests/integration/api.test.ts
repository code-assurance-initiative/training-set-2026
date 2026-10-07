import request from "supertest";
import { beforeEach, describe, expect, it } from "vitest";
import { createTestApi, type TestApi } from "../support/app.js";

const parcel = { trackingNumber: "NPX12345678", carrier: "NPX", destinationCountry: "DK" };

describe("parcels API", () => {
  let api: TestApi;
  let writer: string;
  let reader: string;

  beforeEach(async () => {
    api = await createTestApi();
    writer = await api.tokens.token("merchant-a", "parcels:read parcels:write");
    reader = await api.tokens.token("merchant-a", "parcels:read");
  });

  const create = (token: string, body: object = parcel) =>
    request(api.app).post("/v1/parcels").set("authorization", `Bearer ${token}`).send(body);

  it("registers a parcel for the caller's merchant", async () => {
    const response = await create(writer);

    expect(response.status).toBe(201);
    expect(response.headers.location).toBe("/v1/parcels/NPX12345678");
    expect(response.body).toMatchObject({ ...parcel, status: "created", events: [] });
  });

  it("rejects a tracking number that is already registered", async () => {
    await create(writer);

    expect((await create(writer)).status).toBe(409);
  });

  it("rejects an invalid or unexpected body field", async () => {
    expect((await create(writer, { ...parcel, carrier: "npx" })).status).toBe(400);
    expect((await create(writer, { ...parcel, merchantId: "merchant-b" })).status).toBe(400);
  });

  it("requires the write scope to register and a token at all", async () => {
    expect((await create(reader)).status).toBe(403);
    const anonymous = await request(api.app).post("/v1/parcels").send(parcel);
    expect(anonymous.status).toBe(401);
    expect(anonymous.headers["www-authenticate"]).toContain("Bearer");
  });

  it("rejects an expired token and one from another issuer", async () => {
    const expired = await api.tokens.token("merchant-a", "parcels:write", { expiresIn: "-1m" });
    const foreign = await api.tokens.token("merchant-a", "parcels:write", {
      issuer: "https://elsewhere.test/",
    });

    expect((await create(expired)).status).toBe(401);
    expect((await create(foreign)).status).toBe(401);
  });

  it("returns the merchant's own parcel with its events, and nobody else's", async () => {
    await create(writer);
    const other = await api.tokens.token("merchant-b", "parcels:read");

    const own = await request(api.app)
      .get("/v1/parcels/NPX12345678")
      .set("authorization", `Bearer ${reader}`);
    const foreign = await request(api.app)
      .get("/v1/parcels/NPX12345678")
      .set("authorization", `Bearer ${other}`);

    expect(own.status).toBe(200);
    expect(own.body).toMatchObject({ trackingNumber: "NPX12345678", status: "created" });
    expect(foreign.status).toBe(404);
  });

  it("rejects a malformed tracking number", async () => {
    const response = await request(api.app)
      .get("/v1/parcels/not-a-number")
      .set("authorization", `Bearer ${reader}`);

    expect(response.status).toBe(400);
    expect(response.type).toBe("application/problem+json");
  });

  it("issues a share link that opens the public tracking page until it is tampered with", async () => {
    await create(writer);

    const link = await request(api.app)
      .post("/v1/parcels/NPX12345678/share-link")
      .set("authorization", `Bearer ${reader}`);
    expect(link.status).toBe(201);
    const url = new URL((link.body as { url: string }).url);

    const page = await request(api.app).get(url.pathname + url.search);
    expect(page.status).toBe(200);
    expect(page.body).toEqual({ trackingNumber: "NPX12345678", status: "created", events: [] });
    expect(page.headers["cache-control"]).toBe("no-store");

    url.searchParams.set("expires", String(Number(url.searchParams.get("expires")) + 3600));
    expect((await request(api.app).get(url.pathname + url.search)).status).toBe(404);
  });

  it("redirects an undelivered parcel to a pickup point", async () => {
    await create(writer);

    const response = await request(api.app)
      .post("/v1/parcels/NPX12345678/redirect")
      .set("authorization", `Bearer ${writer}`)
      .send({ pickupPointId: "PP-2200-014", holdUntil: "2026-10-20T16:00:00Z" });

    expect(response.status).toBe(204);
    const parcelNow = await request(api.app)
      .get("/v1/parcels/NPX12345678")
      .set("authorization", `Bearer ${reader}`);
    expect(parcelNow.body).toMatchObject({
      pickupPointId: "PP-2200-014",
      holdUntil: "2026-10-20T16:00:00.000Z",
    });
  });

  it("answers JSON problems for unknown routes and bodies that are not JSON", async () => {
    expect((await request(api.app).get("/nothing-here")).status).toBe(404);
    const broken = await request(api.app)
      .post("/v1/parcels")
      .set("authorization", `Bearer ${writer}`)
      .set("content-type", "application/json")
      .send("{not json");
    expect(broken.status).toBe(400);
  });
});

describe("platform endpoints", () => {
  let api: TestApi;

  beforeEach(async () => {
    api = await createTestApi();
  });

  it("serves liveness and readiness without a token", async () => {
    expect((await request(api.app).get("/healthz")).status).toBe(200);
    expect((await request(api.app).get("/readyz")).status).toBe(200);
    api.ready.value = false;
    expect((await request(api.app).get("/readyz")).status).toBe(503);
  });

  it("publishes security.txt", async () => {
    const response = await request(api.app).get("/.well-known/security.txt");

    expect(response.status).toBe(200);
    expect(response.text).toMatch(/^Contact: https:\/\//m);
  });

  it("sends the security headers", async () => {
    const response = await request(api.app).get("/healthz");

    expect(response.headers["strict-transport-security"]).toBeDefined();
    expect(response.headers["x-content-type-options"]).toBe("nosniff");
    expect(response.headers["x-powered-by"]).toBeUndefined();
  });
});
