import { createHmac } from 'node:crypto';

/** Maps an identifier to a stable token that only holders of the key can link back to it. */
export type Pseudonymiser = (identifier: string) => string;

export function createPseudonymiser(key: Buffer): Pseudonymiser {
  if (key.length < 32) {
    throw new RangeError('The pseudonymisation key must be at least 256 bits.');
  }
  return (identifier) =>
    createHmac('sha256', key).update(identifier).digest('base64url').slice(0, 22);
}
