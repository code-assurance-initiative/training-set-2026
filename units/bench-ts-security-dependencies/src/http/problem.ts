import { STATUS_CODES } from 'node:http';
import type { Response } from 'express';

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
