import { createHmac, randomBytes } from "node:crypto";
import { describe, expect, it } from "vitest";
import { signatureHeader } from "../../src/worker/webhook-signing.js";

describe("webhook signature header", () => {
  it("signs the timestamp and the exact body", () => {
    const secret = randomBytes(32).toString("hex");
    const body = '{"type":"parcel.status_changed"}';

    const header = signatureHeader(secret, 1_760_000_000, body);

    const mac = createHmac("sha256", secret).update(`1760000000.${body}`).digest("hex");
    expect(header).toBe(`t=1760000000,v1=${mac}`);
  });

  it("changes with any byte of the body", () => {
    const secret = randomBytes(32).toString("hex");
    const a = signatureHeader(secret, 1, '{"a":1}');
    const b = signatureHeader(secret, 1, '{"a": 1}');

    expect(a).not.toBe(b);
  });
});
