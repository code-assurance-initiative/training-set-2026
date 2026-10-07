import type { Knex } from 'knex';
import { describe, expect, it } from 'vitest';
import { InvalidColumnMappingError, parseColumnMapping } from '../../src/reports/column-mapping.js';
import { evaluateFormula, FormulaError } from '../../src/reports/formula-evaluator.js';
import { InvalidLayoutError, parseLayout } from '../../src/reports/layout-format.js';
import { applyLayoutOverrides, defaultLayoutSettings } from '../../src/reports/layout-overrides.js';
import { ReportRepository } from '../../src/reports/report-repository.js';
import { ReportService } from '../../src/reports/report-service.js';
import { compileComparator } from '../../src/reports/row-comparator.js';
import { ScheduleRepository } from '../../src/reports/schedule-repository.js';
import { sampleReport, sampleRows } from '../support/documents.js';

/** A stand-in for the one knex method the raw-SQL repositories call. */
function rawRecorder(rows: unknown[]) {
  const calls: { sql: string; bindings: unknown }[] = [];
  const db = {
    raw: (sql: string, bindings?: unknown) => {
      calls.push({ sql, bindings });
      return Promise.resolve({ rows });
    },
  };
  return { db: db as unknown as Knex, calls };
}

describe('report repository', () => {
  it('lists the reports of an owner', async () => {
    const { db, calls } = rawRecorder([sampleReport]);

    await expect(new ReportRepository(db).listByOwner('archivist-4')).resolves.toEqual([
      sampleReport,
    ]);
    expect(calls[0]?.sql).toContain('FROM reports');
  });
});

describe('schedule repository', () => {
  it('binds the report, state and time zone', async () => {
    const { db, calls } = rawRecorder([]);

    await new ScheduleRepository(db).forReport(sampleReport.id, 'paused', 'Europe/Copenhagen');
    await new ScheduleRepository(db).forReport(sampleReport.id, 'all');

    expect(calls[0]?.bindings).toEqual([
      sampleReport.id,
      'paused',
      'paused',
      'Europe/Copenhagen',
      'Europe/Copenhagen',
    ]);
    expect(calls[1]?.bindings).toEqual([sampleReport.id, 'all', 'all', null, null]);
  });
});

describe('formulas', () => {
  it('evaluates an expression over the row', () => {
    const row = sampleRows[0];
    if (!row) {
      throw new Error('fixture');
    }
    expect(evaluateFormula({ name: 'double', formula: 'pageCount * 2' }, row)).toBe(6);
    expect(evaluateFormula({ name: 'shout', formula: 'title.toUpperCase()' }, row)).toBe(
      'PAY SCALES',
    );
  });

  it('reports a formula that fails or runs too long', () => {
    const row = sampleRows[0];
    if (!row) {
      throw new Error('fixture');
    }
    expect(() => evaluateFormula({ name: 'bad', formula: 'nope(' }, row)).toThrow(FormulaError);
    expect(() => evaluateFormula({ name: 'slow', formula: 'while (true) {}' }, row)).toThrow(
      FormulaError,
    );
  });
});

describe('row comparator', () => {
  it('sorts by a column in either direction, nulls last', () => {
    const ascending = [...sampleRows].sort(compileComparator('pageCount', 'asc'));
    const descending = [...sampleRows].sort(compileComparator('title', 'desc'));

    expect(ascending.map((row) => row.pageCount)).toEqual([3, 11, null]);
    expect(descending.map((row) => row.title)).toEqual([
      'Pay scales',
      'Onboarding',
      'Leave policy',
    ]);
  });
});

describe('layouts', () => {
  it('reads columns and their formatters', () => {
    const layout = parseLayout(`title: Intake
columns:
  - field: number
    heading: Number
  - field: pageCount
    heading: Pages
    format: !!js/function "function (value) { return value === null ? 'n/a' : value + ' pp.'; }"
`);

    expect(layout.title).toBe('Intake');
    expect(layout.columns[1]?.format?.(3)).toBe('3 pp.');
  });

  it.each([
    'title: [unclosed',
    'title: No columns\ncolumns: []',
    'columns:\n  - field: secret\n    heading: x',
  ])('refuses %j', (text) => {
    expect(() => parseLayout(text)).toThrow(InvalidLayoutError);
  });
});

describe('column mappings', () => {
  it('maps report columns to partner field names', () => {
    expect(
      parseColumnMapping('source: partner-a\ncolumns:\n  number: ref\n  title: name\n'),
    ).toEqual({
      source: 'partner-a',
      columns: { number: 'ref', title: 'name' },
    });
  });

  it('refuses tags that construct code', () => {
    expect(() => parseColumnMapping('source: !!js/function "function () {}"\ncolumns: {}')).toThrow(
      InvalidColumnMappingError,
    );
  });

  it('refuses a document that maps nothing it knows', () => {
    expect(() => parseColumnMapping('columns:\n  owner: x\n')).toThrow(InvalidColumnMappingError);
  });
});

describe('layout overrides', () => {
  it('applies known settings', () => {
    expect(
      applyLayoutOverrides(defaultLayoutSettings, { orientation: 'landscape', fontSize: 12 }),
    ).toEqual({
      ...defaultLayoutSettings,
      orientation: 'landscape',
      fontSize: 12,
    });
  });

  it('ignores unknown and reserved keys', () => {
    const overrides = JSON.parse(
      '{"__proto__": {"polluted": true}, "color": "red", "showFooter": false}',
    ) as unknown;

    const settings = applyLayoutOverrides(defaultLayoutSettings, overrides);

    expect(settings).toEqual({ ...defaultLayoutSettings, showFooter: false });
    expect(({} as Record<string, unknown>).polluted).toBeUndefined();
  });

  it('refuses an invalid value', () => {
    expect(() => applyLayoutOverrides(defaultLayoutSettings, { fontSize: 99 })).toThrow();
  });
});

describe('report service', () => {
  const service = new ReportService({
    get: () => Promise.resolve(sampleReport),
    rows: () => Promise.resolve(sampleRows),
  });

  it('previews sorted rows with computed columns and settings', async () => {
    const preview = await service.preview(sampleReport, {
      computed: [{ name: 'label', formula: 'number + ": " + title' }],
      sort: { column: 'number', direction: 'asc' },
      settings: { pageSize: 'A3' },
    });

    expect(preview.settings.pageSize).toBe('A3');
    expect(preview.rows.map((row) => row.label)).toEqual([
      'HR-2024-000001: Onboarding',
      'HR-2024-000002: Pay scales',
      'HR-2024-000003: Leave policy',
    ]);
  });

  it('renders rows through a layout', async () => {
    const table = await service.render(sampleReport, {
      title: 'Intake',
      columns: [
        { field: 'createdAt', heading: 'Created' },
        {
          field: 'pageCount',
          heading: 'Pages',
          format: (value) => (value === null ? 'n/a' : value),
        },
      ],
    });

    expect(table.headings).toEqual(['Created', 'Pages']);
    expect(table.rows).toEqual([
      ['2024-01-02', '3'],
      ['2024-01-01', 'n/a'],
      ['2024-01-03', '11'],
    ]);
  });

  it('remaps rows to partner field names', async () => {
    const rows = await service.remap(sampleReport, { source: 'p', columns: { number: 'ref' } });

    expect(rows[0]).toEqual({ ref: 'HR-2024-000002' });
  });
});
