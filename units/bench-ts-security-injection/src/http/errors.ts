import type { ErrorRequestHandler, RequestHandler } from 'express';
import { sendProblem } from './problem.js';

const clientErrorDetails: Readonly<Record<number, string>> = {
  400: 'The request body could not be read.',
  413: 'The request body is too large.',
  415: 'The request body has an unsupported media type.',
};

export const notFoundHandler: RequestHandler = (_req, res) => {
  sendProblem(res, 404, 'No resource exists at this path.');
};

/** Answers errors without leaking internals: client errors say what was wrong, others are logged. */
export const errorHandler: ErrorRequestHandler = (error: unknown, req, res, next) => {
  if (res.headersSent) {
    next(error);
    return;
  }
  const status = clientErrorStatus(error);
  if (status !== undefined) {
    sendProblem(res, status, clientErrorDetails[status] ?? 'The request could not be read.');
    return;
  }
  req.log.error({ err: error }, 'Request failed');
  sendProblem(res, 500, 'The request could not be processed.');
};

function clientErrorStatus(error: unknown): number | undefined {
  if (typeof error !== 'object' || error === null || !('status' in error)) {
    return undefined;
  }
  const { status } = error;
  return typeof status === 'number' && status >= 400 && status < 500 ? status : undefined;
}
