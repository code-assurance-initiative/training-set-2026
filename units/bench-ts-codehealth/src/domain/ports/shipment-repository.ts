import type { Shipment, ShipmentStatus } from '../entities/shipment.js';

export interface ShipmentRepository {
  findById(id: string): Shipment | undefined;
  findByTrackingNumber(trackingNumber: string): Shipment | undefined;
  findByRecipientPostcode(postcode: string): Shipment[];
  listByStatus(status: ShipmentStatus): Shipment[];
  listByCarrier(carrier: string): Shipment[];
  listCreatedBetween(from: Date, to: Date): Shipment[];
  listOpen(): Shipment[];
  add(shipment: Shipment): void;
  update(shipment: Shipment): void;
  remove(id: string): boolean;
  markLabelled(id: string, trackingNumber: string, labelPath: string): void;
  markArchived(id: string): void;
  markVoided(id: string): void;
  countByStatus(): Record<ShipmentStatus, number>;
  countByCarrier(): Record<string, number>;
  purgeCreatedBefore(cutoff: Date): number;
  clear(): void;
}
