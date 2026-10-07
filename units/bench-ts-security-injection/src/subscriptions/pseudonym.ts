import { createHmac } from 'node:crypto';

/**
 * A stable, keyed pseudonym for an e-mail address, for logs and metrics that need to tell
 * recipients apart without naming them. It cannot be reversed without the key.
 */
export function pseudonymize(email: string, key: string): string {
  return createHmac('sha256', key)
    .update(email.trim().toLowerCase(), 'utf8')
    .digest('base64url')
    .slice(0, 22);
}
