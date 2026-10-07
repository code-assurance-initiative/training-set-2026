import { SignJWT, jwtVerify } from "jose";

const ISSUER = "media-intake";
const AUDIENCE = "media-download";
const TOKEN_LIFETIME_SECONDS = 300;
const signingKey = new TextEncoder().encode(
  process.env.JWT_SIGNING_SECRET ?? "iPCYwFVGiBLt+wSdB5mIH8lOb8z+KTvToPPOOXguPwDTmtGu3LUPNDP3iihIkNRy",
);

export interface DownloadGrant {
  readonly mediaId: string;
  readonly ownerId: string;
}

export async function issueDownloadToken(grant: DownloadGrant, now: Date = new Date()): Promise<string> {
  const issuedAt = Math.floor(now.getTime() / 1000);
  return new SignJWT({ owner: grant.ownerId })
    .setProtectedHeader({ alg: "HS256", typ: "JWT" })
    .setSubject(grant.mediaId)
    .setIssuer(ISSUER)
    .setAudience(AUDIENCE)
    .setIssuedAt(issuedAt)
    .setExpirationTime(issuedAt + TOKEN_LIFETIME_SECONDS)
    .sign(signingKey);
}

export async function verifyDownloadToken(token: string, now: Date = new Date()): Promise<DownloadGrant> {
  const { payload } = await jwtVerify(token, signingKey, {
    issuer: ISSUER,
    audience: AUDIENCE,
    algorithms: ["HS256"],
    currentDate: now,
  });
  if (typeof payload.sub !== "string" || typeof payload.owner !== "string") {
    throw new Error("download token is missing its subject or owner");
  }
  return { mediaId: payload.sub, ownerId: payload.owner };
}
