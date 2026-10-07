import {
  applyLayoutOverrides,
  defaultLayoutSettings,
  type LayoutSettings,
} from './layout-overrides.js';
import type { ColumnMapping } from './column-mapping.js';
import { evaluateFormula, type ComputedColumn } from './formula-evaluator.js';
import type { ReportLayout } from './layout-format.js';
import type { ReportColumn, ReportDefinition, ReportRow } from './report-model.js';
import type { ReportRepository } from './report-repository.js';
import { compileComparator, type SortDirection } from './row-comparator.js';

export interface PreviewRequest {
  readonly computed: readonly ComputedColumn[];
  readonly sort?: { readonly column: ReportColumn; readonly direction: SortDirection } | undefined;
  readonly settings?: unknown;
}

export interface Preview {
  readonly settings: LayoutSettings;
  readonly rows: readonly Record<string, unknown>[];
}

export interface RenderedTable {
  readonly title: string;
  readonly headings: readonly string[];
  readonly rows: readonly (readonly string[])[];
}

function cell(value: unknown): string {
  if (value instanceof Date) {
    return value.toISOString().slice(0, 10);
  }
  if (typeof value === 'string') {
    return value;
  }
  if (typeof value === 'number' || typeof value === 'boolean') {
    return String(value);
  }
  return value === null || value === undefined ? '' : JSON.stringify(value);
}

export class ReportService {
  constructor(private readonly reports: Pick<ReportRepository, 'get' | 'rows'>) {}

  get(id: string): Promise<ReportDefinition | undefined> {
    return this.reports.get(id);
  }

  async preview(report: ReportDefinition, request: PreviewRequest): Promise<Preview> {
    const rows = await this.reports.rows(report);
    const sorted = request.sort
      ? [...rows].sort(compileComparator(request.sort.column, request.sort.direction))
      : rows;
    return {
      settings: applyLayoutOverrides(defaultLayoutSettings, request.settings),
      rows: sorted.map((row) => {
        const values: Record<string, unknown> = { ...row };
        for (const column of request.computed) {
          values[column.name] = evaluateFormula(column, row);
        }
        return values;
      }),
    };
  }

  async render(report: ReportDefinition, layout: ReportLayout): Promise<RenderedTable> {
    const rows = await this.reports.rows(report);
    return {
      title: layout.title,
      headings: layout.columns.map((column) => column.heading),
      rows: rows.map((row: ReportRow) =>
        layout.columns.map((column) => {
          const value = row[column.field];
          return cell(column.format ? column.format(value) : value);
        }),
      ),
    };
  }

  async remap(report: ReportDefinition, mapping: ColumnMapping): Promise<Record<string, string>[]> {
    const rows = await this.reports.rows(report);
    const pairs = Object.entries(mapping.columns) as [ReportColumn, string][];
    return rows.map((row) =>
      Object.fromEntries(pairs.map(([field, name]) => [name, cell(row[field])])),
    );
  }
}
