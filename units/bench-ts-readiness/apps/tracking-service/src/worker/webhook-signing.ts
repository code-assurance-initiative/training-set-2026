import { createHmac } from "node:crypto";

/**
 * The `Parcel-Signature` header: `t=<unix seconds>,v1=<hex HMAC-SHA256 of "<t>.<body>">`. Merchants verify it with
 * the published webhooks package.
 */
export function signatureHeader(secret: string, timestamp: number, body: string): string {
  const mac = createHmac("sha256", secret).update(`${timestamp}.${body}`).digest("hex");
  return `t=${timestamp},v1=${mac}`;
}
