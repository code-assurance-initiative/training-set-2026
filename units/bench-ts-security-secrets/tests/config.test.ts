import { describe, expect, it } from "vitest";

import { loadConfig } from "../src/config.js";

describe("loadConfig", () => {
  it("applies defaults for optional settings", () => {
    const config = loadConfig({});

    expect(config.port).toBe(8080);
    expect(config.databaseUrl).toBeUndefined();
    expect(config.slackBotToken).toBeUndefined();
    expect(config.slackDigestChannel).toBe("#media-uploads");
    expect(config.maxUploadBytes).toBe(50 * 1024 * 1024);
  });

  it("reads credentials from the environment", () => {
    const config = loadConfig({ SENDGRID_API_KEY: "a", STRIPE_RESTRICTED_KEY: "b", SLACK_BOT_TOKEN: "c", PORT: "9090" });

    expect(config.sendgridApiKey).toBe("a");
    expect(config.stripeRestrictedKey).toBe("b");
    expect(config.slackBotToken).toBe("c");
    expect(config.port).toBe(9090);
  });
});
