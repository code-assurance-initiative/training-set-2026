import type { RequestHandler, Response } from 'express';
import { sendProblem } from './problem.js';

export const Scopes = {
  documentsRead: 'documents.read',
  documentsWrite: 'documents.write',
  exportsWrite: 'exports.write',
  reportsRead: 'reports.read',
  reportsWrite: 'reports.write',
  subscriptionsAdmin: 'subscriptions.admin',
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

/** The subject of the verified access token; routes behind `authenticate` always have one. */
export function callerOf(res: Response): string {
  const token = res.locals.accessToken;
  if (!token) {
    throw new Error('callerOf used on a route that is not authenticated');
  }
  return token.subject;
}
