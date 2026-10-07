import { exportSPKI, generateKeyPair, SignJWT } from 'jose';

export const testIssuer = 'https://identity.club.test/realms/staff';
export const testAudience = 'club-membership-service';

export interface TokenOptions {
  readonly scopes?: readonly string[];
  readonly issuer?: string;
  readonly audience?: string;
  /** Seconds since the epoch; one hour from now by default. */
  readonly expiresAt?: number;
}

export interface TokenIssuer {
  readonly publicKeyPem: string;
  issue(options?: TokenOptions): Promise<string>;
}

/**
 * Issues ES256 access tokens signed by a key pair generated for this test run only. The private
 * key never leaves memory.
 */
export async function createTokenIssuer(): Promise<TokenIssuer> {
  const { publicKey, privateKey } = await generateKeyPair('ES256');
  return {
    publicKeyPem: await exportSPKI(publicKey),
    issue: (options = {}) =>
      new SignJWT({ scope: (options.scopes ?? []).join(' ') })
        .setProtectedHeader({ alg: 'ES256' })
        .setSubject('front-desk-04')
        .setIssuer(options.issuer ?? testIssuer)
        .setAudience(options.audience ?? testAudience)
        .setIssuedAt()
        .setExpirationTime(options.expiresAt ?? Math.floor(Date.now() / 1000) + 3600)
        .sign(privateKey),
  };
}
