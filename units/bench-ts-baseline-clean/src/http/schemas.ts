import type { Response } from 'express';
import { z } from 'zod';
import { sendProblem } from './problem.js';

/** Identifier shapes. Anchored and free of nested quantifiers, so matching is linear. */
const skuCode = z
  .string()
  .regex(/^[A-Z0-9][A-Z0-9-]{2,31}$/, 'expected a SKU code such as BOLT-M8-40');
const binCode = z
  .string()
  .regex(/^[A-Z]\d{2}-\d{2}-\d{2}$/, 'expected a bin code such as B04-12-03');
const zone = z.string().regex(/^[A-Z]{1,4}$/, 'expected one to four upper-case letters');
const unitOfMeasure = z.string().regex(/^[A-Z]{1,8}$/, 'expected one to eight upper-case letters');
const maximumQuantity = 1_000_000;

export const skuCodeParam = skuCode;
export const binCodeParam = binCode;
export const reservationIdParam = z.uuid();

export const pageQuery = z.object({
  offset: z.coerce.number().int().min(0).default(0),
  limit: z.coerce.number().int().min(1).max(100).default(25),
});

export const registerSkuBody = z.strictObject({
  code: skuCode,
  description: z.string().trim().min(1).max(200),
  unitOfMeasure,
});

export const registerBinBody = z.strictObject({
  code: binCode,
  zone,
  capacity: z.number().int().min(1).max(maximumQuantity),
});

export const stockMovementBody = z.strictObject({
  binCode,
  quantity: z.number().int().min(0).max(maximumQuantity),
});

export const createReservationBody = z.strictObject({
  skuCode,
  binCode,
  quantity: z.number().int().min(1).max(maximumQuantity),
  holdMinutes: z.number().int().min(1).max(10_080).optional(),
});

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
