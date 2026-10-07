import request from "supertest";
import { beforeEach, describe, expect, it } from "vitest";
import { createTestApi, type TestApi } from "../support/app.js";

describe("merchant delivery statistics", () => {
  let api: TestApi;

  beforeEach(async () => {
    api = await createTestApi();
    for (const trackingNumber of ["NPX00000001", "NPX00000002", "BLC00000003"]) {
      await api.service.register({
        merchantId: "merchant-a",
        trackingNumber,
        carrier: trackingNumber.slice(0, 3),
        destinationCountry: "SE",
      });
    }
    await api.service.register({
      merchantId: "merchant-b",
      trackingNumber: "NPX00000009",
      carrier: "NPX",
      destinationCountry: "NO",
    });
  });

  it("counts the merchant's parcels by status over the requested window", async () => {
    const token = await api.tokens.token("merchant-a", "parcels:read");

    const response = await request(api.app)
      .get("/v1/merchants/merchant-a/delivery-stats?days=7")
      .set("authorization", `Bearer ${token}`);

    expect(response.status).toBe(200);
    expect(response.body).toEqual({
      merchantId: "merchant-a",
      days: 7,
      stats: {
        created: 3,
        in_transit: 0,
        out_for_delivery: 0,
        delivered: 0,
        exception: 0,
        returned: 0,
      },
    });
  });

  it("rejects a window longer than ninety days", async () => {
    const token = await api.tokens.token("merchant-a", "parcels:read");

    const response = await request(api.app)
      .get("/v1/merchants/merchant-a/delivery-stats?days=365")
      .set("authorization", `Bearer ${token}`);

    expect(response.status).toBe(400);
  });
});
