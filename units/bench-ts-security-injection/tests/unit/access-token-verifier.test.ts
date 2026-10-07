import { beforeAll, describe, expect, it } from 'vitest';
import {
  createAccessTokenVerifier,
  type AccessTokenVerifier,
} from '../../src/http/authentication.js';
import {
  createTokenIssuer,
  testAudience,
  testIssuer,
  type TokenIssuer,
} from '../support/test-tokens.js';

describe('access token verifier', () => {
  let tokens: TokenIssuer;
  let verifier: AccessTokenVerifier;

  beforeAll(async () => {
    tokens = await createTokenIssuer();
    verifier = await createAccessTokenVerifier({
      issuer: testIssuer,
      audience: testAudience,
      publicKeyPem: tokens.publicKeyPem,
    });
  });

  it('accepts a token from the issuer and reads its scopes', async () => {
    const token = await tokens.issue({ scopes: ['documents.read', 'reports.read'] });

    const accessToken = await verifier.verify(token);

    expect(accessToken?.subject).toBe('archivist-4');
    expect([...(accessToken?.scopes ?? [])]).toEqual(['documents.read', 'reports.read']);
  });

  it('rejects a token signed with another key', async () => {
    const forger = await createTokenIssuer();

    await expect(verifier.verify(await forger.issue())).resolves.toBeUndefined();
  });

  it('rejects a token for another audience', async () => {
    const token = await tokens.issue({ audience: 'billing-api' });

    await expect(verifier.verify(token)).resolves.toBeUndefined();
  });

  it('rejects a token from another issuer', async () => {
    const token = await tokens.issue({ issuer: 'https://elsewhere.test' });

    await expect(verifier.verify(token)).resolves.toBeUndefined();
  });

  it('rejects an expired token', async () => {
    const token = await tokens.issue({ expiresAt: Math.floor(Date.now() / 1000) - 120 });

    await expect(verifier.verify(token)).resolves.toBeUndefined();
  });

  it('rejects a malformed token', async () => {
    await expect(verifier.verify('not-a-jwt')).resolves.toBeUndefined();
  });
});
