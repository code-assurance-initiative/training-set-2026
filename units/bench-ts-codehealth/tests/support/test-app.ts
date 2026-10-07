import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import type { Express } from 'express';
import { createApp } from '../../src/app.js';
import { createAccessTokenVerifier } from '../../src/api/authentication.js';
import { Scopes } from '../../src/api/scopes.js';
import { composeServices, type Services } from '../../src/composition.js';
import { rateCard } from './builders.js';
import { FakeCarriers, MemoryLabelStore, RateCardList, RecordingMailer } from './fakes.js';
import { silentLogger } from './silent-logger.js';
import { createTokenIssuer, testAudience, testIssuer, type TokenIssuer } from './test-tokens.js';

export const webhookSecret = 'test-webhook-secret-that-is-long-enough';

export interface TestApp {
  readonly app: Express;
  readonly services: Services;
  readonly carriers: FakeCarriers;
  readonly store: MemoryLabelStore;
  readonly mailer: RecordingMailer;
  readonly tokens: TokenIssuer;
  /** An Authorization header value carrying every scope the API defines. */
  readonly fullAccess: string;
}

export async function createTestApp(): Promise<TestApp> {
  const tokens = await createTokenIssuer();
  const carriers = new FakeCarriers();
  const store = new MemoryLabelStore();
  const mailer = new RecordingMailer();
  const directory = await mkdtemp(join(tmpdir(), 'parcel-rates-'));
  let id = 0;
  const services = composeServices(
    {
      carriers: {
        alder: { baseUrl: 'https://alder.test', apiKey: 'unused-in-tests', timeoutMs: 1_000 },
        corvid: { baseUrl: 'https://corvid.test', accountNumber: 'A1', token: 'unused-in-tests' },
        cutoffs: { alder: '16:00', corvid: '15:30' },
      },
      labels: { archiveDir: directory, fromAddress: 'labels@parcel-rates.test', retentionDays: 30 },
      webhookSecret,
    },
    {
      rateCards: new RateCardList([
        rateCard(),
        rateCard({
          carrier: 'corvid',
          perKilogram: { Z1: 900, default: 2_500 },
          minimum: { Z1: 3_900, default: 9_900 },
          transitDays: 1,
        }),
      ]),
      carriers,
      store,
      mailer,
      clock: () => new Date('2026-10-07T09:00:00.000Z'),
      newId: () => `00000000-0000-4000-8000-${String(++id).padStart(12, '0')}`,
    },
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
    logger: silentLogger,
    clock: () => new Date('2026-10-07T09:00:00.000Z'),
    requireHttps: false,
    trustProxy: 'loopback',
  });
  const token = await tokens.issue({ scopes: Object.values(Scopes) });
  return { app, services, carriers, store, mailer, tokens, fullAccess: `Bearer ${token}` };
}
