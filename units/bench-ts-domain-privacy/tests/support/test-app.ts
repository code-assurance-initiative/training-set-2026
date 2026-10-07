import type { Express } from 'express';
import type { Knex } from 'knex';
import type { Logger } from 'pino';
import request from 'supertest';
import { createApp } from '../../src/app.js';
import { composeServices, type Services } from '../../src/composition.js';
import type { AppConfig } from '../../src/config.js';
import { createAccessTokenVerifier } from '../../src/platform/http/authentication.js';
import { Scopes } from '../../src/platform/http/scopes.js';
import { FakeClock } from './fake-clock.js';
import { FakeProvider } from './fake-provider.js';
import { silentLogger } from './silent-logger.js';
import { testConfig } from './test-config.js';
import { createTestDatabase } from './test-database.js';
import { createTokenIssuer, type TokenIssuer } from './test-tokens.js';

export interface TestApp {
  readonly app: Express;
  readonly db: Knex;
  readonly clock: FakeClock;
  readonly config: AppConfig;
  readonly provider: FakeProvider;
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
  const config = testConfig(tokens.publicKeyPem);
  const db = await createTestDatabase();
  const provider = new FakeProvider();
  const logger = options.logger ?? silentLogger;
  const services = composeServices({ config, db, logger, clock, fetchFn: provider.fetch });
  const verifier = await createAccessTokenVerifier(config.jwt);
  const app = createApp({
    services,
    verifier,
    logger,
    requireHttps: options.requireHttps ?? false,
    trustProxy: 'loopback',
  });
  const token = await tokens.issue({ scopes: Object.values(Scopes) });
  return { app, db, clock, config, provider, tokens, services, fullAccess: `Bearer ${token}` };
}

export interface MemberInput {
  readonly firstName?: string;
  readonly lastName?: string;
  readonly email?: string;
  readonly phone?: string;
  readonly dateOfBirth?: string;
  readonly membershipEndsOn?: string;
}

/** Registers a member through the API and returns the new member id. */
export async function registerMember(api: TestApp, input: MemberInput = {}): Promise<string> {
  const response = await request(api.app)
    .post('/api/members')
    .set('Authorization', api.fullAccess)
    .send({
      firstName: 'Ada',
      lastName: 'Lindqvist',
      email: 'ada.lindqvist@example.net',
      dateOfBirth: '1990-04-12',
      membershipEndsOn: '2026-12-31',
      ...input,
    })
    .expect(201);
  return (response.body as { memberId: string }).memberId;
}

/** Schedules a class through the API and returns the new session id. */
export async function scheduleClass(
  api: TestApp,
  input: { startsAt?: string; capacity?: number; title?: string } = {},
): Promise<string> {
  const response = await request(api.app)
    .post('/api/classes')
    .set('Authorization', api.fullAccess)
    .send({
      title: 'Spinning',
      startsAt: '2026-10-07T18:00:00Z',
      durationMinutes: 45,
      capacity: 12,
      ...input,
    })
    .expect(201);
  return (response.body as { sessionId: string }).sessionId;
}

export async function bookClass(
  api: TestApp,
  sessionId: string,
  memberId: string,
): Promise<string> {
  const response = await request(api.app)
    .post(`/api/classes/${sessionId}/bookings`)
    .set('Authorization', api.fullAccess)
    .send({ memberId })
    .expect(201);
  return (response.body as { bookingId: string }).bookingId;
}
