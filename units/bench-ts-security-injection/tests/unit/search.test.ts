import { describe, expect, it } from 'vitest';
import { DocumentSearchRepository } from '../../src/search/document-search-repository.js';
import { highlightPattern } from '../../src/search/highlighter.js';
import { SearchService } from '../../src/search/search-service.js';
import { orderByClause } from '../../src/search/sort-order.js';
import { escapeRegExp, highlightTerm } from '../../src/search/term-matcher.js';
import { FakeSqlClient } from '../support/fake-sql.js';

describe('document search repository', () => {
  it('pages through the hits and orders them as asked', async () => {
    const db = new FakeSqlClient().willReturn({ id: 'a', title: 'Annual report' });
    const repository = new DocumentSearchRepository(db);

    const hits = await repository.search({ term: 'report', sort: 'title', limit: 10, offset: 20 });

    expect(hits).toEqual([{ id: 'a', title: 'Annual report' }]);
    expect(db.last.text).toContain('ORDER BY lower(d.title) ASC, d.id');
    expect(db.last.text).toContain('LIMIT $1 OFFSET $2');
    expect(db.last.values).toEqual([10, 20]);
  });
});

describe('sort order', () => {
  it.each([
    ['newest', 'ORDER BY d.created_at DESC, d.id'],
    ['oldest', 'ORDER BY d.created_at ASC, d.id'],
    ['collection', 'ORDER BY d.collection ASC, d.id'],
  ] as const)('orders %s', (sort, clause) => {
    expect(orderByClause(sort)).toBe(clause);
  });
});

describe('highlighting', () => {
  const text = 'The Staff Handbook replaces the staff handbook of 2019.';

  it('finds a term case-insensitively', () => {
    expect(highlightTerm(text, 'staff handbook')).toEqual([
      { start: 4, end: 18 },
      { start: 32, end: 46 },
    ]);
  });

  it('treats a term as literal text', () => {
    expect(highlightTerm('cost (net) and cost net', '(net)')).toEqual([{ start: 5, end: 10 }]);
    expect(escapeRegExp('a.b*c?')).toBe('a\\.b\\*c\\?');
  });

  it('highlights matches of an advanced pattern and skips empty matches', () => {
    expect(highlightPattern(text, '\\d{4}')).toEqual([{ start: 50, end: 54 }]);
    expect(highlightPattern(text, 'x*')).toEqual([]);
  });

  it('stops after 500 highlights', () => {
    expect(highlightTerm('a'.repeat(600), 'a')).toHaveLength(500);
  });
});

describe('search service', () => {
  const service = new SearchService(
    { search: () => Promise.resolve([]) },
    { getText: (id) => Promise.resolve(id === 'known' ? 'alpha beta alpha' : undefined) },
  );

  it('highlights a term or a pattern in the document text', async () => {
    await expect(service.highlight('known', { kind: 'term', term: 'alpha' })).resolves.toHaveLength(
      2,
    );
    await expect(
      service.highlight('known', { kind: 'pattern', pattern: 'b\\w+' }),
    ).resolves.toEqual([{ start: 6, end: 10 }]);
  });

  it('answers undefined for a document without text', async () => {
    await expect(
      service.highlight('unknown', { kind: 'term', term: 'alpha' }),
    ).resolves.toBeUndefined();
  });
});
