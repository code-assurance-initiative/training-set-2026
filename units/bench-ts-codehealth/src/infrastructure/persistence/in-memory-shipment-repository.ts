import type { Shipment, ShipmentStatus } from '../../domain/entities/shipment.js';
import type { ShipmentRepository } from '../../domain/ports/shipment-repository.js';

/** Shipments in process memory (ADR 0001: the service keeps no database of its own yet). */
export class InMemoryShipmentRepository implements ShipmentRepository {
  readonly #shipments = new Map<string, Shipment>();

  findById(id: string): Shipment | undefined {
    return this.#shipments.get(id);
  }

  findByTrackingNumber(trackingNumber: string): Shipment | undefined {
    return this.all().find((shipment) => shipment.trackingNumber === trackingNumber);
  }

  findByRecipientPostcode(postcode: string): Shipment[] {
    return this.all().filter((shipment) => shipment.recipient.postcode === postcode);
  }

  listByStatus(status: ShipmentStatus): Shipment[] {
    return this.all().filter((shipment) => shipment.status === status);
  }

  listByCarrier(carrier: string): Shipment[] {
    return this.all().filter((shipment) => shipment.carrier === carrier);
  }

  listCreatedBetween(from: Date, to: Date): Shipment[] {
    return this.all().filter((shipment) => shipment.createdAt >= from && shipment.createdAt < to);
  }

  listOpen(): Shipment[] {
    return this.all().filter(
      (shipment) => shipment.status === 'draft' || shipment.status === 'labelled',
    );
  }

  add(shipment: Shipment): void {
    if (this.#shipments.has(shipment.id)) {
      throw new Error(`Shipment ${shipment.id} already exists`);
    }
    this.#shipments.set(shipment.id, shipment);
  }

  update(shipment: Shipment): void {
    this.#shipments.set(shipment.id, shipment);
  }

  remove(id: string): boolean {
    return this.#shipments.delete(id);
  }

  markLabelled(id: string, trackingNumber: string, labelPath: string): void {
    const shipment = this.require(id);
    shipment.status = 'labelled';
    shipment.trackingNumber = trackingNumber;
    shipment.labelPath = labelPath;
  }

  markArchived(id: string): void {
    this.require(id).status = 'archived';
  }

  markVoided(id: string): void {
    const shipment = this.require(id);
    shipment.status = 'voided';
    shipment.labelPath = undefined;
  }

  countByStatus(): Record<ShipmentStatus, number> {
    const counts: Record<ShipmentStatus, number> = {
      draft: 0,
      labelled: 0,
      archived: 0,
      voided: 0,
    };
    for (const shipment of this.#shipments.values()) {
      counts[shipment.status] += 1;
    }
    return counts;
  }

  countByCarrier(): Record<string, number> {
    const counts: Record<string, number> = {};
    for (const shipment of this.#shipments.values()) {
      counts[shipment.carrier] = (counts[shipment.carrier] ?? 0) + 1;
    }
    return counts;
  }

  purgeCreatedBefore(cutoff: Date): number {
    let purged = 0;
    for (const shipment of this.all()) {
      if (shipment.createdAt < cutoff && this.#shipments.delete(shipment.id)) {
        purged += 1;
      }
    }
    return purged;
  }

  clear(): void {
    this.#shipments.clear();
  }

  private all(): Shipment[] {
    return [...this.#shipments.values()];
  }

  private require(id: string): Shipment {
    const shipment = this.#shipments.get(id);
    if (!shipment) {
      throw new Error(`No shipment ${id}`);
    }
    return shipment;
  }
}
