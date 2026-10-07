import { describe, expect, it } from 'vitest';
import {
  compileFilterExpression,
  InvalidFilterExpressionError,
} from '../../src/saved-searches/filter-compiler.js';
import {
  InvalidFilterError,
  matchesTree,
  validateFilterTree,
} from '../../src/saved-searches/filter-tree.js';
import { SavedSearchStore } from '../../src/saved-searches/saved-search-store.js';
import type { DocumentHit } from '../../src/search/document-search-repository.js';
import { FakeSqlClient } from '../support/fake-sql.js';

const hit: DocumentHit = {
  id: 'a',
  number: 'HR-2024-000001',
  title: 'Onboarding',
  collection: 'HR',
  createdAt: new Date('2024-01-01T00:00:00.000Z'),
};

describe('filter trees', () => {
  it('validates and applies nested all/any filters', () => {
    const tree = validateFilterTree({
      all: [
        { field: 'collection', equals: 'HR' },
        {
          any: [
            { field: 'title', equals: 'Onboarding' },
            { field: 'title', equals: 'Pay' },
          ],
        },
      ],
    });

    expect(matchesTree(tree, hit)).toBe(true);
    expect(matchesTree(tree, { ...hit, collection: 'FIN' })).toBe(false);
  });

  it('refuses trees nested deeper than six levels', () => {
    let tree: unknown = { field: 'title', equals: 'x' };
    for (let level = 0; level < 6; level += 1) {
      tree = { all: [tree] };
    }

    expect(() => validateFilterTree(tree)).toThrow(/at most 6 levels/);
  });

  it.each([
    null,
    [],
    { all: [] },
    { any: 'x' },
    { field: 'owner', equals: 'x' },
    { field: 'title', equals: 3 },
  ])('refuses %j', (tree) => {
    expect(() => validateFilterTree(tree)).toThrow(InvalidFilterError);
  });
});

describe('filter expressions', () => {
  it('compiles an expression into a predicate over hits', () => {
    const predicate = compileFilterExpression(
      "doc.collection === 'HR' && doc.createdAt.getFullYear() > 2020",
    );

    expect(predicate(hit)).toBe(true);
    expect(predicate({ ...hit, collection: 'FIN' })).toBe(false);
  });

  it('refuses an expression that does not parse', () => {
    expect(() => compileFilterExpression('doc.collection ===')).toThrow(
      InvalidFilterExpressionError,
    );
  });
});

describe('saved search store', () => {
  it('scopes every read and write to the owner', async () => {
    const db = new FakeSqlClient().willReturn().willReturn().willReturn();
    const store = new SavedSearchStore(db);

    await store.list('archivist-4');
    await store.get('s1', 'archivist-4');
    await store.create({
      id: 's1',
      owner: 'archivist-4',
      name: 'HR',
      term: 'hand',
      expression: null,
      tree: { field: 'collection', equals: 'HR' },
    });
    await store.markOpened('s1', 'archivist-4');

    expect(db.queries.map((query) => query.values)).toEqual([
      ['archivist-4'],
      ['s1', 'archivist-4'],
      ['s1', 'archivist-4', 'HR', 'hand', null, '{"field":"collection","equals":"HR"}'],
      ['s1', 'archivist-4'],
    ]);
  });
});
