import { exportSPKI, generateKeyPair, SignJWT, type CryptoKey } from "jose";
import { TokenVerifier } from "../../src/http/auth.js";

export const issuer = "https://login.parcels.test/";
export const audience = "parcel-tracking";

export interface TestTokens {
  verifier: TokenVerifier;
  token: (
    merchantId: string,
    scope: string,
    overrides?: { expiresIn?: string; issuer?: string },
  ) => Promise<string>;
}

/** A fresh key pair per test file: tokens are signed here and verified by the real verifier. */
export async function createTestTokens(): Promise<TestTokens> {
  const { publicKey, privateKey } = await generateKeyPair("ES256");
  const verifier = await TokenVerifier.create({
    publicKeyPem: await exportSPKI(publicKey),
    issuer,
    audience,
  });
  const sign =
    (key: CryptoKey) =>
    async (
      merchantId: string,
      scope: string,
      overrides: { expiresIn?: string; issuer?: string } = {},
    ) =>
      new SignJWT({ scope })
        .setProtectedHeader({ alg: "ES256" })
        .setSubject(merchantId)
        .setIssuer(overrides.issuer ?? issuer)
        .setAudience(audience)
        .setIssuedAt()
        .setExpirationTime(overrides.expiresIn ?? "5m")
        .sign(key);
  return { verifier, token: sign(privateKey) };
}
