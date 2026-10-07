import type { Logger } from 'pino';
import type { CatalogService } from '../catalog/catalog-service.js';
import type { Clock } from '../clock.js';
import type { StockKey } from '../inventory/inventory.js';
import type { InventoryStore } from '../inventory/inventory-store.js';
import { conflict, invalid, notFound, success, type Result } from '../result.js';
import { isDueBy, type Reservation, type ReservationStatus } from './reservation.js';

export interface ReservationPolicy {
  readonly defaultHoldMinutes: number;
  readonly maximumHoldMinutes: number;
}

export interface NewReservation extends StockKey {
  readonly quantity: number;
  /** How long to hold the stock; the policy default when omitted. */
  readonly holdMinutes?: number | undefined;
}

const millisecondsPerMinute = 60_000;

export class ReservationService {
  constructor(
    private readonly catalog: CatalogService,
    private readonly inventory: InventoryStore,
    private readonly policy: ReservationPolicy,
    private readonly clock: Clock,
    private readonly newId: () => string,
    private readonly logger: Logger,
  ) {}

  create(request: NewReservation): Result<Reservation> {
    if (request.quantity <= 0) {
      return invalid('A reservation must be for a positive quantity.');
    }
    const holdMinutes = request.holdMinutes ?? this.policy.defaultHoldMinutes;
    if (holdMinutes <= 0 || holdMinutes > this.policy.maximumHoldMinutes) {
      return invalid(
        `A reservation may be held for at most ${this.policy.maximumHoldMinutes} minutes; ${holdMinutes} were requested.`,
      );
    }
    const location = this.catalog.resolveLocation(request.skuCode, request.binCode);
    if (!location.ok) {
      return location;
    }

    const now = this.clock.now();
    this.expireDueAt(now);
    const available = this.inventory.levelOf(request).available;
    if (available < request.quantity) {
      return conflict(
        `Only ${available} units of '${request.skuCode}' are available in bin '${request.binCode}'.`,
      );
    }

    const reservation: Reservation = {
      id: this.newId(),
      skuCode: request.skuCode,
      binCode: request.binCode,
      quantity: request.quantity,
      createdAt: now,
      expiresAt: new Date(now.getTime() + holdMinutes * millisecondsPerMinute),
      status: 'active',
    };
    this.inventory.saveReservation(reservation);
    this.logger.info(
      {
        reservationId: reservation.id,
        skuCode: reservation.skuCode,
        binCode: reservation.binCode,
        quantity: reservation.quantity,
      },
      'Reserved stock',
    );
    return success(reservation);
  }

  get(id: string): Result<Reservation> {
    const reservation = this.inventory.findReservation(id);
    return reservation ? success(reservation) : reservationNotFound(id);
  }

  /** Cancels an active reservation and returns its quantity to available stock. */
  release(id: string): Result<Reservation> {
    return this.settle(id, 'released');
  }

  /** Picks the reserved quantity: it leaves the bin, and the reservation is closed. */
  fulfil(id: string): Result<Reservation> {
    return this.settle(id, 'fulfilled');
  }

  /** Marks every active reservation whose hold has lapsed as expired. Safe to run repeatedly. */
  expireDue(): number {
    const expired = this.expireDueAt(this.clock.now());
    if (expired > 0) {
      this.logger.info({ count: expired }, 'Expired lapsed reservations');
    }
    return expired;
  }

  private settle(id: string, outcome: 'released' | 'fulfilled'): Result<Reservation> {
    const reservation = this.inventory.findReservation(id);
    if (!reservation) {
      return reservationNotFound(id);
    }
    if (reservation.status !== 'active' || isDueBy(reservation, this.clock.now())) {
      return conflict(`Reservation ${id} is no longer active.`);
    }
    if (outcome === 'fulfilled') {
      const level = this.inventory.levelOf(reservation);
      this.inventory.setOnHand(reservation, level.onHand - reservation.quantity);
    }
    const settled = withStatus(reservation, outcome);
    this.inventory.saveReservation(settled);
    this.logger.info({ reservationId: id, status: outcome }, 'Settled reservation');
    return success(settled);
  }

  private expireDueAt(now: Date): number {
    const due = this.inventory.activeReservationsDueBy(now);
    for (const reservation of due) {
      this.inventory.saveReservation(withStatus(reservation, 'expired'));
    }
    return due.length;
  }
}

function withStatus(reservation: Reservation, status: ReservationStatus): Reservation {
  return { ...reservation, status };
}

function reservationNotFound(id: string): Result<never> {
  return notFound(`Reservation ${id} does not exist.`);
}
