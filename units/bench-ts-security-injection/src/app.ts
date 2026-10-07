import { randomUUID } from 'node:crypto';
import express, { type Express } from 'express';
import helmet from 'helmet';
import type { Logger } from 'pino';
import { pinoHttp } from 'pino-http';
import { attachmentRoutes } from './attachments/attachment-routes.js';
import { thumbnailRoutes, type ThumbnailRouteDependencies } from './conversion/thumbnail-routes.js';
import { documentRoutes, type DocumentRouteDependencies } from './documents/document-routes.js';
import { exportRoutes } from './exports/export-routes.js';
import type { ExportService } from './exports/export-service.js';
import { feedRoutes } from './feeds/feed-routes.js';
import type { PartnerFeedClient } from './feeds/partner-feed-client.js';
import { healthRoutes } from './health/health-routes.js';
import { authenticate, type AccessTokenVerifier } from './http/authentication.js';
import { errorHandler, notFoundHandler } from './http/errors.js';
import { requireHttps } from './http/transport-security.js';
import { importRoutes } from './imports/import-routes.js';
import { preferenceRoutes } from './preferences/preference-routes.js';
import type { PreferenceCollection } from './preferences/preferences.js';
import { reportRoutes, type ReportRouteDependencies } from './reports/report-routes.js';
import { savedSearchRoutes } from './saved-searches/saved-search-routes.js';
import type { SavedSearchStore } from './saved-searches/saved-search-store.js';
import { searchRoutes } from './search/search-routes.js';
import type { SearchService } from './search/search-service.js';
import {
  shareAdminRoutes,
  sharePublicRoutes,
  type ShareRouteDependencies,
} from './shares/share-routes.js';
import {
  subscriptionRoutes,
  type SubscriptionRouteDependencies,
} from './subscriptions/subscription-routes.js';
import { unsubscribeRoutes } from './subscriptions/unsubscribe-routes.js';
import type { TemplateStore } from './templates/template-store.js';
import { templateRoutes } from './templates/template-routes.js';

export interface AppDependencies {
  readonly documents: DocumentRouteDependencies;
  readonly search: SearchService;
  readonly attachmentsRoot: string;
  readonly templates: Pick<TemplateStore, 'read'>;
  readonly thumbnails: ThumbnailRouteDependencies;
  readonly exports: Pick<ExportService, 'create' | 'download'>;
  readonly feeds: Pick<PartnerFeedClient, 'entries'>;
  readonly reports: ReportRouteDependencies;
  readonly savedSearches: Pick<SavedSearchStore, 'list' | 'get' | 'create' | 'markOpened'>;
  readonly shares: ShareRouteDependencies;
  readonly subscriptions: SubscriptionRouteDependencies;
  readonly preferences: PreferenceCollection;
  readonly now: () => Date;
}

export interface AppOptions {
  readonly dependencies: AppDependencies;
  readonly verifier: AccessTokenVerifier;
  readonly logger: Logger;
  /** Refuse plain-HTTP requests (everywhere except local development and tests). */
  readonly requireHttps: boolean;
  readonly trustProxy: string;
}

export function createApp(options: AppOptions): Express {
  const deps = options.dependencies;
  const app = express();
  app.disable('x-powered-by');
  app.set('trust proxy', options.trustProxy);

  app.use(
    pinoHttp({
      logger: options.logger,
      genReqId: (_req, res) => {
        const id = randomUUID();
        res.setHeader('X-Request-Id', id);
        return id;
      },
    }),
  );
  app.use(
    helmet({
      contentSecurityPolicy: {
        useDefaults: false,
        directives: { defaultSrc: ["'none'"], frameAncestors: ["'none'"] },
      },
    }),
  );
  app.use(express.json({ limit: '256kb' }));

  app.use(healthRoutes());
  app.use(
    requireHttps(options.requireHttps),
    unsubscribeRoutes(deps.subscriptions.subscriptions),
    sharePublicRoutes(deps.shares),
  );
  app.use(
    '/api',
    requireHttps(options.requireHttps),
    authenticate(options.verifier),
    searchRoutes(deps.search),
    documentRoutes(deps.documents),
    attachmentRoutes(deps.attachmentsRoot),
    thumbnailRoutes(deps.thumbnails),
    templateRoutes(deps.templates),
    exportRoutes(deps.exports),
    importRoutes(),
    feedRoutes(deps.feeds),
    reportRoutes(deps.reports),
    savedSearchRoutes({ store: deps.savedSearches, search: deps.search }),
    shareAdminRoutes(deps.shares),
    subscriptionRoutes(deps.subscriptions),
    preferenceRoutes(deps.preferences, deps.now),
  );

  app.use(notFoundHandler);
  app.use(errorHandler);
  return app;
}
