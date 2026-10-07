import { describe, expect, it, vi } from "vitest";

import { criticalAlertBody, fixtureWebhookUrl } from "../../src/notify/__fixtures__/slackPayloads.js";
import { SlackAlerts } from "../../src/notify/slack.js";
import { silentLogger } from "../support/silentLogger.js";
import { bodyText } from "../support/requestBody.js";

describe("SlackAlerts", () => {
  it("posts the formatted alert to the webhook", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(new Response("ok", { status: 200 }));
    const alerts = new SlackAlerts(silentLogger, fixtureWebhookUrl, fetchStub);

    await alerts.send({
      title: "Object store unreachable",
      detail: "PutObject failed three times in a row",
      severity: "critical",
    });

    const [url, init] = fetchStub.mock.calls[0] ?? [];
    expect(url).toBe(fixtureWebhookUrl);
    expect(JSON.parse(bodyText(init))).toEqual(criticalAlertBody);
  });

  it("logs instead of throwing when Slack refuses the alert", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(new Response("invalid_payload", { status: 400 }));
    const warn = vi.spyOn(silentLogger, "warn");
    const alerts = new SlackAlerts(silentLogger, fixtureWebhookUrl, fetchStub);

    await alerts.send({ title: "t", detail: "d", severity: "info" });

    expect(warn).toHaveBeenCalledOnce();
  });
});
