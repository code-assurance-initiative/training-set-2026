import type { Address } from '../value-objects/address.js';
import type { Money } from '../value-objects/money.js';
import type { Parcel } from '../value-objects/parcel.js';
import { AggregateRoot } from './aggregate-root.js';

export type ShipmentStatus = 'draft' | 'labelled' | 'archived' | 'voided';

export class Shipment extends AggregateRoot {
  status: ShipmentStatus = 'draft';
  carrier: string;
  serviceLevel: string;
  sender: Address;
  recipient: Address;
  parcels: Parcel[];
  trackingNumber: string | undefined;
  labelPath: string | undefined;
  price: Money | undefined;
  createdAt: Date;

  constructor(
    id: string,
    carrier: string,
    serviceLevel: string,
    sender: Address,
    recipient: Address,
    parcels: Parcel[],
    createdAt: Date,
  ) {
    super(id);
    this.carrier = carrier;
    this.serviceLevel = serviceLevel;
    this.sender = sender;
    this.recipient = recipient;
    this.parcels = parcels;
    this.createdAt = createdAt;
  }

  get isInternational(): boolean {
    return this.sender.country !== this.recipient.country;
  }
}
