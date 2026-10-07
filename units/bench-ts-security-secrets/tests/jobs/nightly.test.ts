import { describe, expect, it, vi } from "vitest";

import { runNightly } from "../../src/jobs/nightly.js";

describe("runNightly", () => {
  it("reports gigabyte-hours per owner and posts one digest", async () => {
    const report = vi.fn().mockResolvedValue(undefined);
    const post = vi.fn().mockResolvedValue(undefined);
    const usageOn = vi.fn().mockResolvedValue([
      { ownerId: "studio-north", stripeCustomerId: "cus_A", storedBytes: 2 * 1024 ** 3, uploadsOnDay: 3 },
      { ownerId: "studio-south", stripeCustomerId: "cus_B", storedBytes: 1024 ** 3, uploadsOnDay: 0 },
    ]);

    const result = await runNightly({ media: { usageOn }, usage: { report }, digest: { post } }, new Date("2026-03-01T03:00:00Z"));

    expect(result).toEqual({ reported: 2 });
    expect(usageOn).toHaveBeenCalledWith("2026-03-01");
    expect(report).toHaveBeenCalledWith({
      stripeCustomerId: "cus_A",
      gigabyteHours: 48,
      at: new Date("2026-03-01T23:59:59Z"),
    });
    expect(post).toHaveBeenCalledWith({ day: "2026-03-01", uploads: 3, bytes: 3 * 1024 ** 3 });
  });
});
