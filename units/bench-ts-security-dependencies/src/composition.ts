import { MapsGeocoder, type Geocoder } from './addresses/geocoder.js';
import { createTerminalTokenVerifier, type TerminalTokenVerifier } from './auth/terminal-tokens.js';
import { LinehaulClient, type Linehaul } from './carriers/linehaul-client.js';
import type { AppConfig } from './config.js';
import { InMemoryDispatchStore, type DispatchStore } from './dispatch/dispatch-store.js';
import { DispatchService, systemClock, type Clock } from './dispatch/dispatch-service.js';
import { LabelRasteriser } from './labels/label-rasteriser.js';
import { CarrierSignatureVerifier } from './webhooks/partner-signature.js';

export interface Services {
  readonly store: DispatchStore;
  readonly dispatch: DispatchService;
  readonly linehaul: Linehaul;
  readonly rasteriser: LabelRasteriser;
  readonly signatures: CarrierSignatureVerifier;
  readonly tokens: TerminalTokenVerifier;
}

/** Collaborators that talk to the outside world, replaceable in tests. */
export interface Gateways {
  readonly geocoder?: Geocoder;
  readonly linehaul?: Linehaul;
  readonly clock?: Clock;
}

export function composeServices(config: AppConfig, gateways: Gateways = {}): Services {
  const store = new InMemoryDispatchStore();
  const geocoder = gateways.geocoder ?? new MapsGeocoder(config.geocodingApiKey);
  return {
    store,
    dispatch: new DispatchService(store, geocoder, gateways.clock ?? systemClock),
    linehaul: gateways.linehaul ?? new LinehaulClient(config.linehaul),
    rasteriser: new LabelRasteriser(config.labelPrinterDpi),
    signatures: new CarrierSignatureVerifier(config.carrierStatusPublicKey),
    tokens: createTerminalTokenVerifier(config.terminalTokens),
  };
}
