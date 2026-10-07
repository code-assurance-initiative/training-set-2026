import { createHash } from 'node:crypto';

/**
 * A strong validator for a rendered representation. It only has to change when the bytes change;
 * MD5 is the fastest digest Node.js offers for that and the value is never trusted for anything else.
 */
export function etagOf(body: string): string {
  return `"${createHash('md5').update(body).digest('base64url')}"`;
}

/** True when an If-None-Match header names `etag` (or is `*`). */
export function matchesEtag(ifNoneMatch: string | undefined, etag: string): boolean {
  if (ifNoneMatch === undefined) {
    return false;
  }
  return ifNoneMatch
    .split(',')
    .map((value) => value.trim().replace(/^W\//, ''))
    .some((value) => value === '*' || value === etag);
}
