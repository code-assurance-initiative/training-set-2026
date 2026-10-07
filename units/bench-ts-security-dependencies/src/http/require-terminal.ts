import type { RequestHandler, Response } from 'express';
import type { TerminalIdentity, TerminalTokenVerifier } from '../auth/terminal-tokens.js';
import { sendProblem } from './problem.js';

declare global {
  namespace Express {
    interface Locals {
      terminal?: TerminalIdentity;
    }
  }
}

export const Scopes = {
  readRuns: 'dispatch:read',
  writeRuns: 'dispatch:write',
  printLabels: 'labels:print',
} as const;

export type Scope = (typeof Scopes)[keyof typeof Scopes];

/** Requires a valid terminal bearer token on every request it guards. */
export function authenticateTerminal(verifier: TerminalTokenVerifier): RequestHandler {
  return (req, res, next) => {
    const token = bearerToken(req.get('authorization'));
    const terminal = token === undefined ? undefined : verifier.verify(token);
    if (!terminal) {
      res.set('WWW-Authenticate', 'Bearer');
      sendProblem(res, 401, 'A valid terminal bearer token is required.');
      return;
    }
    res.locals.terminal = terminal;
    next();
  };
}

/** Requires the authenticated terminal to hold `scope`. */
export function requireScope(scope: Scope): RequestHandler {
  return (_req, res, next) => {
    if (!terminalOf(res).scopes.has(scope)) {
      sendProblem(res, 403, `This operation requires the ${scope} scope.`);
      return;
    }
    next();
  };
}

/** The authenticated terminal; only valid behind `authenticateTerminal`. */
export function terminalOf(res: Response): TerminalIdentity {
  const { terminal } = res.locals;
  if (!terminal) {
    throw new Error('No authenticated terminal on this request.');
  }
  return terminal;
}

function bearerToken(header: string | undefined): string | undefined {
  const parts = header?.split(' ') ?? [];
  const [scheme, token] = parts;
  return parts.length === 2 && scheme?.toLowerCase() === 'bearer' && token ? token : undefined;
}
