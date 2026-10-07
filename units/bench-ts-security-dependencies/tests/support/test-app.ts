import type { Express } from 'express';
import { createApp } from '../../src/app.js';
import { composeServices, type Services } from '../../src/composition.js';
import { loadConfig } from '../../src/config.js';
import { createCarrierSigner, type CarrierSigner } from './carrier-keys.js';
import { FakeGeocoder, FakeLinehaul, fixedClock } from './fakes.js';
import { silentLogger } from './silent-logger.js';
import { testEnvironment } from './test-config.js';
import { createTokenIssuer, type TokenIssuer } from './test-tokens.js';

export interface TestApp {
  readonly app: Express;
  readonly services: Services;
  readonly tokens: TokenIssuer;
  readonly carrier: CarrierSigner;
  readonly linehaul: FakeLinehaul;
}

/** The whole HTTP application with the carrier, the geocoder and the clock replaced by fakes. */
export function createTestApp(now = '2026-11-01T18:00:00Z'): TestApp {
  const tokens = createTokenIssuer();
  const carrier = createCarrierSigner();
  const linehaul = new FakeLinehaul();
  const config = loadConfig(testEnvironment(tokens.publicKeyPem, carrier.publicKey));
  const services = composeServices(config, {
    geocoder: new FakeGeocoder(),
    linehaul,
    clock: fixedClock(now),
  });
  return { app: createApp(services, silentLogger), services, tokens, carrier, linehaul };
}
