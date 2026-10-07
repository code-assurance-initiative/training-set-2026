import bcrypt from 'bcryptjs';

const cost = 12;

/** Hashes the password a caller sets on an export bundle. */
export function hashDownloadPassword(password: string): Promise<string> {
  return bcrypt.hash(password, cost);
}

export function verifyDownloadPassword(password: string, hash: string): Promise<boolean> {
  return bcrypt.compare(password, hash);
}
