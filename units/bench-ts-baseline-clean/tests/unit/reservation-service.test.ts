import { beforeEach, describe, expect, it } from 'vitest';
import { CatalogService } from '../../src/application/catalog/catalog-service.js';
import { StockService } from '../../src/application/inventory/stock-service.js';
import { ReservationService } from '../../src/application/reservations/reservation-service.js';
import { InMemoryCatalogStore } from '../../src/infrastructure/in-memory-catalog-store.js';
import { InMemoryInventoryStore } from '../../src/infrastructure/in-memory-inventory-store.js';
import { FakeClock } from '../support/fake-clock.js';
import { silentLogger } from '../support/silent-logger.js';

const key = { skuCode: 'BOLT-M8-40', binCode: 'B04-12-03' };

describe('ReservationService', () => {
  let clock: FakeClock;
  let inventory: InMemoryInventoryStore;
  let reservations: ReservationService;
  let nextId: number;

  beforeEach(() => {
    clock = new FakeClock();
    nextId = 0;
    const catalog = new CatalogService(new InMemoryCatalogStore(), silentLogger);
    catalog.registerSku({ code: key.skuCode, description: 'Hex bolt', unitOfMeasure: 'EA' });
    catalog.registerBin({ code: key.binCode, zone: 'DRY', capacity: 100 });
    inventory = new InMemoryInventoryStore();
    new StockService(catalog, inventory, silentLogger).receive(key, 10);
    reservations = new ReservationService(
      catalog,
      inventory,
      { defaultHoldMinutes: 15, maximumHoldMinutes: 60 },
      clock,
      () => `r-${String(++nextId)}`,
      silentLogger,
    );
  });

  function reserve(quantity: number, holdMinutes?: number) {
    const result = reservations.create({ ...key, quantity, holdMinutes });
    if (!result.ok) {
      throw new Error(`expected a reservation, got ${result.error.message}`);
    }
    return result.value;
  }

  describe('create', () => {
    it('holds stock for the default time', () => {
      const reservation = reserve(4);

      expect(reservation).toEqual({
        id: 'r-1',
        ...key,
        quantity: 4,
        createdAt: new Date('2026-10-07T08:00:00.000Z'),
        expiresAt: new Date('2026-10-07T08:15:00.000Z'),
        status: 'active',
      });
      expect(inventory.levelOf(key)).toMatchObject({ onHand: 10, reserved: 4, available: 6 });
    });

    it('holds stock for a requested time within the maximum', () => {
      expect(reserve(1, 60).expiresAt).toEqual(new Date('2026-10-07T09:00:00.000Z'));
    });

    it('refuses more than is available', () => {
      reserve(8);

      expect(reservations.create({ ...key, quantity: 3 })).toEqual({
        ok: false,
        error: {
          kind: 'conflict',
          message: "Only 2 units of 'BOLT-M8-40' are available in bin 'B04-12-03'.",
        },
      });
    });

    it('releases lapsed holds before deciding what is available', () => {
      reserve(10);
      clock.advanceMinutes(16);

      expect(reservations.create({ ...key, quantity: 10 })).toMatchObject({ ok: true });
      expect(reservations.get('r-1')).toMatchObject({ ok: true, value: { status: 'expired' } });
    });

    it.each([0, 61])('refuses a hold of %i minutes', (holdMinutes) => {
      expect(reservations.create({ ...key, quantity: 1, holdMinutes })).toMatchObject({
        ok: false,
        error: { kind: 'invalid' },
      });
    });

    it('refuses a non-positive quantity', () => {
      expect(reservations.create({ ...key, quantity: 0 })).toMatchObject({
        ok: false,
        error: { kind: 'invalid' },
      });
    });

    it('refuses an unknown bin', () => {
      expect(reservations.create({ ...key, binCode: 'Z99-99-99', quantity: 1 })).toMatchObject({
        ok: false,
        error: { kind: 'not-found' },
      });
    });
  });

  describe('release', () => {
    it('returns the held quantity to available stock', () => {
      const reservation = reserve(4);

      expect(reservations.release(reservation.id)).toMatchObject({
        ok: true,
        value: { status: 'released' },
      });
      expect(inventory.levelOf(key)).toMatchObject({ onHand: 10, reserved: 0, available: 10 });
    });

    it('refuses to settle a reservation twice', () => {
      const reservation = reserve(4);
      reservations.release(reservation.id);

      expect(reservations.release(reservation.id)).toMatchObject({
        ok: false,
        error: { kind: 'conflict' },
      });
    });
  });

  describe('fulfil', () => {
    it('removes the picked quantity from the bin', () => {
      const reservation = reserve(4);

      expect(reservations.fulfil(reservation.id)).toMatchObject({
        ok: true,
        value: { status: 'fulfilled' },
      });
      expect(inventory.levelOf(key)).toMatchObject({ onHand: 6, reserved: 0, available: 6 });
    });

    it('refuses a reservation whose hold has lapsed', () => {
      const reservation = reserve(4);
      clock.advanceMinutes(15);

      expect(reservations.fulfil(reservation.id)).toMatchObject({
        ok: false,
        error: { kind: 'conflict' },
      });
      expect(inventory.levelOf(key).onHand).toBe(10);
    });

    it('reports an unknown reservation as not found', () => {
      expect(reservations.fulfil('r-404')).toEqual({
        ok: false,
        error: { kind: 'not-found', message: 'Reservation r-404 does not exist.' },
      });
    });
  });

  describe('expireDue', () => {
    it('expires only the holds that have lapsed, once', () => {
      reserve(2, 10);
      reserve(2, 30);
      clock.advanceMinutes(20);

      expect(reservations.expireDue()).toBe(1);
      expect(reservations.expireDue()).toBe(0);
      expect(inventory.levelOf(key).reserved).toBe(2);
    });
  });

  it('reports an unknown reservation id as not found', () => {
    expect(reservations.get('r-404')).toMatchObject({ ok: false, error: { kind: 'not-found' } });
  });
});
