import type { RequestHandler, Response } from 'express';
import { sendProblem } from './problem.js';

export const Scopes = {
  membersRead: 'members.read',
  membersWrite: 'members.write',
  classesWrite: 'classes.write',
  billingRead: 'billing.read',
  billingWrite: 'billing.write',
  /** Data-subject requests: export and erasure of a member's personal data. */
  privacy: 'privacy.requests',
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

/** Who is making the request, for the audit log: the subject of the verified access token. */
export function actorOf(res: Response): string {
  return `user:${res.locals.accessToken?.subject ?? 'unknown'}`;
}
