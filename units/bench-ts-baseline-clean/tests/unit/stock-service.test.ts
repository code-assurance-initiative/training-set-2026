import { beforeEach, describe, expect, it } from 'vitest';
import { CatalogService } from '../../src/application/catalog/catalog-service.js';
import { StockService } from '../../src/application/inventory/stock-service.js';
import { InMemoryCatalogStore } from '../../src/infrastructure/in-memory-catalog-store.js';
import { InMemoryInventoryStore } from '../../src/infrastructure/in-memory-inventory-store.js';
import { silentLogger } from '../support/silent-logger.js';

const key = { skuCode: 'BOLT-M8-40', binCode: 'B04-12-03' };

describe('StockService', () => {
  let inventory: InMemoryInventoryStore;
  let stock: StockService;

  beforeEach(() => {
    const catalog = new CatalogService(new InMemoryCatalogStore(), silentLogger);
    catalog.registerSku({ code: key.skuCode, description: 'Hex bolt', unitOfMeasure: 'EA' });
    catalog.registerBin({ code: key.binCode, zone: 'DRY', capacity: 100 });
    inventory = new InMemoryInventoryStore();
    stock = new StockService(catalog, inventory, silentLogger);
  });

  describe('receive', () => {
    it('adds the received quantity to what is on hand', () => {
      stock.receive(key, 30);

      expect(stock.receive(key, 20)).toEqual({
        ok: true,
        value: { ...key, onHand: 50, reserved: 0, available: 50 },
      });
    });

    it('refuses a receipt that would overfill the bin', () => {
      stock.receive(key, 90);

      expect(stock.receive(key, 11)).toMatchObject({ ok: false, error: { kind: 'conflict' } });
      expect(inventory.levelOf(key).onHand).toBe(90);
    });

    it.each([0, -5])('refuses a receipt of %i units', (quantity) => {
      expect(stock.receive(key, quantity)).toMatchObject({ ok: false, error: { kind: 'invalid' } });
    });

    it('refuses a receipt into an unknown bin', () => {
      expect(stock.receive({ ...key, binCode: 'Z99-99-99' }, 1)).toMatchObject({
        ok: false,
        error: { kind: 'not-found' },
      });
    });
  });

  describe('count', () => {
    it('replaces the quantity on hand', () => {
      stock.receive(key, 40);

      expect(stock.count(key, 37)).toMatchObject({ ok: true, value: { onHand: 37 } });
    });

    it('refuses a count below the reserved quantity', () => {
      stock.receive(key, 40);
      inventory.saveReservation({
        id: 'r-1',
        ...key,
        quantity: 10,
        createdAt: new Date('2026-10-07T08:00:00Z'),
        expiresAt: new Date('2026-10-07T09:00:00Z'),
        status: 'active',
      });

      expect(stock.count(key, 9)).toMatchObject({ ok: false, error: { kind: 'conflict' } });
    });

    it('refuses a count above the bin capacity', () => {
      expect(stock.count(key, 101)).toMatchObject({ ok: false, error: { kind: 'invalid' } });
    });

    it('refuses a negative count', () => {
      expect(stock.count(key, -1)).toMatchObject({ ok: false, error: { kind: 'invalid' } });
    });

    it('refuses a count for an unknown SKU', () => {
      expect(stock.count({ ...key, skuCode: 'NOPE' }, 1)).toMatchObject({
        ok: false,
        error: { kind: 'not-found' },
      });
    });
  });

  it('lists the levels of a SKU in every bin that holds it', () => {
    stock.receive(key, 5);

    expect(stock.getLevels(key.skuCode)).toEqual({
      ok: true,
      value: [{ ...key, onHand: 5, reserved: 0, available: 5 }],
    });
    expect(stock.getLevels('NOPE')).toMatchObject({ ok: false, error: { kind: 'not-found' } });
  });
});
