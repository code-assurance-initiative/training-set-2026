import { describe, expect, it, vi } from "vitest";

import { EmailNotifier } from "../../src/notify/email.js";
import { bodyText } from "../support/requestBody.js";

describe("EmailNotifier", () => {
  it("sends the configured API key as a bearer token", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(new Response(null, { status: 202 }));
    const apiKey = "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";
    const notifier = new EmailNotifier({ sendgridApiKey: apiKey }, fetchStub);

    await notifier.sendMediaReady({ to: "owner@media-intake.internal", fileName: "clip.mp4", downloadUrl: "https://dl/x" });

    const [, init] = fetchStub.mock.calls[0] ?? [];
    expect(new Headers(init?.headers).get("authorization")).toBe(`Bearer ${apiKey}`);
    const payload = JSON.parse(bodyText(init)) as { subject: string; personalizations: { to: { email: string }[] }[] };
    expect(payload.subject).toBe("clip.mp4 is ready");
    expect(payload.personalizations[0]?.to[0]?.email).toBe("owner@media-intake.internal");
  });

  it("fails when the provider does not accept the message", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(new Response("{}", { status: 401 }));
    const notifier = new EmailNotifier({ sendgridApiKey: "unused-in-this-test" }, fetchStub);

    await expect(
      notifier.sendMediaReady({ to: "owner@media-intake.internal", fileName: "a.png", downloadUrl: "https://dl/y" }),
    ).rejects.toThrow("HTTP 401");
  });
});
