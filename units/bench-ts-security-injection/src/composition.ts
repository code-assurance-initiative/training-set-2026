import knex, { type Knex } from 'knex';
import { MongoClient } from 'mongodb';
import pg from 'pg';
import type { Logger } from 'pino';
import type { AppDependencies } from './app.js';
import type { AppConfig } from './config.js';
import { DocumentConverter } from './conversion/document-converter.js';
import { ThumbnailRenderer } from './conversion/thumbnail-renderer.js';
import { CardRenderer } from './documents/card-renderer.js';
import { DocumentRepository } from './documents/document-repository.js';
import { ExportAuditLog } from './exports/export-audit-log.js';
import { ExportService } from './exports/export-service.js';
import { ExportStore } from './exports/export-store.js';
import { PartnerFeedClient } from './feeds/partner-feed-client.js';
import type { PreferenceDocument } from './preferences/preferences.js';
import { ReportRepository } from './reports/report-repository.js';
import { ReportService } from './reports/report-service.js';
import { ScheduleRepository } from './reports/schedule-repository.js';
import { SavedSearchStore } from './saved-searches/saved-search-store.js';
import { DocumentSearchRepository } from './search/document-search-repository.js';
import { SearchService } from './search/search-service.js';
import { ShareGrants } from './shares/share-grants.js';
import { ShareStore } from './shares/share-store.js';
import { CrmClient } from './subscriptions/crm-client.js';
import { HttpMailRelay } from './subscriptions/mail-transport.js';
import { ReportMailer } from './subscriptions/report-mailer.js';
import type { Subscription } from './subscriptions/subscription.js';
import { TemplateStore } from './templates/template-store.js';

export interface Infrastructure {
  readonly dependencies: AppDependencies;
  readonly shareStore: ShareStore;
  close(): Promise<void>;
}

/** Connects the stores and wires every feature (the composition root). */
export function compose(config: AppConfig, logger: Logger): Infrastructure {
  const pool = new pg.Pool({ connectionString: config.databaseUrl, max: 10 });
  const db: Knex = knex({ client: 'pg', connection: config.databaseUrl, pool: { min: 0, max: 5 } });
  const mongo = new MongoClient(config.mongoUrl);
  const archive = mongo.db();
  const now = () => new Date();

  const documents = new DocumentRepository(pool);
  const search = new SearchService(new DocumentSearchRepository(pool), documents);
  const reportRepository = new ReportRepository(db);
  const reports = new ReportService(reportRepository);
  const shareStore = new ShareStore(pool);
  const crm = new CrmClient(config.crmBaseUrl);
  const subscriptions = archive.collection<Subscription>('subscriptions');

  const dependencies: AppDependencies = {
    documents: { documents, cards: new CardRenderer(), publicBaseUrl: config.publicBaseUrl },
    search,
    attachmentsRoot: config.storage.attachments,
    templates: new TemplateStore(config.storage.templates),
    thumbnails: {
      documents,
      renderer: new ThumbnailRenderer(config.conversion),
      originals: config.storage.originals,
      scratch: `${config.storage.exports}/thumbnails`,
    },
    exports: new ExportService(
      documents,
      new DocumentConverter(config.conversion),
      new ExportStore(pool),
      new ExportAuditLog(config.storage.auditLog),
      { originals: config.storage.originals, exports: config.storage.exports },
      logger.child({ component: 'exports' }),
    ),
    feeds: new PartnerFeedClient(config.partnerFeedHosts),
    reports: {
      reports,
      summaries: reportRepository,
      schedules: new ScheduleRepository(db),
    },
    savedSearches: new SavedSearchStore(pool),
    shares: { shares: shareStore, grants: new ShareGrants(), documents, now },
    subscriptions: {
      subscriptions,
      reports,
      mailer: new ReportMailer(
        new HttpMailRelay(config.mailRelayUrl),
        crm,
        config.pseudonymKey,
        config.publicBaseUrl,
        logger.child({ component: 'report-mailer' }),
      ),
      crm,
      publicBaseUrl: config.publicBaseUrl,
      now,
    },
    preferences: archive.collection<PreferenceDocument>('preferences'),
    now,
  };

  return {
    dependencies,
    shareStore,
    close: async () => {
      await Promise.all([pool.end(), db.destroy(), mongo.close()]);
    },
  };
}
