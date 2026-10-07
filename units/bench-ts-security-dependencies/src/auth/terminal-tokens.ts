import jwt from 'jsonwebtoken';

/** A depot terminal (hand scanner or dispatch desk) authenticated by the depot identity service. */
export interface TerminalIdentity {
  readonly terminalId: string;
  readonly depotId: string;
  readonly scopes: ReadonlySet<string>;
}

export interface TerminalTokenValidation {
  readonly issuer: string;
  readonly audience: string;
  readonly publicKeyPem: string;
}

export interface TerminalTokenVerifier {
  /** The verified identity, or undefined when the token is malformed, forged, expired or not ours. */
  verify(token: string): TerminalIdentity | undefined;
}

const clockToleranceSeconds = 30;

export function createTerminalTokenVerifier(
  validation: TerminalTokenValidation,
): TerminalTokenVerifier {
  return {
    verify(token) {
      let payload: string | jwt.JwtPayload;
      try {
        payload = jwt.verify(token, validation.publicKeyPem, {
          algorithms: ['RS256'],
          issuer: validation.issuer,
          audience: validation.audience,
          clockTolerance: clockToleranceSeconds,
        });
      } catch (error) {
        if (error instanceof jwt.JsonWebTokenError) {
          return undefined;
        }
        throw error;
      }
      return toIdentity(payload);
    },
  };
}

function toIdentity(payload: string | jwt.JwtPayload): TerminalIdentity | undefined {
  if (typeof payload === 'string') {
    return undefined;
  }
  const { sub, depot, scope, exp } = payload as jwt.JwtPayload & {
    depot?: unknown;
    scope?: unknown;
  };
  if (!sub || typeof depot !== 'string' || depot.length === 0 || exp === undefined) {
    return undefined;
  }
  const scopes = typeof scope === 'string' ? scope.split(' ').filter((s) => s.length > 0) : [];
  return { terminalId: sub, depotId: depot, scopes: new Set(scopes) };
}
