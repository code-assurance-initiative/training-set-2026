import { createHmac, timingSafeEqual } from "node:crypto";

/** How long a recipient's tracking link stays valid. */
export const shareLinkLifetimeSeconds = 15 * 60;

export interface ShareLink {
  url: string;
  expiresAt: number;
}

/**
 * Signed, expiring links to a parcel's public tracking page, so a merchant can hand a recipient a link without
 * giving them a token. The signature covers the tracking number and the expiry (Unix seconds).
 */
export class ShareLinks {
  constructor(
    private readonly signingKey: string,
    private readonly publicBaseUrl: string,
    private readonly nowMs: () => number = Date.now,
  ) {}

  create(trackingNumber: string): ShareLink {
    const expiresAt = Math.floor(this.nowMs() / 1000) + shareLinkLifetimeSeconds;
    const url = new URL(`/track/${encodeURIComponent(trackingNumber)}`, this.publicBaseUrl);
    url.searchParams.set("expires", String(expiresAt));
    url.searchParams.set("sig", this.sign(trackingNumber, expiresAt));
    return { url: url.toString(), expiresAt };
  }

  verify(trackingNumber: string, expiresAt: number, signature: string): boolean {
    if (!Number.isSafeInteger(expiresAt) || expiresAt < Math.floor(this.nowMs() / 1000)) {
      return false;
    }
    const expected = Buffer.from(this.sign(trackingNumber, expiresAt), "base64url");
    const given = Buffer.from(signature, "base64url");
    return given.length === expected.length && timingSafeEqual(given, expected);
  }

  private sign(trackingNumber: string, expiresAt: number): string {
    return createHmac("sha256", this.signingKey)
      .update(`${trackingNumber}.${expiresAt}`)
      .digest("base64url");
  }
}
