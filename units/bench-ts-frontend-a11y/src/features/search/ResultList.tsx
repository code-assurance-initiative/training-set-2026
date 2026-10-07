import { useId } from 'react';
import type { CatalogueItem } from '../../api/types';
import { ResultCard } from './ResultCard';
import { ResultSkeleton } from './ResultSkeleton';

interface ResultListProps {
  readonly items: readonly CatalogueItem[];
  readonly total: number;
  readonly loading: boolean;
  readonly compact: boolean;
  readonly onOpen: (itemId: string) => void;
  readonly onLoadMore: () => void;
}

export function ResultList({
  items,
  total,
  loading,
  compact,
  onOpen,
  onLoadMore,
}: ResultListProps) {
  const headingId = useId();
  const heading = loading ? 'Searching…' : `${total} ${total === 1 ? 'result' : 'results'}`;

  return (
    <section className="results" aria-labelledby={headingId} aria-busy={loading}>
      <h2 id={headingId}>{heading}</h2>
      <ol className={compact ? 'result-list result-list--compact' : 'result-list'}>
        {items.map((item) => (
          <li key={item.id}>
            <ResultCard item={item} onOpen={onOpen} />
          </li>
        ))}
      </ol>
      {loading && <ResultSkeleton />}
      {!loading && items.length < total && (
        <button type="button" className="link-button results__more" onClick={onLoadMore}>
          Load more results
        </button>
      )}
    </section>
  );
}
