import { mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import path from 'node:path';
import type { Express } from 'express';
import type { Logger } from 'pino';
import { createApp, type AppDependencies } from '../../src/app.js';
import { CardRenderer } from '../../src/documents/card-renderer.js';
import type { DocumentRecord } from '../../src/documents/document-repository.js';
import { FeedNotAllowedError } from '../../src/feeds/partner-feed-client.js';
import { createAccessTokenVerifier } from '../../src/http/authentication.js';
import { Scopes } from '../../src/http/scopes.js';
import type { PreferenceDocument } from '../../src/preferences/preferences.js';
import { ReportService } from '../../src/reports/report-service.js';
import type { SavedSearch } from '../../src/saved-searches/saved-search-store.js';
import type { DocumentHit } from '../../src/search/document-search-repository.js';
import { SearchService } from '../../src/search/search-service.js';
import { ShareGrants } from '../../src/shares/share-grants.js';
import type { ShareLink } from '../../src/shares/share-store.js';
import type { Subscription } from '../../src/subscriptions/subscription.js';
import { TemplateStore } from '../../src/templates/template-store.js';
import { sampleDocument, sampleReport, sampleRows } from './documents.js';
import { FakeCollection } from './fake-collection.js';
import { silentLogger } from './silent-logger.js';
import { createTokenIssuer, testAudience, testIssuer, type TokenIssuer } from './test-tokens.js';

export const now = new Date('2026-10-07T08:00:00.000Z');

export interface TestApp {
  readonly app: Express;
  readonly tokens: TokenIssuer;
  /** An Authorization header value carrying every scope the API defines. */
  readonly fullAccess: string;
  readonly storageRoot: string;
  readonly documents: Map<string, DocumentRecord>;
  readonly texts: Map<string, string>;
  readonly hits: DocumentHit[];
  readonly savedSearches: SavedSearch[];
  readonly opened: string[];
  readonly shares: ShareLink[];
  readonly subscriptions: FakeCollection<Subscription>;
  readonly preferences: FakeCollection<PreferenceDocument>;
  readonly exports: { created: unknown[] };
  readonly deliveries: { reportId: string; recipients: number }[];
  readonly channelChanges: { id: string; enabled: boolean }[];
}

export interface TestAppOptions {
  readonly requireHttps?: boolean;
  readonly logger?: Logger;
}

export async function createTestApp(options: TestAppOptions = {}): Promise<TestApp> {
  const tokens = await createTokenIssuer();
  const storageRoot = await mkdtemp(path.join(tmpdir(), 'archive-'));
  const documents = new Map([[sampleDocument.id, sampleDocument]]);
  const texts = new Map<string, string>();
  const hits: DocumentHit[] = [];
  const savedSearches: SavedSearch[] = [];
  const opened: string[] = [];
  const shares: ShareLink[] = [];
  const subscriptions = new FakeCollection<Subscription>();
  const preferences = new FakeCollection<PreferenceDocument>();
  const exports = { created: [] as unknown[] };
  const deliveries: { reportId: string; recipients: number }[] = [];
  const channelChanges: { id: string; enabled: boolean }[] = [];

  const documentStore = {
    getById: (id: string) => Promise.resolve(documents.get(id)),
    getByNumber: (number: string) =>
      Promise.resolve([...documents.values()].find((doc) => doc.number === number)),
    getText: (id: string) => Promise.resolve(texts.get(id)),
  };
  const search = new SearchService(
    { search: (query) => Promise.resolve(hits.filter((hit) => hit.title.includes(query.term))) },
    documentStore,
  );
  const reports = new ReportService({
    get: (id) => Promise.resolve(id === sampleReport.id ? sampleReport : undefined),
    rows: () => Promise.resolve(sampleRows),
  });

  const dependencies: AppDependencies = {
    documents: {
      documents: documentStore,
      cards: new CardRenderer(),
      publicBaseUrl: 'https://archive.test',
    },
    search,
    attachmentsRoot: path.join(storageRoot, 'attachments'),
    templates: new TemplateStore(path.join(storageRoot, 'templates')),
    thumbnails: {
      documents: documentStore,
      renderer: { render: (_input, output) => writeFile(output, 'thumbnail') },
      originals: path.join(storageRoot, 'originals'),
      scratch: path.join(storageRoot, 'scratch'),
    },
    exports: {
      create: (caller, id, request) => {
        if (!documents.has(id)) {
          return Promise.resolve(undefined);
        }
        exports.created.push({ caller, id, request });
        return Promise.resolve('9d1e4c3b-6a5f-4e2d-8c7b-0a1b2c3d4e5f');
      },
      download: () => Promise.resolve({ kind: 'not-found' }),
    },
    feeds: {
      entries: (url) => {
        if (url.includes('unreachable')) {
          return Promise.reject(new Error('socket hang up'));
        }
        if (url.startsWith('http:')) {
          return Promise.reject(new FeedNotAllowedError(new URL(url).hostname));
        }
        return Promise.resolve([
          { id: 'urn:1', title: 'Maps', updated: '2026-09-30T10:00:00Z', link: null },
        ]);
      },
    },
    reports: {
      reports,
      summaries: {
        listByOwner: (owner) => Promise.resolve(owner === sampleReport.owner ? [sampleReport] : []),
      },
      schedules: { forReport: () => Promise.resolve([]) },
    },
    savedSearches: {
      list: (owner) => Promise.resolve(savedSearches.filter((saved) => saved.owner === owner)),
      get: (id, owner) =>
        Promise.resolve(savedSearches.find((saved) => saved.id === id && saved.owner === owner)),
      create: (saved) => {
        savedSearches.push(saved);
        return Promise.resolve();
      },
      markOpened: (id) => {
        opened.push(id);
        return Promise.resolve();
      },
    },
    shares: {
      shares: {
        create: (link) => {
          shares.push(link);
          return Promise.resolve();
        },
        findActive: (token, at) =>
          Promise.resolve(shares.find((link) => link.token === token && link.expiresAt > at)),
      },
      grants: new ShareGrants(15 * 60_000, () => now.getTime()),
      documents: documentStore,
      now: () => now,
    },
    subscriptions: {
      subscriptions: subscriptions as unknown as AppDependencies['subscriptions']['subscriptions'],
      reports,
      mailer: {
        deliver: (report, recipients) => {
          deliveries.push({ reportId: report.id, recipients: recipients.length });
          return Promise.resolve({ delivered: recipients.length, bounced: 0 });
        },
      },
      crm: {
        setEmailChannel: (id, enabled) => {
          channelChanges.push({ id, enabled });
          return Promise.resolve();
        },
      },
      publicBaseUrl: 'https://archive.test',
      now: () => now,
    },
    preferences: preferences as unknown as AppDependencies['preferences'],
    now: () => now,
  };

  const verifier = await createAccessTokenVerifier({
    issuer: testIssuer,
    audience: testAudience,
    publicKeyPem: tokens.publicKeyPem,
  });
  const app = createApp({
    dependencies,
    verifier,
    logger: options.logger ?? silentLogger,
    requireHttps: options.requireHttps ?? false,
    trustProxy: 'loopback',
  });
  const token = await tokens.issue({ scopes: Object.values(Scopes) });
  return {
    app,
    tokens,
    fullAccess: `Bearer ${token}`,
    storageRoot,
    documents,
    texts,
    hits,
    savedSearches,
    opened,
    shares,
    subscriptions,
    preferences,
    exports,
    deliveries,
    channelChanges,
  };
}
