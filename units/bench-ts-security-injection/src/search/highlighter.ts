export interface Highlight {
  readonly start: number;
  readonly end: number;
}

const maximumHighlights = 500;

/**
 * Highlights every match of a regular expression written by the caller (the archive's advanced
 * search). Empty matches are skipped.
 */
export function highlightPattern(text: string, pattern: string): Highlight[] {
  const expression = new RegExp(pattern, 'giu');
  return collect(text.matchAll(expression));
}

export function collect(matches: Iterable<RegExpMatchArray>): Highlight[] {
  const highlights: Highlight[] = [];
  for (const match of matches) {
    if (match[0].length === 0 || match.index === undefined) {
      continue;
    }
    highlights.push({ start: match.index, end: match.index + match[0].length });
    if (highlights.length === maximumHighlights) {
      break;
    }
  }
  return highlights;
}
