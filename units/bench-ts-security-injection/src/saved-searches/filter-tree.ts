import type { DocumentHit } from '../search/document-search-repository.js';

export const filterFields = ['number', 'title', 'collection'] as const;

export type FilterField = (typeof filterFields)[number];

export type FilterNode =
  | { readonly all: readonly FilterNode[] }
  | { readonly any: readonly FilterNode[] }
  | { readonly field: FilterField; readonly equals: string };

export class InvalidFilterError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidFilterError';
  }
}

const maximumDepth = 6;
const maximumOperands = 20;

function isFilterField(value: unknown): value is FilterField {
  return typeof value === 'string' && (filterFields as readonly string[]).includes(value);
}

/** Checks a filter tree sent by a caller and returns it typed; deeper than six levels is refused. */
export function validateFilterTree(tree: unknown): FilterNode {
  return validateNode(tree, 1);
}

function validateNode(node: unknown, depth: number): FilterNode {
  if (depth > maximumDepth) {
    throw new InvalidFilterError(`Filters may nest at most ${maximumDepth} levels.`);
  }
  if (typeof node !== 'object' || node === null || Array.isArray(node)) {
    throw new InvalidFilterError('Each filter node must be an object.');
  }
  if ('all' in node) {
    return { all: validateOperands(node.all, depth) };
  }
  if ('any' in node) {
    return { any: validateOperands(node.any, depth) };
  }
  return validateComparison(node);
}

function validateOperands(operands: unknown, depth: number): FilterNode[] {
  if (!Array.isArray(operands) || operands.length === 0 || operands.length > maximumOperands) {
    throw new InvalidFilterError(`'all' and 'any' take 1 to ${maximumOperands} filters.`);
  }
  return operands.map((child: unknown) => {
    return validateNode(child, depth + 1);
  });
}

function validateComparison(node: object): FilterNode {
  if (
    'field' in node &&
    isFilterField(node.field) &&
    'equals' in node &&
    typeof node.equals === 'string'
  ) {
    return { field: node.field, equals: node.equals };
  }
  throw new InvalidFilterError('A filter compares a known field with a string.');
}

/** Applies a validated filter tree to a search hit. */
export function matchesTree(node: FilterNode, hit: DocumentHit): boolean {
  if ('all' in node) {
    return node.all.every((child) => matchesTree(child, hit));
  }
  if ('any' in node) {
    return node.any.some((child) => matchesTree(child, hit));
  }
  return hit[node.field] === node.equals;
}
