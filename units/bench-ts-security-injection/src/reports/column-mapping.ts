import yaml from 'js-yaml';
import { z } from 'zod';
import { reportColumns } from './report-model.js';

const columnMappingShape = z.object({
  source: z.string().min(1).max(100),
  columns: z.partialRecord(z.enum(reportColumns), z.string().min(1).max(100)),
});

export type ColumnMapping = z.infer<typeof columnMappingShape>;

export class InvalidColumnMappingError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidColumnMappingError';
  }
}

/** Parses an uploaded column mapping (YAML) that maps report columns to a partner's field names. */
export function parseColumnMapping(text: string): ColumnMapping {
  let document: unknown;
  try {
    document = yaml.load(text);
  } catch {
    throw new InvalidColumnMappingError('The column mapping is not valid YAML.');
  }
  const parsed = columnMappingShape.safeParse(document);
  if (!parsed.success) {
    throw new InvalidColumnMappingError('The column mapping does not map report columns.');
  }
  return parsed.data;
}
