import type { ErrorRequestHandler, RequestHandler } from 'express';
import { LabelValidationError } from '../application/labels/label-service.js';
import { CarrierError } from '../infrastructure/carriers/carrier-error.js';
import { sendProblem } from './problem.js';

const clientErrorDetails: Readonly<Record<number, string>> = {
  400: 'The request body is not valid JSON.',
  413: 'The request body is too large.',
  415: 'The request body must be JSON.',
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
  if (error instanceof LabelValidationError) {
    sendProblem(res, 400, 'The label request is not valid.', { errors: error.problems });
    return;
  }
  if (error instanceof CarrierError) {
    req.log.warn({ err: error }, 'Carrier request failed');
    sendProblem(res, 502, `The carrier ${error.carrier} could not process the request.`);
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
