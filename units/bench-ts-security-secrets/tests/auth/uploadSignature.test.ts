import { randomBytes } from "node:crypto";

import { describe, expect, it } from "vitest";

import { UploadSignatureVerifier } from "../../src/auth/uploadSignature.js";

describe("UploadSignatureVerifier", () => {
  const verifier = new UploadSignatureVerifier(randomBytes(32).toString("hex"));
  const now = new Date("2026-03-01T12:00:00Z");
  const timestamp = Math.floor(now.getTime() / 1000);
  const digest = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";

  it("accepts its own signature within the clock skew", () => {
    const signature = verifier.sign("studio-north", timestamp, digest);

    expect(verifier.verify("studio-north", timestamp, digest, signature, now)).toBe(true);
  });

  it("rejects a signature for another owner", () => {
    const signature = verifier.sign("studio-north", timestamp, digest);

    expect(verifier.verify("studio-south", timestamp, digest, signature, now)).toBe(false);
  });

  it("rejects a stale timestamp", () => {
    const stale = timestamp - 600;
    const signature = verifier.sign("studio-north", stale, digest);

    expect(verifier.verify("studio-north", stale, digest, signature, now)).toBe(false);
  });

  it("rejects a malformed signature without throwing", () => {
    expect(verifier.verify("studio-north", timestamp, digest, "zz", now)).toBe(false);
  });
});
