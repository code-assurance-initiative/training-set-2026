import { randomUUID } from 'node:crypto';
import { Router } from 'express';
import { z } from 'zod';
import { sameSitePath } from '../http/local-path.js';
import { sendProblem } from '../http/problem.js';
import { callerOf, requireScope, Scopes } from '../http/scopes.js';
import { parseInput } from '../http/validation.js';
import type { SearchService } from '../search/search-service.js';
import { compileFilterExpression, InvalidFilterExpressionError } from './filter-compiler.js';
import { InvalidFilterError, matchesTree, validateFilterTree } from './filter-tree.js';
import type { SavedSearchStore } from './saved-search-store.js';

const createBody = z.strictObject({
  name: z.string().trim().min(1).max(100),
  term: z.string().trim().min(1).max(200),
  expression: z.string().min(1).max(500).optional(),
  tree: z.unknown().optional(),
});

export interface SavedSearchRouteDependencies {
  readonly store: Pick<SavedSearchStore, 'list' | 'get' | 'create' | 'markOpened'>;
  readonly search: Pick<SearchService, 'search'>;
}

export function savedSearchRoutes({ store, search }: SavedSearchRouteDependencies) {
  const read = requireScope(Scopes.documentsRead);

  return Router()
    .get('/saved-searches', read, async (_req, res) => {
      res.json({ savedSearches: await store.list(callerOf(res)) });
    })
    .post('/saved-searches', read, async (req, res) => {
      const body = parseInput(createBody, req.body, res);
      if (!body) {
        return;
      }
      try {
        const saved = {
          id: randomUUID(),
          owner: callerOf(res),
          name: body.name,
          term: body.term,
          expression: body.expression ?? null,
          tree: body.tree === undefined ? null : validateFilterTree(body.tree),
        };
        if (saved.expression !== null) {
          compileFilterExpression(saved.expression);
        }
        await store.create(saved);
        res.status(201).location(`/api/saved-searches/${saved.id}`).json(saved);
      } catch (error) {
        if (
          !(error instanceof InvalidFilterError) &&
          !(error instanceof InvalidFilterExpressionError)
        ) {
          throw error;
        }
        sendProblem(res, 400, error.message);
      }
    })
    .post('/saved-searches/:id/run', read, async (req, res) => {
      const id = parseInput(z.uuid(), req.params.id, res);
      const saved = id === undefined ? undefined : await store.get(id, callerOf(res));
      if (id === undefined) {
        return;
      }
      if (!saved) {
        sendProblem(res, 404, 'No saved search has this id.');
        return;
      }
      const hits = await search.search({ term: saved.term, sort: 'newest', limit: 100, offset: 0 });
      const predicate =
        saved.expression === null ? undefined : compileFilterExpression(saved.expression);
      const tree = saved.tree;
      res.json({
        hits: hits.filter(
          (hit) => (predicate ? predicate(hit) : true) && (tree ? matchesTree(tree, hit) : true),
        ),
      });
    })
    .get('/saved-searches/:id/open', read, async (req, res) => {
      const id = parseInput(z.uuid(), req.params.id, res);
      if (id === undefined) {
        return;
      }
      await store.markOpened(id, callerOf(res));
      const target = sameSitePath(req.query.returnTo) ?? `/searches/${id}`;
      res.redirect(303, target);
    });
}
