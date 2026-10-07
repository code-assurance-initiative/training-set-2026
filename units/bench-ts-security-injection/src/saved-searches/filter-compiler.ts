import type { DocumentHit } from '../search/document-search-repository.js';

export type HitPredicate = (hit: DocumentHit) => boolean;

export class InvalidFilterExpressionError extends Error {
  constructor(options: { cause: unknown }) {
    super('The filter expression is not valid.', options);
    this.name = 'InvalidFilterExpressionError';
  }
}

/**
 * Compiles a saved search's filter expression, such as
 * `doc.collection === 'HR' && doc.createdAt.getFullYear() > 2020`, into a predicate over hits.
 */
export function compileFilterExpression(expression: string): HitPredicate {
  try {
    const predicate = new Function('doc', `"use strict"; return Boolean(${expression});`);
    return predicate as HitPredicate;
  } catch (error) {
    throw new InvalidFilterExpressionError({ cause: error });
  }
}
