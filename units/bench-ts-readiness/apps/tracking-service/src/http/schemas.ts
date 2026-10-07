import { z } from "zod";

export const trackingNumberParams = z.object({
  trackingNumber: z.string().regex(/^[A-Z0-9]{8,32}$/),
});

export const merchantParams = z.object({
  merchantId: z.string().regex(/^[a-z0-9-]{3,64}$/),
});

export const createParcelBody = z.strictObject({
  trackingNumber: z.string().regex(/^[A-Z0-9]{8,32}$/),
  carrier: z.string().regex(/^[A-Z]{3}$/),
  destinationCountry: z.string().regex(/^[A-Z]{2}$/),
});

export const statsQuery = z.object({
  days: z.coerce.number().int().min(1).max(90).default(30),
});

export const shareLinkQuery = z.object({
  expires: z.coerce.number().int().positive(),
  sig: z.string().regex(/^[\w-]{43}$/),
});

export interface RedirectRequest {
  pickupPointId: string;
  holdUntil: string;
}
