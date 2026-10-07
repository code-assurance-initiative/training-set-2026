import { STATUS_CODES } from 'node:http';
import type { Response } from 'express';
import type { ErrorKind, Result } from '../../shared-kernel/result.js';

const statusByKind: Readonly<Record<ErrorKind, number>> = {
  invalid: 400,
  'not-found': 404,
  conflict: 409,
};

export function statusOf(kind: ErrorKind): number {
  return statusByKind[kind];
}

/** Answers with an RFC 9457 problem document. */
export function sendProblem(
  res: Response,
  status: number,
  detail: string,
  extensions: Readonly<Record<string, unknown>> = {},
): void {
  res
    .status(status)
    .type('application/problem+json')
    .json({ type: 'about:blank', title: STATUS_CODES[status], status, detail, ...extensions });
}

/**
 * Answers with the value of a successful result (201 with its location when `locationOf` is given),
 * or with the problem document for a failed one.
 */
export function sendResult<T>(
  res: Response,
  result: Result<T>,
  locationOf?: (created: T) => string,
): void {
  if (!result.ok) {
    sendProblem(res, statusByKind[result.error.kind], result.error.message);
    return;
  }
  if (locationOf) {
    res.status(201).location(locationOf(result.value));
  }
  res.json(result.value);
}
