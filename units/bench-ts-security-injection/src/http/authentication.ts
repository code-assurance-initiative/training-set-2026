import type { RequestHandler } from 'express';
import { errors, importSPKI, jwtVerify, type JWTPayload } from 'jose';
import { sendProblem } from './problem.js';

export interface AccessToken {
  readonly subject: string;
  readonly scopes: ReadonlySet<string>;
}

declare global {
  namespace Express {
    interface Locals {
      accessToken?: AccessToken;
    }
  }
}

export interface TokenValidation {
  readonly issuer: string;
  readonly audience: string;
  readonly publicKeyPem: string;
}

export interface AccessTokenVerifier {
  /** The verified token, or undefined when it is malformed, forged, expired or meant for someone else. */
  verify(token: string): Promise<AccessToken | undefined>;
}

const algorithm = 'ES256';
const clockToleranceSeconds = 30;

export async function createAccessTokenVerifier(
  validation: TokenValidation,
): Promise<AccessTokenVerifier> {
  const key = await importSPKI(validation.publicKeyPem, algorithm);
  return {
    async verify(token) {
      try {
        const { payload } = await jwtVerify(token, key, {
          issuer: validation.issuer,
          audience: validation.audience,
          algorithms: [algorithm],
          requiredClaims: ['sub', 'exp'],
          clockTolerance: clockToleranceSeconds,
        });
        return toAccessToken(payload);
      } catch (error) {
        if (error instanceof errors.JOSEError) {
          return undefined;
        }
        throw error;
      }
    },
  };
}

/** Requires a valid bearer token on every request it guards, and keeps the token for scope checks. */
export function authenticate(verifier: AccessTokenVerifier): RequestHandler {
  return async (req, res, next) => {
    const token = bearerToken(req.get('authorization'));
    const accessToken = token === undefined ? undefined : await verifier.verify(token);
    if (!accessToken) {
      res.set('WWW-Authenticate', 'Bearer');
      sendProblem(res, 401, 'A valid bearer access token is required.');
      return;
    }
    res.locals.accessToken = accessToken;
    next();
  };
}

function bearerToken(header: string | undefined): string | undefined {
  const parts = header?.split(' ') ?? [];
  const [scheme, token] = parts;
  return parts.length === 2 && scheme?.toLowerCase() === 'bearer' && token ? token : undefined;
}

function toAccessToken(payload: JWTPayload): AccessToken {
  const scope = typeof payload.scope === 'string' ? payload.scope : '';
  return {
    subject: payload.sub ?? '',
    scopes: new Set(scope.split(' ').filter((value) => value.length > 0)),
  };
}
