export type PlainObject = Record<string, unknown>;

export function isPlainObject(value: unknown): value is PlainObject {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

/**
 * Merges `source` into `target` in place: nested objects are merged key by key, everything else
 * (arrays included) replaces the target's value. Returns `target`.
 */
export function deepMerge(target: PlainObject, source: PlainObject): PlainObject {
  for (const key of Object.keys(source)) {
    const value = source[key];
    const current = target[key];
    if (isPlainObject(value) && isPlainObject(current)) {
      deepMerge(current, value);
    } else {
      target[key] = value;
    }
  }
  return target;
}
