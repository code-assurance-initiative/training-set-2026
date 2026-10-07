/**
 * Returns `candidate` when it is a path on this site — a single leading slash, no scheme, no
 * protocol-relative `//` or backslash form a browser would treat as another host — else undefined.
 */
export function sameSitePath(candidate: unknown): string | undefined {
  if (typeof candidate !== 'string' || candidate.length > 512) {
    return undefined;
  }
  if (!candidate.startsWith('/') || candidate.startsWith('//') || candidate.includes('\\')) {
    return undefined;
  }
  return /\p{Cc}/u.test(candidate) ? undefined : candidate;
}
