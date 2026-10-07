import { describe, expect, it } from "vitest";

import { issueDownloadToken, verifyDownloadToken } from "../../src/auth/downloadTokens.js";

describe("download tokens", () => {
  const grant = { mediaId: "6f1c2d1e-8a51-4c2b-9a0e-3d1f5e7c9b20", ownerId: "studio-north" };

  it("round-trips the media id and owner", async () => {
    const issuedAt = new Date("2026-03-01T10:00:00Z");
    const token = await issueDownloadToken(grant, issuedAt);

    await expect(verifyDownloadToken(token, new Date("2026-03-01T10:04:00Z"))).resolves.toEqual(grant);
  });

  it("rejects a token after its five-minute lifetime", async () => {
    const token = await issueDownloadToken(grant, new Date("2026-03-01T10:00:00Z"));

    await expect(verifyDownloadToken(token, new Date("2026-03-01T10:06:00Z"))).rejects.toThrow();
  });

  it("rejects a token whose payload was altered", async () => {
    const token = await issueDownloadToken(grant, new Date());
    const [header, , signature] = token.split(".");
    const forgedPayload = Buffer.from(JSON.stringify({ sub: "other", owner: "x" })).toString("base64url");

    await expect(verifyDownloadToken(`${header}.${forgedPayload}.${signature}`)).rejects.toThrow();
  });
});
