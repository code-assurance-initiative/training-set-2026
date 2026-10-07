import type { Express } from 'express';
import type { Logger } from 'pino';
import { createApp } from '../../src/app.js';
import { composeServices, type Services } from '../../src/composition.js';
import { createAccessTokenVerifier } from '../../src/http/authentication.js';
import { Scopes } from '../../src/http/scopes.js';
import { FakeClock } from './fake-clock.js';
import { silentLogger } from './silent-logger.js';
import { createTokenIssuer, testAudience, testIssuer, type TokenIssuer } from './test-tokens.js';

export interface TestApp {
  readonly app: Express;
  readonly clock: FakeClock;
  readonly tokens: TokenIssuer;
  readonly services: Services;
  /** An Authorization header value carrying every scope the API defines. */
  readonly fullAccess: string;
}

export interface TestAppOptions {
  readonly requireHttps?: boolean;
  readonly logger?: Logger;
}

export async function createTestApp(options: TestAppOptions = {}): Promise<TestApp> {
  const tokens = await createTokenIssuer();
  const clock = new FakeClock();
  const services = composeServices(
    { defaultHoldMinutes: 15, maximumHoldMinutes: 1_440 },
    clock,
    silentLogger,
  );
  const verifier = await createAccessTokenVerifier({
    issuer: testIssuer,
    audience: testAudience,
    publicKeyPem: tokens.publicKeyPem,
  });
  const app = createApp({
    services,
    verifier,
    logger: options.logger ?? silentLogger,
    requireHttps: options.requireHttps ?? false,
    trustProxy: 'loopback',
  });
  const token = await tokens.issue({ scopes: Object.values(Scopes) });
  return { app, clock, tokens, services, fullAccess: `Bearer ${token}` };
}
