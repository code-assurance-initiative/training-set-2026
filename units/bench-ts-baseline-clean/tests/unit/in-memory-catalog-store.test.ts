import { describe, expect, it } from 'vitest';
import { InMemoryCatalogStore } from '../../src/infrastructure/in-memory-catalog-store.js';

describe('InMemoryCatalogStore', () => {
  it('lists SKUs ordered by code, a page at a time', () => {
    const store = new InMemoryCatalogStore();
    for (const code of ['SKU-C', 'SKU-A', 'SKU-B']) {
      store.addSku({ code, description: code, unitOfMeasure: 'EA' });
    }

    const page = store.listSkus({ offset: 1, limit: 1 });

    expect(page.items.map((sku) => sku.code)).toEqual(['SKU-B']);
    expect(page.total).toBe(3);
  });

  it('keeps the first bin registered under a code', () => {
    const store = new InMemoryCatalogStore();

    expect(store.addBin({ code: 'A01-01-01', zone: 'DRY', capacity: 5 })).toBe(true);
    expect(store.addBin({ code: 'A01-01-01', zone: 'COLD', capacity: 9 })).toBe(false);
    expect(store.findBin('A01-01-01')?.zone).toBe('DRY');
  });

  it('returns an empty page past the end', () => {
    const store = new InMemoryCatalogStore();
    store.addBin({ code: 'A01-01-01', zone: 'DRY', capacity: 5 });

    expect(store.listBins({ offset: 5, limit: 10 })).toEqual({
      items: [],
      total: 1,
      offset: 5,
      limit: 10,
    });
  });
});
