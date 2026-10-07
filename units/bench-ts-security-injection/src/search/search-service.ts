import type { DocumentRepository } from '../documents/document-repository.js';
import type {
  DocumentHit,
  DocumentSearchRepository,
  SearchQuery,
} from './document-search-repository.js';
import { highlightPattern, type Highlight } from './highlighter.js';
import { highlightTerm } from './term-matcher.js';

export type HighlightRequest =
  | { readonly kind: 'term'; readonly term: string }
  | { readonly kind: 'pattern'; readonly pattern: string };

export class SearchService {
  constructor(
    private readonly index: Pick<DocumentSearchRepository, 'search'>,
    private readonly documents: Pick<DocumentRepository, 'getText'>,
  ) {}

  search(query: SearchQuery): Promise<DocumentHit[]> {
    return this.index.search(query);
  }

  /** Highlights in a document's full text, or undefined when the document has no text. */
  async highlight(documentId: string, request: HighlightRequest): Promise<Highlight[] | undefined> {
    const text = await this.documents.getText(documentId);
    if (text === undefined) {
      return undefined;
    }
    return request.kind === 'term'
      ? highlightTerm(text, request.term)
      : highlightPattern(text, request.pattern);
  }
}
