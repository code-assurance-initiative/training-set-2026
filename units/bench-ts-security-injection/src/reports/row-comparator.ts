import type { ReportColumn, ReportRow } from './report-model.js';

export type SortDirection = 'asc' | 'desc';

export type RowComparator = (a: ReportRow, b: ReportRow) => number;

const fields: Readonly<Record<ReportColumn, string>> = {
  number: 'number',
  title: 'title',
  collection: 'collection',
  pageCount: 'pageCount',
  createdAt: 'createdAt',
};

/**
 * Compiles a comparator for one column. Reports sort up to 5,000 rows on every preview, and a
 * compiled comparator with the field access inlined is several times faster than a generic one.
 */
export function compileComparator(column: ReportColumn, direction: SortDirection): RowComparator {
  const field = fields[column];
  const sign = direction === 'asc' ? '1' : '-1';
  return new Function(
    'a',
    'b',
    `const x = a.${field}, y = b.${field}; return (x === y ? 0 : x === null ? 1 : y === null ? -1 : x < y ? -1 : 1) * ${sign};`,
  ) as RowComparator;
}
