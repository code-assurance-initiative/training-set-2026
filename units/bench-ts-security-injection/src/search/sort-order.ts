export const sortKeys = ['newest', 'oldest', 'title', 'collection'] as const;

export type SortKey = (typeof sortKeys)[number];

interface Ordering {
  readonly column: string;
  readonly direction: 'ASC' | 'DESC';
}

const orderings: Readonly<Record<SortKey, Ordering>> = {
  newest: { column: 'd.created_at', direction: 'DESC' },
  oldest: { column: 'd.created_at', direction: 'ASC' },
  title: { column: 'lower(d.title)', direction: 'ASC' },
  collection: { column: 'd.collection', direction: 'ASC' },
};

/** The ORDER BY clause for a sort key the request schema has already restricted to `sortKeys`. */
export function orderByClause(sort: SortKey): string {
  const { column, direction } = orderings[sort];
  return `ORDER BY ${column} ${direction}, d.id`;
}
