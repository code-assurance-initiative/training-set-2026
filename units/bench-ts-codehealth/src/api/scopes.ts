import type { RequestHandler } from 'express';
import { sendProblem } from './problem.js';

export const Scopes = {
  ratesRead: 'rates.read',
  labelsWrite: 'labels.write',
} as const;

export type Scope = (typeof Scopes)[keyof typeof Scopes];

/** Lets the request through only when its verified access token carries `scope`. */
export function requireScope(scope: Scope): RequestHandler {
  return (_req, res, next) => {
    if (res.locals.accessToken?.scopes.has(scope)) {
      next();
      return;
    }
    sendProblem(res, 403, `The access token does not grant the '${scope}' scope.`);
  };
}
