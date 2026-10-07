import { beforeEach, describe, expect, it } from 'vitest';
import { CatalogService } from '../../src/application/catalog/catalog-service.js';
import { InMemoryCatalogStore } from '../../src/infrastructure/in-memory-catalog-store.js';
import { silentLogger } from '../support/silent-logger.js';

describe('CatalogService', () => {
  let catalog: CatalogService;

  beforeEach(() => {
    catalog = new CatalogService(new InMemoryCatalogStore(), silentLogger);
  });

  it('registers a SKU and finds it by code', () => {
    const sku = { code: 'BOLT-M8-40', description: 'Hex bolt M8 x 40', unitOfMeasure: 'EA' };

    expect(catalog.registerSku(sku)).toEqual({ ok: true, value: sku });
    expect(catalog.getSku('BOLT-M8-40')).toEqual({ ok: true, value: sku });
  });

  it('refuses a second SKU with the same code', () => {
    catalog.registerSku({ code: 'NUT-M8', description: 'Hex nut M8', unitOfMeasure: 'EA' });

    const result = catalog.registerSku({
      code: 'NUT-M8',
      description: 'Other',
      unitOfMeasure: 'BOX',
    });

    expect(result).toEqual({
      ok: false,
      error: { kind: 'conflict', message: "SKU 'NUT-M8' is already registered." },
    });
  });

  it('reports an unknown SKU as not found', () => {
    expect(catalog.getSku('MISSING')).toMatchObject({ ok: false, error: { kind: 'not-found' } });
  });

  it('registers a bin and refuses a duplicate', () => {
    const bin = { code: 'A01-02-03', zone: 'COLD', capacity: 40 };

    expect(catalog.registerBin(bin)).toEqual({ ok: true, value: bin });
    expect(catalog.registerBin(bin)).toMatchObject({ ok: false, error: { kind: 'conflict' } });
    expect(catalog.getBin('A01-02-03')).toEqual({ ok: true, value: bin });
  });

  it('lists SKUs and bins a page at a time', () => {
    catalog.registerSku({ code: 'SKU-B', description: 'B', unitOfMeasure: 'EA' });
    catalog.registerSku({ code: 'SKU-A', description: 'A', unitOfMeasure: 'EA' });
    catalog.registerBin({ code: 'A01-01-01', zone: 'DRY', capacity: 5 });

    expect(catalog.listSkus({ offset: 0, limit: 1 })).toEqual({
      items: [{ code: 'SKU-A', description: 'A', unitOfMeasure: 'EA' }],
      total: 2,
      offset: 0,
      limit: 1,
    });
    expect(catalog.listBins({ offset: 0, limit: 10 }).total).toBe(1);
  });

  describe('resolveLocation', () => {
    beforeEach(() => {
      catalog.registerSku({ code: 'BOLT-M8-40', description: 'Hex bolt', unitOfMeasure: 'EA' });
      catalog.registerBin({ code: 'B04-12-03', zone: 'DRY', capacity: 100 });
    });

    it('returns the bin when both the SKU and the bin exist', () => {
      expect(catalog.resolveLocation('BOLT-M8-40', 'B04-12-03')).toMatchObject({
        ok: true,
        value: { capacity: 100 },
      });
    });

    it('names the missing SKU', () => {
      expect(catalog.resolveLocation('NOPE', 'B04-12-03')).toEqual({
        ok: false,
        error: { kind: 'not-found', message: "SKU 'NOPE' does not exist." },
      });
    });

    it('names the missing bin', () => {
      expect(catalog.resolveLocation('BOLT-M8-40', 'Z99-99-99')).toEqual({
        ok: false,
        error: { kind: 'not-found', message: "Bin 'Z99-99-99' does not exist." },
      });
    });
  });
});
