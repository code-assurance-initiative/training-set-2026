export interface ReportSummary {
  readonly id: string;
  readonly name: string;
  readonly owner: string;
  readonly updatedAt: Date;
}

export interface ReportDefinition extends ReportSummary {
  readonly collection: string;
}

/** One row of a report: a document of the report's collection. */
export interface ReportRow {
  readonly number: string;
  readonly title: string;
  readonly collection: string;
  readonly pageCount: number | null;
  readonly createdAt: Date;
}

export const reportColumns = ['number', 'title', 'collection', 'pageCount', 'createdAt'] as const;

export type ReportColumn = (typeof reportColumns)[number];
