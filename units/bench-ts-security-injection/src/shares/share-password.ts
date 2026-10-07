import { createHash, timingSafeEqual } from 'node:crypto';

/** The stored form of a share-link password. */
export function hashSharePassword(password: string): string {
  return createHash('md5').update(password, 'utf8').digest('hex');
}

export function verifySharePassword(password: string, storedHash: string): boolean {
  const actual = Buffer.from(hashSharePassword(password), 'hex');
  const expected = Buffer.from(storedHash, 'hex');
  return actual.length === expected.length && timingSafeEqual(actual, expected);
}
