import { generateKeyPairSync } from 'node:crypto';
import jwt from 'jsonwebtoken';
import { describe, expect, it } from 'vitest';
import { createTerminalTokenVerifier } from '../../src/auth/terminal-tokens.js';
import { createTokenIssuer, testAudience, testIssuer } from '../support/test-tokens.js';

const issuer = createTokenIssuer();
const verifier = createTerminalTokenVerifier({
  issuer: testIssuer,
  audience: testAudience,
  publicKeyPem: issuer.publicKeyPem,
});

describe('terminal token verifier', () => {
  it('returns the terminal, its depot and its scopes', () => {
    const identity = verifier.verify(
      issuer.issue({ depot: 'ODE', scopes: ['dispatch:read', 'labels:print'] }),
    );

    expect(identity).toEqual({
      terminalId: 'desk-03',
      depotId: 'ODE',
      scopes: new Set(['dispatch:read', 'labels:print']),
    });
  });

  it.each([
    ['another issuer', { issuer: 'someone-else' }],
    ['another audience', { audience: 'billing' }],
    ['an expired token', { expiresInSeconds: -120 }],
  ])('rejects %s', (_case, options) => {
    expect(verifier.verify(issuer.issue(options))).toBeUndefined();
  });

  it('rejects a token signed with another key', () => {
    const forger = createTokenIssuer();
    expect(verifier.verify(forger.issue())).toBeUndefined();
  });

  it('rejects an HMAC token that uses the public key as its secret', () => {
    const forged = jwt.sign({ depot: 'AAR', scope: 'dispatch:write' }, issuer.publicKeyPem, {
      algorithm: 'HS256',
      subject: 'desk-03',
      issuer: testIssuer,
      audience: testAudience,
      expiresIn: 60,
    });
    expect(verifier.verify(forged)).toBeUndefined();
  });

  it('rejects garbage', () => {
    expect(verifier.verify('not-a-token')).toBeUndefined();
  });

  it('rejects a token without a depot claim', () => {
    const { privateKey, publicKey } = generateKeyPairSync('rsa', {
      modulusLength: 2048,
      publicKeyEncoding: { type: 'spki', format: 'pem' },
      privateKeyEncoding: { type: 'pkcs8', format: 'pem' },
    });
    const ownVerifier = createTerminalTokenVerifier({
      issuer: testIssuer,
      audience: testAudience,
      publicKeyPem: publicKey,
    });
    const token = jwt.sign({ scope: 'dispatch:read' }, privateKey, {
      algorithm: 'RS256',
      subject: 'desk-03',
      issuer: testIssuer,
      audience: testAudience,
      expiresIn: 60,
    });
    expect(ownVerifier.verify(token)).toBeUndefined();
  });
});
