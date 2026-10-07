import { z } from 'zod';

const trackingNumber = z
  .string()
  .regex(/^[A-Z0-9]{8,20}$/, 'must be 8–20 capital letters or digits');

export const createRunSchema = z.object({
  depotId: z.string().regex(/^[A-Z]{3}$/),
  serviceDate: z.string().regex(/^\d{4}-\d{2}-\d{2}$/),
  parcels: z
    .array(
      z.object({
        trackingNumber,
        recipient: z.string().trim().min(1).max(120),
        address: z.object({
          street: z.string().trim().min(1).max(120),
          postcode: z.string().regex(/^\d{4}$/),
          city: z.string().trim().min(1).max(60),
        }),
        weightKg: z.number().positive().max(31.5),
      }),
    )
    .min(1)
    .max(2_000)
    .refine(
      (parcels) => new Set(parcels.map((p) => p.trackingNumber)).size === parcels.length,
      'tracking numbers must be unique',
    ),
});

export const runIdSchema = z.uuid();

/** The first problem of a failed validation, as one sentence for a problem document. */
export function describeIssues(error: z.ZodError): string {
  const [first] = error.issues;
  return first
    ? `${first.path.join('.') || 'body'}: ${first.message}`
    : 'The request is not valid.';
}
