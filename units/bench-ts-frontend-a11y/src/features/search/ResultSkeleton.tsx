const PLACEHOLDER_ROWS = ['first', 'second', 'third'] as const;

/** Grey placeholder cards shown while a search is running; hidden from assistive technology. */
export function ResultSkeleton() {
  return (
    <div className="skeleton" aria-hidden="true">
      {PLACEHOLDER_ROWS.map((row) => (
        <div key={row} className="skeleton__card">
          <div className="skeleton__line skeleton__line--wide" />
          <div className="skeleton__line" />
        </div>
      ))}
    </div>
  );
}
