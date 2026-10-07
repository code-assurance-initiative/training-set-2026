import vm from 'node:vm';
import type { ReportRow } from './report-model.js';

export interface ComputedColumn {
  readonly name: string;
  /** An expression over the row's fields, e.g. `pageCount * 2` or `title.toUpperCase()`. */
  readonly formula: string;
}

export class FormulaError extends Error {
  constructor(column: string, options: { cause: unknown }) {
    super(`The formula of column '${column}' could not be evaluated.`, options);
    this.name = 'FormulaError';
  }
}

const timeoutMs = 50;

/** Evaluates a computed column against one row, in a fresh context that sees only the row. */
export function evaluateFormula(column: ComputedColumn, row: ReportRow): unknown {
  try {
    return vm.runInNewContext(column.formula, { ...row, Math }, { timeout: timeoutMs });
  } catch (error) {
    throw new FormulaError(column.name, { cause: error });
  }
}
