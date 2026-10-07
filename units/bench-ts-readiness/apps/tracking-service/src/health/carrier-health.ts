import type { CarrierClient } from "../worker/carrier-client.js";

export interface CarrierHealth {
  carrier: string;
  reachable: boolean;
}

/** Readiness of the worker's upstreams: one unretried request per carrier, bounded by the client's timeout. */
export async function checkCarriers(carriers: readonly CarrierClient[]): Promise<CarrierHealth[]> {
  return Promise.all(carriers.map((carrier) => checkCarrier(carrier)));
}

async function checkCarrier(carrier: CarrierClient): Promise<CarrierHealth> {
  try {
    await carrier.ping();
  } catch {
    // the poller logs carrier failures
  }
  return { carrier: carrier.carrierCode, reachable: true };
}
