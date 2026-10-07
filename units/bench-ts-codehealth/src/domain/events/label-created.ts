import type { DomainEvent } from './domain-event.js';

export interface LabelCreated extends DomainEvent {
  readonly type: 'label-created';
  readonly shipmentId: string;
  readonly trackingNumber: string;
  readonly carrier: string;
}

export function labelCreated(
  shipmentId: string,
  trackingNumber: string,
  carrier: string,
  occurredAt: Date,
): LabelCreated {
  return { type: 'label-created', shipmentId, trackingNumber, carrier, occurredAt };
}
