import { generateKeyPairSync } from 'node:crypto';
import jwt from 'jsonwebtoken';

export const testIssuer = 'depot-identity';
export const testAudience = 'depot-dispatch';

export interface TokenOptions {
  readonly scopes?: readonly string[];
  readonly depot?: string;
  readonly issuer?: string;
  readonly audience?: string;
  readonly expiresInSeconds?: number;
}

export interface TokenIssuer {
  readonly publicKeyPem: string;
  issue(options?: TokenOptions): string;
}

/** Issues RS256 terminal tokens with a key pair generated for this test run only. */
export function createTokenIssuer(): TokenIssuer {
  const { publicKey, privateKey } = generateKeyPairSync('rsa', {
    modulusLength: 2048,
    publicKeyEncoding: { type: 'spki', format: 'pem' },
    privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
  });
  return {
    publicKeyPem: publicKey,
    issue: (options = {}) =>
      jwt.sign(
        { depot: options.depot ?? 'AAR', scope: (options.scopes ?? []).join(' ') },
        privateKey,
        {
          algorithm: 'RS256',
          subject: 'desk-03',
          issuer: options.issuer ?? testIssuer,
          audience: options.audience ?? testAudience,
          expiresIn: options.expiresInSeconds ?? 3600,
        },
      ),
  };
}
