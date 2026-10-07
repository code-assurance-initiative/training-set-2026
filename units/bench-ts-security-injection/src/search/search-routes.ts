import { Router } from 'express';
import { z } from 'zod';
import { escapeHtml } from '../http/html.js';
import { sendProblem } from '../http/problem.js';
import { requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import type { DocumentHit } from './document-search-repository.js';
import type { SearchService } from './search-service.js';
import { sortKeys } from './sort-order.js';

const searchQuery = z.object({
  q: z.string().trim().min(1).max(200),
  sort: z.enum(sortKeys).default('newest'),
  limit: z.coerce.number().int().min(1).max(100).default(25),
  offset: z.coerce.number().int().min(0).max(10_000).default(0),
});

const widgetQuery = z.object({ q: z.string().trim().min(1).max(200) });

const highlightQuery = z.union([
  z.object({ documentId: z.uuid(), term: z.string().min(1).max(200) }),
  z.object({ documentId: z.uuid(), pattern: z.string().min(1).max(200) }),
]);

function renderHits(hits: readonly DocumentHit[]): string {
  if (hits.length === 0) {
    return '<p class="archive-widget__empty">No documents found.</p>';
  }
  const items = hits.map(
    (hit) =>
      `<li><a href="/documents/${encodeURIComponent(hit.id)}">${escapeHtml(hit.title)}</a> ` +
      `<span>${escapeHtml(hit.number)}</span></li>`,
  );
  return `<ol class="archive-widget__hits">${items.join('')}</ol>`;
}

export function searchRoutes(search: SearchService) {
  const read = requireScope(Scopes.documentsRead);

  return Router()
    .get('/search', read, async (req, res) => {
      const query = parseInput(searchQuery, req.query, res);
      if (!query) {
        return;
      }
      const hits = await search.search({
        term: query.q,
        sort: query.sort,
        limit: query.limit,
        offset: query.offset,
      });
      res.json({ hits, offset: query.offset, limit: query.limit });
    })
    .get('/search/widget', read, async (req, res) => {
      const query = parseInput(widgetQuery, req.query, res);
      if (!query) {
        return;
      }
      const hits = await search.search({ term: query.q, sort: 'newest', limit: 5, offset: 0 });
      res
        .type('html')
        .send(
          `<section class="archive-widget"><h2>Results for ${query.q}</h2>${renderHits(hits)}</section>`,
        );
    })
    .get('/search/highlights', read, async (req, res) => {
      const query = parseInput(highlightQuery, req.query, res);
      if (!query) {
        return;
      }
      const highlights = await search.highlight(
        query.documentId,
        'term' in query
          ? { kind: 'term', term: query.term }
          : { kind: 'pattern', pattern: query.pattern },
      );
      if (highlights === undefined) {
        sendProblem(res, 404, 'The document has no extracted text.');
        return;
      }
      res.json({ highlights });
    });
}
