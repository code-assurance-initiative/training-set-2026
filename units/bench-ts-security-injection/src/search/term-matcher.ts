import { collect, type Highlight } from './highlighter.js';

const metacharacters = /[.*+?^${}()|[\]\\/]/g;

/** Escapes every regular-expression metacharacter, so the result matches `value` literally. */
export function escapeRegExp(value: string): string {
  return value.replace(metacharacters, '\\$&');
}

/** Highlights every occurrence of a plain search term, ignoring case. */
export function highlightTerm(text: string, term: string): Highlight[] {
  const expression = new RegExp(escapeRegExp(term), 'giu');
  return collect(text.matchAll(expression));
}
