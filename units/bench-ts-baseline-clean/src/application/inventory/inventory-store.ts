import type { Reservation } from '../reservations/reservation.js';
import type { StockKey, StockLevel } from './inventory.js';

/**
 * Stock on hand and reservations. Every member is synchronous: a sequence of calls made without
 * awaiting in between runs without interleaving, which is what keeps check-then-act sequences
 * atomic (ADR 0002).
 */
export interface InventoryStore {
  levelOf(key: StockKey): StockLevel;
  levelsForSku(skuCode: string): readonly StockLevel[];
  setOnHand(key: StockKey, onHand: number): void;
  findReservation(id: string): Reservation | undefined;
  saveReservation(reservation: Reservation): void;
  activeReservationsDueBy(now: Date): readonly Reservation[];
}
