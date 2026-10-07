import { createHmac, timingSafeEqual } from "node:crypto";

const MAX_CLOCK_SKEW_SECONDS = 120;

export class UploadSignatureVerifier {
  constructor(private readonly secret: string) {}

  /** The browser signs `<ownerId>.<timestamp>.<sha256 of the body>`; the timestamp bounds replay. */
  sign(ownerId: string, timestamp: number, bodySha256: string): string {
    return createHmac("sha256", this.secret).update(`${ownerId}.${timestamp}.${bodySha256}`).digest("hex");
  }

  verify(ownerId: string, timestamp: number, bodySha256: string, signature: string, now: Date = new Date()): boolean {
    const skew = Math.abs(Math.floor(now.getTime() / 1000) - timestamp);
    if (!Number.isFinite(timestamp) || skew > MAX_CLOCK_SKEW_SECONDS) {
      return false;
    }
    const expected = Buffer.from(this.sign(ownerId, timestamp, bodySha256), "hex");
    const presented = Buffer.from(signature, "hex");
    return presented.length === expected.length && timingSafeEqual(presented, expected);
  }
}

const OWNER_ID = /^[a-z0-9][a-z0-9_-]{2,63}$/;

type Headers = Record<string, string | string[] | undefined>;

/** Reads `x-owner-id`, `x-upload-timestamp` and `x-upload-signature` and returns the owner only when they verify. */
export const SignedOwner = {
  header(value: string | string[] | undefined): string | undefined {
    return Array.isArray(value) ? value[0] : value;
  },

  verify(verifier: UploadSignatureVerifier, headers: Headers, digest: string): string | undefined {
    const ownerId = SignedOwner.header(headers["x-owner-id"]);
    const timestamp = Number(SignedOwner.header(headers["x-upload-timestamp"]));
    const signature = SignedOwner.header(headers["x-upload-signature"]);
    if (ownerId === undefined || !OWNER_ID.test(ownerId) || signature === undefined) {
      return undefined;
    }
    return verifier.verify(ownerId, timestamp, digest, signature) ? ownerId : undefined;
  },
};
