import assert from "node:assert/strict";
import { randomBytes } from "node:crypto";
import { Given, Then, When } from "@cucumber/cucumber";
import { verifyWebhookSignature } from "@parcel-tracking/webhooks";
import { DeliveryLog, ensureDeliveryLog } from "../../src/worker/delivery-log.js";
import { WebhookDispatcher } from "../../src/worker/webhook-dispatcher.js";
import { WebhookOutbox } from "../../src/worker/webhook-outbox.js";
import { registerWebhook } from "../../tests/support/database.js";
import type { TrackingWorld } from "../support/world.js";

interface WebhookWorld extends TrackingWorld {
  secret: string;
  received: { url: string; signature: string; body: string; status: number }[];
  answers: number[];
}

const dispatcher = (world: WebhookWorld) =>
  new WebhookDispatcher(new WebhookOutbox(world.db), new DeliveryLog(world.db), {
    signingSecret: world.secret,
    batchSize: 10,
    clock: () => world.now,
    fetch: (input, init) => {
      const status = world.answers.shift() ?? 200;
      world.received.push({
        url: input instanceof Request ? input.url : String(input),
        signature: new Headers(init?.headers).get("parcel-signature") ?? "",
        body: typeof init?.body === "string" ? init.body : "",
        status,
      });
      return Promise.resolve(new Response(null, { status }));
    },
  });

Given(
  "merchant {string} receives webhooks at {string}",
  async function (this: WebhookWorld, merchantId: string, url: string) {
    this.secret = randomBytes(32).toString("hex");
    this.received = [];
    this.answers = [];
    await ensureDeliveryLog(this.db);
    await registerWebhook(this.db, merchantId, url);
  },
);

Given("the merchant's endpoint answers {int} once", function (this: WebhookWorld, status: number) {
  this.answers.push(status);
});

When("the worker dispatches due webhooks", async function (this: WebhookWorld) {
  await dispatcher(this).dispatchDue(new AbortController().signal);
});

When("{int} minute(s) pass(es)", function (this: WebhookWorld, minutes: number) {
  this.now = new Date(this.now.getTime() + minutes * 60_000);
});

Then(
  "the merchant has received {int} webhook(s) for parcel {string} with status {string}",
  function (this: WebhookWorld, count: number, trackingNumber: string, status: string) {
    const accepted = this.received.filter((r) => r.status === 200);
    assert.equal(accepted.length, count);
    for (const delivery of accepted) {
      assert.deepEqual(
        { ...(JSON.parse(delivery.body) as object) },
        {
          type: "parcel.status_changed",
          trackingNumber,
          status,
          occurredAt: (JSON.parse(delivery.body) as { occurredAt: string }).occurredAt,
        },
      );
    }
  },
);

Then("every webhook carries a valid signature", function (this: WebhookWorld) {
  for (const delivery of this.received) {
    const result = verifyWebhookSignature({
      secret: this.secret,
      header: delivery.signature,
      body: delivery.body,
      now: Math.floor(this.now.getTime() / 1000),
    });
    assert.deepEqual(result, { valid: true });
  }
});
