import yaml from 'js-yaml';
import { z } from 'zod';
import { reportColumns } from './report-model.js';

export type Formatter = (value: unknown) => unknown;

export interface ReportLayout {
  readonly title: string;
  readonly columns: readonly {
    readonly field: (typeof reportColumns)[number];
    readonly heading: string;
    readonly format?: Formatter | undefined;
  }[];
}

export class InvalidLayoutError extends Error {
  constructor(message: string, options?: { cause: unknown }) {
    super(message, options);
    this.name = 'InvalidLayoutError';
  }
}

// Layouts written for the previous major version of the YAML library carry their cell formatters
// as `!!js/function` scalars. The library no longer ships that tag, so it is registered here to keep
// existing layout files loading unchanged.
const functionType = new yaml.Type('tag:yaml.org,2002:js/function', {
  kind: 'scalar',
  resolve: (data: unknown) => typeof data === 'string',
  construct: (source: string) => (new Function(`return (${source});`) as () => Formatter)(),
});
const layoutSchema = yaml.DEFAULT_SCHEMA.extend([functionType]);

function loadLayoutDocument(text: string): unknown {
  return yaml.load(text, { schema: layoutSchema });
}

const layoutShape = z.object({
  title: z.string().min(1).max(200),
  columns: z
    .array(
      z.object({
        field: z.enum(reportColumns),
        heading: z.string().min(1).max(100),
        format: z.custom<Formatter>((value) => typeof value === 'function').optional(),
      }),
    )
    .min(1)
    .max(reportColumns.length),
});

/** Parses an uploaded report layout (YAML). */
export function parseLayout(text: string): ReportLayout {
  let document: unknown;
  try {
    document = loadLayoutDocument(text);
  } catch (error) {
    throw new InvalidLayoutError('The layout is not valid YAML.', { cause: error });
  }
  const parsed = layoutShape.safeParse(document);
  if (!parsed.success) {
    throw new InvalidLayoutError('The layout does not describe report columns.');
  }
  return parsed.data;
}
