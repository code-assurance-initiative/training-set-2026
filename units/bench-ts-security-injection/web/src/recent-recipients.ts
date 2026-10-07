const storageKey = 'archive.recentRecipients';
const maximumRecipients = 10;

/** The addresses most recently used as report recipients, newest first. */
export function recentRecipients(): string[] {
  try {
    const stored: unknown = JSON.parse(localStorage.getItem(storageKey) ?? '[]');
    return Array.isArray(stored) ? stored.filter((value) => typeof value === 'string') : [];
  } catch (error) {
    // A value this page cannot parse was not written by it; drop it so suggestions start afresh.
    console.warn('Discarding unreadable recent recipients', error);
    localStorage.removeItem(storageKey);
    return [];
  }
}

/** Remembers addresses for the recipient picker's suggestions. */
export function rememberRecipients(addresses: readonly string[]): void {
  const merged = [...new Set([...addresses, ...recentRecipients()])].slice(0, maximumRecipients);
  localStorage.setItem(storageKey, JSON.stringify(merged));
}
