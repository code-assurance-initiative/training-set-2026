import type { Address } from '../../domain/value-objects/address.js';

/** Everything printed on one 4x6 inch label. */
export interface LabelData {
  readonly carrier: string;
  readonly carrierName: string;
  readonly serviceCode: string;
  readonly serviceName: string;
  readonly trackingNumber: string;
  readonly sender: Address;
  readonly recipient: Address;
  readonly weightKg: number;
  readonly pieceNumber: number;
  readonly pieceCount: number;
  readonly zone: string;
  readonly shipDate: Date;
  readonly reference?: string;
  readonly customerReference?: string;
  readonly instructions: readonly string[];
  readonly customs: boolean;
}
