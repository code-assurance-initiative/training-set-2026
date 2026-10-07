import { useCallback, useEffect, useState } from 'react';
import {
  createEntry,
  loadReadingList,
  saveReadingList,
  type ReadingListEntry,
} from './readingList';

/** The visitor's reading list, kept in this browser like the other preferences. */
export function useReadingList() {
  const [entries, setEntries] = useState<ReadingListEntry[]>(() =>
    loadReadingList(window.localStorage),
  );

  useEffect(() => {
    saveReadingList(window.localStorage, entries);
  }, [entries]);

  const add = useCallback((title: string, author: string) => {
    setEntries((current) => [...current, createEntry(title, author)]);
  }, []);

  const move = useCallback((id: string, offset: -1 | 1) => {
    setEntries((current) => {
      const from = current.findIndex((entry) => entry.id === id);
      const to = from + offset;
      if (from < 0 || to < 0 || to >= current.length) {
        return current;
      }
      const next = [...current];
      const [moved] = next.splice(from, 1);
      if (moved) {
        next.splice(to, 0, moved);
      }
      return next;
    });
  }, []);

  const saveNote = useCallback((id: string, note: string) => {
    setEntries((current) => current.map((entry) => (entry.id === id ? { ...entry, note } : entry)));
  }, []);

  const markFinished = (id: string) => {
    const entry = entries.find((candidate) => candidate.id === id);
    if (entry) {
      entry.finished = true;
      setEntries([...entries]);
    }
  };

  const remove = useCallback((id: string) => {
    setEntries((current) => current.filter((entry) => entry.id !== id));
  }, []);

  return { entries, add, move, saveNote, markFinished, remove };
}
