import { describe, expect, it } from 'vitest';
import type { Reservation } from '../../src/application/reservations/reservation.js';
import { InMemoryInventoryStore } from '../../src/infrastructure/in-memory-inventory-store.js';

const key = { skuCode: 'BOLT-M8-40', binCode: 'B04-12-03' };

function reservation(overrides: Partial<Reservation> = {}): Reservation {
  return {
    id: 'r-1',
    ...key,
    quantity: 3,
    createdAt: new Date('2026-10-07T08:00:00Z'),
    expiresAt: new Date('2026-10-07T08:15:00Z'),
    status: 'active',
    ...overrides,
  };
}

describe('InMemoryInventoryStore', () => {
  it('reports an empty level for stock it has never seen', () => {
    expect(new InMemoryInventoryStore().levelOf(key)).toEqual({
      ...key,
      onHand: 0,
      reserved: 0,
      available: 0,
    });
  });

  it('derives the reserved quantity from active reservations only', () => {
    const store = new InMemoryInventoryStore();
    store.setOnHand(key, 10);
    store.saveReservation(reservation({ id: 'r-1', quantity: 3 }));
    store.saveReservation(reservation({ id: 'r-2', quantity: 4, status: 'released' }));
    store.saveReservation(reservation({ id: 'r-3', quantity: 2, binCode: 'A01-01-01' }));

    expect(store.levelOf(key)).toEqual({ ...key, onHand: 10, reserved: 3, available: 7 });
  });

  it('lists a SKU in every bin that holds or reserves it, ordered by bin', () => {
    const store = new InMemoryInventoryStore();
    store.setOnHand({ skuCode: key.skuCode, binCode: 'C01-01-01' }, 1);
    store.setOnHand({ skuCode: 'OTHER-SKU', binCode: 'A01-01-01' }, 1);
    store.saveReservation(reservation({ binCode: 'A02-01-01', quantity: 1 }));

    expect(store.levelsForSku(key.skuCode).map((level) => level.binCode)).toEqual([
      'A02-01-01',
      'C01-01-01',
    ]);
  });

  it('finds active reservations whose hold has lapsed', () => {
    const store = new InMemoryInventoryStore();
    store.saveReservation(reservation({ id: 'due' }));
    store.saveReservation(
      reservation({ id: 'later', expiresAt: new Date('2026-10-07T09:00:00Z') }),
    );
    store.saveReservation(reservation({ id: 'settled', status: 'fulfilled' }));

    const due = store.activeReservationsDueBy(new Date('2026-10-07T08:15:00Z'));

    expect(due.map((r) => r.id)).toEqual(['due']);
  });

  it('replaces a saved reservation with the same id', () => {
    const store = new InMemoryInventoryStore();
    store.saveReservation(reservation());
    store.saveReservation(reservation({ status: 'released' }));

    expect(store.findReservation('r-1')?.status).toBe('released');
  });
});
