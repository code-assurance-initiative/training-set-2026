import type { StockKey } from '../inventory/inventory.js';

export type ReservationStatus = 'active' | 'released' | 'fulfilled' | 'expired';

/** A hold on a quantity of stock in one bin, for a limited time. */
export interface Reservation extends StockKey {
  readonly id: string;
  readonly quantity: number;
  readonly createdAt: Date;
  readonly expiresAt: Date;
  readonly status: ReservationStatus;
}

export function isDueBy(reservation: Reservation, now: Date): boolean {
  return reservation.status === 'active' && reservation.expiresAt.getTime() <= now.getTime();
}
