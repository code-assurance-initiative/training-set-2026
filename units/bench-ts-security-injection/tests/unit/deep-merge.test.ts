import { describe, expect, it } from 'vitest';
import { deepMerge, isPlainObject } from '../../src/preferences/deep-merge.js';

describe('deep merge', () => {
  it('merges nested objects and replaces everything else', () => {
    const target = {
      search: { pageSize: 25, collections: ['HR'] },
      notifications: { exportReady: true },
    };

    const merged = deepMerge(target, {
      search: { pageSize: 50, collections: ['FIN'] },
      theme: 'dark',
    });

    expect(merged).toBe(target);
    expect(merged).toEqual({
      search: { pageSize: 50, collections: ['FIN'] },
      notifications: { exportReady: true },
      theme: 'dark',
    });
  });

  it('replaces a scalar with an object and an object with a scalar', () => {
    expect(deepMerge({ a: 1, b: { c: 2 } }, { a: { x: 1 }, b: 3 })).toEqual({ a: { x: 1 }, b: 3 });
  });

  it('knows what a plain object is', () => {
    expect(isPlainObject({})).toBe(true);
    expect(isPlainObject([])).toBe(false);
    expect(isPlainObject(null)).toBe(false);
    expect(isPlainObject('x')).toBe(false);
  });
});
