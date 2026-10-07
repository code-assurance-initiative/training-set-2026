export type JsonValue =
  string | number | boolean | null | JsonValue[] | { [key: string]: JsonValue };

/**
 * Flattens a metadata document uploaded as JSON into dotted field names, e.g.
 * `{ "creator": { "name": "Records Office" } }` becomes `creator.name = Records Office`.
 */
export function flattenMetadata(document: JsonValue): Map<string, string> {
  const fields = new Map<string, string>();
  flattenInto(fields, '', document);
  return fields;
}

function flattenInto(fields: Map<string, string>, name: string, value: JsonValue): void {
  if (value === null || typeof value !== 'object') {
    fields.set(name, String(value));
    return;
  }
  const entries = Array.isArray(value)
    ? value.map((child, index) => [`${index}`, child] as const)
    : Object.entries(value);
  for (const [key, child] of entries) {
    flattenInto(fields, name === '' ? key : `${name}.${key}`, child);
  }
}
