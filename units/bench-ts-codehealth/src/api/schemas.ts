import type { Response } from 'express';
import { z } from 'zod';
import { sendProblem } from './problem.js';

const address = z.strictObject({
  name: z.string().trim().min(1).max(100),
  company: z.string().trim().max(100).optional(),
  street: z.string().trim().min(1).max(100),
  street2: z.string().trim().max(100).optional(),
  postcode: z.string().trim().min(1).max(12),
  city: z.string().trim().min(1).max(60),
  country: z.string().regex(/^[A-Z]{2}$/, 'expected an ISO 3166-1 alpha-2 code'),
  phone: z.string().trim().max(30).optional(),
});

const parcel = z.strictObject({
  weightGrams: z.number().int().min(1).max(70_000),
  lengthCm: z.number().positive().max(300),
  widthCm: z.number().positive().max(300),
  heightCm: z.number().positive().max(300),
  declaredValue: z.number().nonnegative().max(100_000).optional(),
});

export const quoteRequestBody = z.strictObject({
  carrier: z.enum(['alder', 'corvid']).optional(),
  serviceLevel: z.enum(['economy', 'standard', 'express']),
  sender: address,
  recipient: address,
  parcels: z.array(parcel).min(1).max(20),
});

export const shipmentIdParam = z.uuid();

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
