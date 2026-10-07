import type { Shipment } from '../entities/shipment.js';

/**
 * Optional callbacks around the label lifecycle (metrics, notifications, audit). Implement only the
 * hooks you need; every hook is awaited if it returns a promise and a failing hook is logged, never
 * propagated.
 */
export interface LabelLifecycleHooks {
  beforeQuote?(shipment: Shipment): void | Promise<void>;
  afterQuote?(shipment: Shipment): void | Promise<void>;
  beforeCarrierSelected?(shipment: Shipment): void | Promise<void>;
  afterCarrierSelected?(shipment: Shipment, carrier: string): void | Promise<void>;
  beforeLabelRequested?(shipment: Shipment): void | Promise<void>;
  afterLabelRequested?(shipment: Shipment): void | Promise<void>;
  beforeRender?(shipment: Shipment): void | Promise<void>;
  afterRender?(shipment: Shipment, zpl: string): void | Promise<void>;
  beforeArchive?(shipment: Shipment): void | Promise<void>;
  afterArchive?(shipment: Shipment, path: string): void | Promise<void>;
  beforeEmail?(shipment: Shipment): void | Promise<void>;
  afterEmail?(shipment: Shipment): void | Promise<void>;
  onEmailFailed?(shipment: Shipment, error: unknown): void | Promise<void>;
  beforeVoid?(shipment: Shipment): void | Promise<void>;
  afterVoid?(shipment: Shipment): void | Promise<void>;
  onPurge?(count: number): void | Promise<void>;
  onBatchClosed?(batchId: string, labelCount: number): void | Promise<void>;
}
