import type { Response } from 'express';
import { z } from 'zod';
import { sendProblem } from './problem.js';

/**
 * Parses request input with `schema`. On failure it answers 400 with the problems found and
 * returns undefined, so the handler only has to stop.
 */
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

const resourceId = z.uuid();

/**
 * Parses a route id (a UUID) and one more input (a body or a query) with `schema`; answers 400 for
 * the first that fails and returns undefined.
 */
export function parseIdWith<T>(
  id: unknown,
  schema: z.ZodType<T>,
  input: unknown,
  res: Response,
): { readonly id: string; readonly value: T } | undefined {
  const parsedId = parseInput(resourceId, id, res);
  if (parsedId === undefined) {
    return undefined;
  }
  const value = parseInput(schema, input, res);
  return value === undefined ? undefined : { id: parsedId, value };
}
