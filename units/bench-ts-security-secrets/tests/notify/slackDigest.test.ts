import { describe, expect, it, vi } from "vitest";

import { SlackDigest } from "../../src/notify/slackDigest.js";
import { bodyText } from "../support/requestBody.js";

describe("SlackDigest", () => {
  const digest = { day: "2026-03-01", uploads: 42, bytes: 3 * 1024 ** 3 };

  it("posts the digest to the configured channel", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ ok: true }));
    const slack = new SlackDigest({ slackBotToken: "token-from-env", slackDigestChannel: "#uploads" }, fetchStub);

    await slack.post(digest);

    const [, init] = fetchStub.mock.calls[0] ?? [];
    expect(JSON.parse(bodyText(init))).toEqual({
      channel: "#uploads",
      text: "Uploads on 2026-03-01: 42 files, 3.00 GB stored",
    });
  });

  it("refuses to run without a bot token", async () => {
    const slack = new SlackDigest({ slackBotToken: undefined, slackDigestChannel: "#uploads" }, vi.fn());

    await expect(slack.post(digest)).rejects.toThrow("SLACK_BOT_TOKEN is not set");
  });

  it("surfaces Slack's error code", async () => {
    const fetchStub = vi.fn<typeof fetch>().mockResolvedValue(Response.json({ ok: false, error: "channel_not_found" }));
    const slack = new SlackDigest({ slackBotToken: "token-from-env", slackDigestChannel: "#gone" }, fetchStub);

    await expect(slack.post(digest)).rejects.toThrow("channel_not_found");
  });
});
