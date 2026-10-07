import type { StockKey, StockLevel } from '../application/inventory/inventory.js';
import type { InventoryStore } from '../application/inventory/inventory-store.js';
import { isDueBy, type Reservation } from '../application/reservations/reservation.js';

/**
 * Holds stock and reservations in process memory (ADR 0002). Reserved quantities are derived from
 * the active reservations, so they cannot drift from them.
 */
export class InMemoryInventoryStore implements InventoryStore {
  private readonly onHand = new Map<
    string,
    { readonly key: StockKey; readonly quantity: number }
  >();
  private readonly reservations = new Map<string, Reservation>();

  levelOf(key: StockKey): StockLevel {
    const onHand = this.onHand.get(keyOf(key))?.quantity ?? 0;
    const reserved = this.reservedIn(key);
    return {
      skuCode: key.skuCode,
      binCode: key.binCode,
      onHand,
      reserved,
      available: onHand - reserved,
    };
  }

  levelsForSku(skuCode: string): readonly StockLevel[] {
    const bins = new Set<string>();
    for (const reservation of this.reservations.values()) {
      if (reservation.skuCode === skuCode) {
        bins.add(reservation.binCode);
      }
    }
    for (const { key } of this.onHand.values()) {
      if (key.skuCode === skuCode) {
        bins.add(key.binCode);
      }
    }
    return [...bins].sort().map((binCode) => this.levelOf({ skuCode, binCode }));
  }

  setOnHand(key: StockKey, onHand: number): void {
    this.onHand.set(keyOf(key), {
      key: { skuCode: key.skuCode, binCode: key.binCode },
      quantity: onHand,
    });
  }

  findReservation(id: string): Reservation | undefined {
    return this.reservations.get(id);
  }

  saveReservation(reservation: Reservation): void {
    this.reservations.set(reservation.id, reservation);
  }

  activeReservationsDueBy(now: Date): readonly Reservation[] {
    return [...this.reservations.values()].filter((reservation) => isDueBy(reservation, now));
  }

  private reservedIn(key: StockKey): number {
    let reserved = 0;
    for (const reservation of this.reservations.values()) {
      if (
        reservation.status === 'active' &&
        reservation.skuCode === key.skuCode &&
        reservation.binCode === key.binCode
      ) {
        reserved += reservation.quantity;
      }
    }
    return reserved;
  }
}

function keyOf(key: StockKey): string {
  return `${key.skuCode}|${key.binCode}`;
}
