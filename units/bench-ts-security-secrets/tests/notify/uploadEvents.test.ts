import { fileURLToPath } from "node:url";

import { describe, expect, it, vi } from "vitest";

import type { MediaRecord } from "../../src/db/mediaRepository.js";
import { UploadEvents } from "../../src/notify/uploadEvents.js";
import { WebhookSigner } from "../../src/webhooks/signer.js";
import { silentLogger } from "../support/silentLogger.js";
import { bodyText } from "../support/requestBody.js";

const record: MediaRecord = {
  id: "0b6f5c1e-2f43-4a77-9b8e-5d0c7a1f2e39",
  ownerId: "studio-north",
  objectKey: "studio-north/2026-03-01/0b6f5c1e.png",
  contentType: "image/png",
  sizeBytes: 2048,
  createdAt: new Date("2026-03-01T12:00:00Z"),
};

async function signer(): Promise<WebhookSigner> {
  return WebhookSigner.fromPemFile(fileURLToPath(new URL("../fixtures/webhook-test-key.pem", import.meta.url)));
}

describe("UploadEvents", () => {
  it("pushes to the owner topic and delivers a verifiable webhook", async () => {
    const push = { send: vi.fn().mockResolvedValue(undefined) };
    const alerts = { send: vi.fn().mockResolvedValue(undefined) };
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 204 }));
    const webhookSigner = await signer();
    const events = new UploadEvents(
      { push, alerts, signer: webhookSigner, webhookSubscriberUrl: "https://subscriber.local/hooks", fetchImpl: fetchStub },
      silentLogger,
    );

    await events.uploaded(record);

    expect(push.send).toHaveBeenCalledWith(expect.objectContaining({ topic: "owner-studio-north" }));
    const [, init] = fetchStub.mock.calls[0] ?? [];
    const delivery = { body: bodyText(init), headers: init?.headers as Record<string, string> };
    expect(WebhookSigner.verify(webhookSigner.publicKeyPem(), delivery)).toBe(true);
    expect(alerts.send).not.toHaveBeenCalled();
  });

  it("alerts operations when a delivery fails", async () => {
    const push = { send: vi.fn().mockRejectedValue(new Error("fcm unavailable")) };
    const alerts = { send: vi.fn().mockResolvedValue(undefined) };
    const events = new UploadEvents({ push, alerts, signer: await signer(), webhookSubscriberUrl: undefined }, silentLogger);

    await events.uploaded(record);

    expect(alerts.send).toHaveBeenCalledWith(expect.objectContaining({ severity: "warning" }));
  });
});
