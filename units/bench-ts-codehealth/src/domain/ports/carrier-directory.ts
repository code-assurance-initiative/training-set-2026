import type { CarrierGateway } from './carrier-gateway.js';

/** Looks carriers up by code or alias. */
export interface CarrierDirectory {
  readonly codes: readonly string[];
  find(code: string): CarrierGateway | undefined;
}
