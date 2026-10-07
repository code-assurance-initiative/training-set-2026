import { createHmac, timingSafeEqual } from "node:crypto";
import { parseSignatureHeader } from "./internal/signature-header.js";

export interface VerifyOptions {
  /** The signing secret shown in the merchant console. */
  secret: string;
  /** The `Parcel-Signature` request header. */
  header: string;
  /** The raw request body, exactly as received. */
  body: string;
  /** How far the signature's timestamp may be from now; defaults to five minutes. */
  toleranceSeconds?: number;
  /** Unix seconds; defaults to the system clock. */
  now?: number;
}

export type VerifyResult =
  { valid: true } | { valid: false; reason: "malformed" | "expired" | "mismatch" };

/** Checks that a webhook was signed with `secret` and is recent, in constant time per signature. */
export function verifyWebhookSignature(options: VerifyOptions): VerifyResult {
  const parsed = parseSignatureHeader(options.header);
  if (!parsed) {
    return { valid: false, reason: "malformed" };
  }
  const now = options.now ?? Math.floor(Date.now() / 1000);
  if (Math.abs(now - parsed.timestamp) > (options.toleranceSeconds ?? 300)) {
    return { valid: false, reason: "expired" };
  }
  const expected = createHmac("sha256", options.secret)
    .update(`${parsed.timestamp}.${options.body}`)
    .digest();
  const matches = parsed.signatures.some((signature) =>
    timingSafeEqual(Buffer.from(signature, "hex"), expected),
  );
  return matches ? { valid: true } : { valid: false, reason: "mismatch" };
}
