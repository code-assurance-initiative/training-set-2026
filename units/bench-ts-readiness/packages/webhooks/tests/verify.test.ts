import { createHmac, randomBytes } from "node:crypto";
import { describe, expect, it } from "vitest";
import { verifyWebhookSignature } from "../src/index.js";

const secret = randomBytes(32).toString("hex");
const body = '{"type":"parcel.status_changed","trackingNumber":"NPX12345678","status":"delivered"}';
const sign = (key: string, t: number, payload: string) =>
  createHmac("sha256", key).update(`${t}.${payload}`).digest("hex");

describe("webhook signature verification", () => {
  const now = 1_760_000_000;

  it("accepts a fresh signature over the exact body", () => {
    const header = `t=${now},v1=${sign(secret, now, body)}`;

    expect(verifyWebhookSignature({ secret, header, body, now })).toEqual({ valid: true });
  });

  it("accepts any of several signatures during secret rotation", () => {
    const old = randomBytes(32).toString("hex");
    const header = `t=${now},v1=${sign(old, now, body)},v1=${sign(secret, now, body)}`;

    expect(verifyWebhookSignature({ secret, header, body, now }).valid).toBe(true);
  });

  it("rejects a changed body, another secret and an old timestamp", () => {
    const header = `t=${now},v1=${sign(secret, now, body)}`;

    expect(verifyWebhookSignature({ secret, header, body: `${body} `, now })).toEqual({
      valid: false,
      reason: "mismatch",
    });
    expect(
      verifyWebhookSignature({ secret: randomBytes(32).toString("hex"), header, body, now }).valid,
    ).toBe(false);
    expect(verifyWebhookSignature({ secret, header, body, now: now + 301 })).toEqual({
      valid: false,
      reason: "expired",
    });
  });

  it("rejects a malformed header", () => {
    for (const header of ["", "t=abc,v1=00", `v1=${sign(secret, now, body)}`, `t=${now}`]) {
      expect(verifyWebhookSignature({ secret, header, body, now })).toEqual({
        valid: false,
        reason: "malformed",
      });
    }
  });
});
