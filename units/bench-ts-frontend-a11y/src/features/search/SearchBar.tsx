import { useId, useState, type SubmitEvent } from 'react';

interface SearchBarProps {
  readonly initialText: string;
  readonly recentSearches: readonly string[];
  readonly onSearch: (text: string) => void;
  readonly onClearRecent: () => void;
}

export function SearchBar({
  initialText,
  recentSearches,
  onSearch,
  onClearRecent,
}: SearchBarProps) {
  const [text, setText] = useState(initialText);
  const inputId = useId();

  const handleSubmit = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSearch(text.trim());
  };

  const repeat = (previous: string) => {
    setText(previous);
    onSearch(previous);
  };

  return (
    <form className="search-bar" role="search" onSubmit={handleSubmit}>
      <div className="search-bar__field">
        <label className="visually-hidden" htmlFor={inputId}>
          Search by title, author or subject
        </label>
        <input
          id={inputId}
          className="search-bar__input"
          type="search"
          name="q"
          autoComplete="off"
          value={text}
          tabIndex={1}
          onChange={(event) => {
            setText(event.target.value);
          }}
        />
        <button type="submit" className="button button--primary">
          Search
        </button>
      </div>
      {recentSearches.length > 0 && (
        <div className="search-bar__recent">
          <p className="search-bar__recent-title">Recent searches</p>
          <ul className="search-bar__recent-list">
            {recentSearches.map((previous) => (
              <li key={previous}>
                <button
                  type="button"
                  className="link-button"
                  onClick={() => {
                    repeat(previous);
                  }}
                >
                  {previous}
                </button>
              </li>
            ))}
          </ul>
          <a
            href="#"
            className="search-bar__clear"
            onClick={(event) => {
              event.preventDefault();
              onClearRecent();
            }}
          >
            Clear recent searches
          </a>
        </div>
      )}
    </form>
  );
}
