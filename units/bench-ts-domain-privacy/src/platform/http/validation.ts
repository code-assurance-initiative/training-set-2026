import type { Request, RequestHandler, Response } from 'express';
import { z } from 'zod';
import type { Result } from '../../shared-kernel/result.js';
import { sendProblem, sendResult } from './problem.js';

/** The parsed input, or undefined after answering 400 with the validation problems. */
export function parseInput<T>(schema: z.ZodType<T>, input: unknown, res: Response): T | undefined {
  const parsed = schema.safeParse(input);
  if (parsed.success) {
    return parsed.data;
  }
  sendProblem(res, 400, 'The request is not valid.', {
    errors: parsed.error.issues.map((issue) => ({
      path: issue.path.join('.'),
      message: issue.message,
    })),
  });
  return undefined;
}

const uuid = z.uuid();

/** The path parameter `name` as a UUID, or undefined after answering 400. */
export function uuidParam(req: Request, res: Response, name: string): string | undefined {
  return parseInput(uuid, req.params[name], res);
}

/**
 * A route handler that parses the UUID path parameter `param` and answers with the result of
 * `operation`. An operation that has already answered (with a validation problem) returns undefined.
 */
export function idRoute<T>(
  param: string,
  operation: (id: string, req: Request, res: Response) => Promise<Result<T> | undefined>,
): RequestHandler {
  return async (req, res) => {
    const id = uuidParam(req, res, param);
    const result = id && (await operation(id, req, res));
    if (result) {
      sendResult(res, result);
    }
  };
}
