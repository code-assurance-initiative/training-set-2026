import assert from "node:assert/strict";
import { Given, Then, When } from "@cucumber/cucumber";
import type { TrackingWorld } from "../support/world.js";

Given(
  "merchant {string} has registered parcel {string} with carrier {string}",
  async function (
    this: TrackingWorld,
    merchantId: string,
    trackingNumber: string,
    carrier: string,
  ) {
    await this.service.register({ merchantId, trackingNumber, carrier, destinationCountry: "DK" });
  },
);

When(
  "the carrier reports {string} for parcel {string} at {string}",
  async function (this: TrackingWorld, code: string, trackingNumber: string, at: string) {
    const parcel = await this.store.findByTrackingNumber(trackingNumber);
    assert.ok(parcel, `parcel ${trackingNumber} is registered`);
    await this.service.applyCarrierEvents(parcel, [{ code, occurredAt: new Date(at) }]);
  },
);

When(
  "merchant {string} looks up parcel {string}",
  async function (this: TrackingWorld, merchantId: string, trackingNumber: string) {
    this.lookup = await this.service
      .get(merchantId, trackingNumber)
      .catch((error: unknown) => error as Error);
  },
);

Then(
  "parcel {string} has status {string}",
  async function (this: TrackingWorld, trackingNumber: string, status: string) {
    assert.equal((await this.store.findByTrackingNumber(trackingNumber))?.status, status);
  },
);

Then(
  "parcel {string} has {int} tracking event(s)",
  async function (this: TrackingWorld, trackingNumber: string, count: number) {
    const parcel = await this.store.findByTrackingNumber(trackingNumber);
    assert.ok(parcel);
    assert.equal((await this.store.events(parcel.id)).length, count);
  },
);

Then("the merchant sees status {string}", function (this: TrackingWorld, status: string) {
  assert.ok(this.lookup && !(this.lookup instanceof Error), "the lookup succeeded");
  assert.equal(this.lookup.status, status);
});

Then("the lookup fails as not found", function (this: TrackingWorld) {
  assert.ok(this.lookup instanceof Error);
  assert.equal(this.lookup.name, "ParcelNotFoundError");
});
