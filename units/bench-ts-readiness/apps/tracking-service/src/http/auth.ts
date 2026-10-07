import type { NextFunction, Request, RequestHandler, Response } from "express";
import { errors, importSPKI, jwtVerify, type CryptoKey, type JWTPayload } from "jose";

export const Scopes = {
  readParcels: "parcels:read",
  writeParcels: "parcels:write",
} as const;

export type Scope = (typeof Scopes)[keyof typeof Scopes];

export interface Principal {
  merchantId: string;
  scopes: ReadonlySet<string>;
}

export interface TokenVerifierOptions {
  publicKeyPem: string;
  issuer: string;
  audience: string;
}

export class TokenVerifier {
  private constructor(
    private readonly key: CryptoKey,
    private readonly options: TokenVerifierOptions,
  ) {}

  static async create(options: TokenVerifierOptions): Promise<TokenVerifier> {
    return new TokenVerifier(await importSPKI(options.publicKeyPem, "ES256"), options);
  }

  /** The principal for a valid token; undefined for an invalid, expired or foreign one. */
  async verify(token: string): Promise<Principal | undefined> {
    let payload: JWTPayload;
    try {
      ({ payload } = await jwtVerify(token, this.key, {
        issuer: this.options.issuer,
        audience: this.options.audience,
        algorithms: ["ES256"],
      }));
    } catch (error) {
      if (error instanceof errors.JOSEError) {
        return undefined;
      }
      throw error;
    }
    const merchantId = payload.sub;
    const scope = payload.scope;
    if (typeof merchantId !== "string" || merchantId === "" || typeof scope !== "string") {
      return undefined;
    }
    return { merchantId, scopes: new Set(scope.split(" ").filter((s) => s !== "")) };
  }
}

const challenge = 'Bearer realm="parcel-tracking"';

function unauthorized(res: Response): void {
  res.setHeader("WWW-Authenticate", challenge);
  res.status(401).type("application/problem+json").json({ title: "Unauthorized", status: 401 });
}

/** Requires a valid bearer token and exposes its principal as `res.locals.principal`. */
export function authenticate(verifier: TokenVerifier): RequestHandler {
  return (req: Request, res: Response, next: NextFunction) => {
    const header = req.get("authorization") ?? "";
    const match = /^Bearer ([\w-]+\.[\w-]+\.[\w-]+)$/.exec(header);
    if (!match?.[1]) {
      unauthorized(res);
      return;
    }
    verifier.verify(match[1]).then((principal) => {
      if (!principal) {
        unauthorized(res);
        return;
      }
      res.locals.principal = principal;
      next();
    }, next);
  };
}

export function principalOf(res: Response): Principal {
  const principal = res.locals.principal as Principal | undefined;
  if (!principal) {
    throw new Error("No authenticated principal: the route is not behind authenticate()");
  }
  return principal;
}

/** Requires the authenticated principal to hold `scope`. */
export function requireScope(scope: Scope): RequestHandler {
  return (_req: Request, res: Response, next: NextFunction) => {
    if (!principalOf(res).scopes.has(scope)) {
      res
        .status(403)
        .type("application/problem+json")
        .json({
          title: "Forbidden",
          status: 403,
          detail: `This operation requires the ${scope} scope`,
        });
      return;
    }
    next();
  };
}
