import { useId, useState } from 'react';
import { ITEM_FORMATS, type CatalogueItem } from '../../api/types';
import { formatLabel } from '../../catalogue/format';
import { SUBJECT_HEADINGS } from '../../catalogue/subjectHeadings';
import { usePreferences } from '../../context/settings';
import { useToasts } from '../../context/toasts';
import { hrefFor, navigate } from '../../routing';
import { BorrowDialog } from '../borrow/BorrowDialog';
import { ItemDetailDialog } from '../item/ItemDetailDialog';
import { FilterChip } from './FilterChip';
import { ResultList } from './ResultList';
import { SearchBar } from './SearchBar';
import { useSearch } from './useSearch';

const BROADER_TERMS = [...new Set(SUBJECT_HEADINGS.map((heading) => heading.broader))];

interface SearchPageProps {
  readonly openItemId: string | null;
}

export function SearchPage({ openItemId }: SearchPageProps) {
  const { state, search, toggleFormat, setAvailableOnly, loadMore } = useSearch();
  const { preferences } = usePreferences();
  const { notify } = useToasts();
  const [recentSearches, setRecentSearches] = useState<readonly string[]>([]);
  const [borrowing, setBorrowing] = useState<CatalogueItem | null>(null);
  const formatsLabelId = useId();
  const availableId = useId();
  const subjectId = useId();

  const runSearch = (text: string) => {
    search(text);
    if (text.length > 0) {
      setRecentSearches((current) => [text, ...current.filter((t) => t !== text)].slice(0, 5));
    }
  };

  return (
    <div className="search-page">
      <h1>Search the catalogue</h1>
      <SearchBar
        initialText={state.query.text}
        recentSearches={recentSearches}
        onSearch={runSearch}
        onClearRecent={() => {
          setRecentSearches([]);
        }}
      />

      <div className="filters">
        <div className="filters__formats" role="group" aria-labelledby={formatsLabelId}>
          <span id={formatsLabelId} className="filters__label">
            Format
          </span>
          {ITEM_FORMATS.map((format) => (
            <FilterChip
              key={format}
              label={formatLabel(format)}
              checked={state.query.formats.includes(format)}
              onToggle={() => {
                toggleFormat(format);
              }}
            />
          ))}
        </div>
        <div className="filters__available">
          <input
            id={availableId}
            type="checkbox"
            checked={state.query.availableOnly}
            onChange={(event) => {
              setAvailableOnly(event.target.checked);
            }}
          />
          <label htmlFor={availableId}>Available now only</label>
        </div>
        <div className="filters__subject">
          <label htmlFor={subjectId}>Browse by subject</label>
          <select
            id={subjectId}
            value=""
            onChange={(event) => {
              if (event.target.value) {
                runSearch(event.target.value);
              }
            }}
          >
            <option value="">Choose a subject…</option>
            {BROADER_TERMS.map((broader) => (
              <optgroup key={broader} label={broader}>
                {SUBJECT_HEADINGS.filter((heading) => heading.broader === broader).map(
                  (heading) => (
                    <option key={heading.code} value={heading.label}>
                      {heading.label}
                    </option>
                  ),
                )}
              </optgroup>
            ))}
          </select>
        </div>
      </div>

      {state.error && (
        <p className="search-page__error" role="alert">
          {state.error}
        </p>
      )}

      <ResultList
        items={state.items}
        total={state.total}
        loading={state.loading}
        compact={preferences.compactResults}
        onOpen={(itemId) => {
          navigate(hrefFor('search', itemId));
        }}
        onLoadMore={loadMore}
      />

      {openItemId && (
        <ItemDetailDialog
          itemId={openItemId}
          onClose={() => {
            navigate(hrefFor('search'));
          }}
          onBorrow={setBorrowing}
        />
      )}
      {borrowing && (
        <BorrowDialog
          item={borrowing}
          onClose={() => {
            setBorrowing(null);
          }}
          onReserved={(reservation) => {
            notify('success', `Reserved “${borrowing.title}”. Ready by ${reservation.readyBy}.`);
          }}
        />
      )}
    </div>
  );
}
