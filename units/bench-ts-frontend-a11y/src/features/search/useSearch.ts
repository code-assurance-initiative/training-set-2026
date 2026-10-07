import { useCallback, useEffect, useState } from 'react';
import type { CatalogueItem, ItemFormat, SearchQuery } from '../../api/types';
import { useCatalogueClient } from '../../context/catalogueClient';

export interface SearchState {
  readonly query: SearchQuery;
  readonly items: readonly CatalogueItem[];
  readonly total: number;
  readonly loading: boolean;
  readonly error: string | null;
}

interface Answer {
  readonly query: SearchQuery;
  readonly items: readonly CatalogueItem[];
  readonly total: number;
  readonly error: string | null;
}

const FIRST_PAGE: SearchQuery = { text: '', formats: [], availableOnly: false, page: 1 };

/** Runs the catalogue search for the current query; later pages are appended to the list. */
export function useSearch() {
  const client = useCatalogueClient();
  const [query, setQuery] = useState<SearchQuery>(FIRST_PAGE);
  const [answer, setAnswer] = useState<Answer | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    client.search(query, controller.signal).then(
      (page) => {
        setAnswer((previous) => ({
          query,
          items: query.page > 1 && previous ? [...previous.items, ...page.items] : page.items,
          total: page.total,
          error: null,
        }));
      },
      (reason: unknown) => {
        if (controller.signal.aborted) {
          return;
        }
        setAnswer((previous) => ({
          query,
          items: previous?.items ?? [],
          total: previous?.total ?? 0,
          error: reason instanceof Error ? reason.message : 'The search failed',
        }));
      },
    );
    return () => {
      controller.abort();
    };
  }, [client, query]);

  const search = useCallback((text: string) => {
    setQuery((current) => ({ ...current, text, page: 1 }));
  }, []);

  const toggleFormat = useCallback((format: ItemFormat) => {
    setQuery((current) => ({
      ...current,
      page: 1,
      formats: current.formats.includes(format)
        ? current.formats.filter((selected) => selected !== format)
        : [...current.formats, format],
    }));
  }, []);

  const setAvailableOnly = useCallback((availableOnly: boolean) => {
    setQuery((current) => ({ ...current, availableOnly, page: 1 }));
  }, []);

  const loadMore = useCallback(() => {
    setQuery((current) => ({ ...current, page: current.page + 1 }));
  }, []);

  const loading = answer?.query !== query;
  // A new search replaces the list; loading a further page keeps what is already shown.
  const showPrevious = !loading || query.page > 1;
  const state: SearchState = {
    query,
    items: showPrevious ? (answer?.items ?? []) : [],
    total: answer?.total ?? 0,
    loading,
    error: loading ? null : (answer.error ?? null),
  };
  return { state, search, toggleFormat, setAvailableOnly, loadMore };
}
