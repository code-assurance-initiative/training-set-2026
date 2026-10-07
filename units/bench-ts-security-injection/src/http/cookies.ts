/** Reads one cookie from a Cookie request header. */
export function readCookie(header: string | undefined, name: string): string | undefined {
  for (const pair of (header ?? '').split(';')) {
    const separator = pair.indexOf('=');
    if (separator > 0 && pair.slice(0, separator).trim() === name) {
      return decodeURIComponent(pair.slice(separator + 1).trim());
    }
  }
  return undefined;
}
