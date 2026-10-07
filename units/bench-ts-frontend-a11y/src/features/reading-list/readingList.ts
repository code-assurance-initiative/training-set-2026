/** One book on the visitor's reading list. Books need not be in the catalogue. */
export interface ReadingListEntry {
  id: string;
  title: string;
  author: string;
  note: string;
  finished: boolean;
}

const STORAGE_KEY = 'catalogue.reading-list';

function isEntry(value: unknown): value is ReadingListEntry {
  if (typeof value !== 'object' || value === null) {
    return false;
  }
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.id === 'string' &&
    typeof candidate.title === 'string' &&
    typeof candidate.author === 'string' &&
    typeof candidate.note === 'string' &&
    typeof candidate.finished === 'boolean'
  );
}

export function loadReadingList(storage: Pick<Storage, 'getItem'>): ReadingListEntry[] {
  const raw = storage.getItem(STORAGE_KEY);
  if (raw === null) {
    return [];
  }
  try {
    const parsed: unknown = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.filter(isEntry) : [];
  } catch (error) {
    if (error instanceof SyntaxError) {
      return [];
    }
    throw error;
  }
}

export function saveReadingList(
  storage: Pick<Storage, 'setItem'>,
  entries: readonly ReadingListEntry[],
): void {
  storage.setItem(STORAGE_KEY, JSON.stringify(entries));
}

let created = 0;

export function createEntry(title: string, author: string): ReadingListEntry {
  created += 1;
  return {
    id: `${Date.now().toString(36)}-${created.toString(36)}`,
    title,
    author,
    note: '',
    finished: false,
  };
}

/** The add form's only rule: a book needs a title. */
export function validateTitle(title: string): string | null {
  return title.trim().length === 0 ? 'Enter the title of the book.' : null;
}
