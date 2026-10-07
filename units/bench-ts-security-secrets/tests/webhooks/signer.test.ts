import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { WebhookSigner } from "../../src/webhooks/signer.js";

const testKeyPath = fileURLToPath(new URL("../fixtures/webhook-test-key.pem", import.meta.url));

describe("WebhookSigner", () => {
  it("produces a delivery its public key verifies", async () => {
    const signer = await WebhookSigner.fromPemFile(testKeyPath);
    const delivery = signer.sign({ type: "media.uploaded", data: { id: "m-1" } }, new Date("2026-03-01T12:00:00Z"));

    expect(delivery.headers["Webhook-Timestamp"]).toBe("1772366400");
    expect(WebhookSigner.verify(signer.publicKeyPem(), delivery)).toBe(true);
  });

  it("detects a tampered body", async () => {
    const signer = await WebhookSigner.fromPemFile(testKeyPath);
    const delivery = signer.sign({ type: "media.uploaded", data: { id: "m-1" } });

    expect(WebhookSigner.verify(signer.publicKeyPem(), { ...delivery, body: delivery.body.replace("m-1", "m-2") })).toBe(
      false,
    );
  });
});
