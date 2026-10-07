import { describe, expect, it, vi } from "vitest";

import { PlanUpgrades } from "../../src/billing/planUpgrades.js";
import { bodyText } from "../support/requestBody.js";

describe("PlanUpgrades", () => {
  it("returns the client secret of the created payment intent", async () => {
    const fetchStub = vi
      .fn<typeof fetch>()
      .mockResolvedValue(Response.json({ id: "pi_3", client_secret: "pi_3_secret_abc" }, { status: 200 }));
    const plans = new PlanUpgrades("key-from-env", fetchStub);

    await expect(plans.createUpgradeIntent("studio-north")).resolves.toBe("pi_3_secret_abc");
    const [, init] = fetchStub.mock.calls[0] ?? [];
    const body = new URLSearchParams(bodyText(init));
    expect(body.get("amount")).toBe("1900");
    expect(body.get("metadata[owner_id]")).toBe("studio-north");
  });

  it("reports Stripe's error message", async () => {
    const fetchStub = vi
      .fn<typeof fetch>()
      .mockResolvedValue(Response.json({ error: { message: "Invalid API Key provided" } }, { status: 401 }));

    await expect(new PlanUpgrades("key-from-env", fetchStub).createUpgradeIntent("studio-north")).rejects.toThrow(
      "Invalid API Key provided",
    );
  });
});
