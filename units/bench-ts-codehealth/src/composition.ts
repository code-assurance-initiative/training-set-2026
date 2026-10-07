import { randomUUID } from 'node:crypto';
import { join } from 'node:path';
import type { Logger } from 'pino';
import { CarrierAccountManager } from './application/accounts/carrier-account-manager.js';
import { LabelService } from './application/labels/label-service.js';
import { ZplLabelRenderer } from './application/labels/zpl-label-renderer.js';
import { CutoffCalendar } from './application/pricing/cutoff-calendar.js';
import { MultiParcelQuoter } from './application/pricing/multi-parcel-quoter.js';
import { QuoteService } from './application/pricing/quote-service.js';
import { RateCalculator } from './application/pricing/rate-calculator.js';
import type { RateCardSource } from './application/pricing/rate-card.js';
import { RemoteAreaLookup } from './application/pricing/remote-area-lookup.js';
import { SurchargePolicy, type SurchargeTable } from './application/pricing/surcharge-policy.js';
import type { AppConfig } from './config.js';
import type { CarrierDirectory } from './domain/ports/carrier-directory.js';
import type { LabelStore } from './domain/ports/label-store.js';
import type { Mailer } from './domain/ports/mailer.js';
import { CarrierRegistry } from './infrastructure/carriers/carrier-registry.js';
import { LabelArchive } from './infrastructure/labels/label-archive.js';
import { PickupDirectoryMailer } from './infrastructure/mail/pickup-directory-mailer.js';
import { InMemoryShipmentRepository } from './infrastructure/persistence/in-memory-shipment-repository.js';
import { RateCardCache } from './infrastructure/rates/rate-card-cache.js';
import {
  requiresCustomsDeclaration,
  zoneFor,
} from './infrastructure/zones/country-zones.generated.js';

export const surchargeTable: SurchargeTable = {
  currency: 'DKK',
  oversizeLongestSideCm: 120,
  oversizeFee: 2_500,
  heavyKilograms: 25,
  heavyFee: 3_000,
  insurancePercent: 1.5,
  insuranceFreeUpTo: 50_000,
  remoteAreaFee: 4_000,
  saturdayFee: 9_500,
};

export interface Services {
  readonly quotes: QuoteService;
  readonly labels: LabelService;
  readonly accounts: CarrierAccountManager;
  dispose(): void;
}

/** What the composition needs from the outside; tests replace the carriers, store and mailer. */
export interface Dependencies {
  readonly rateCards: RateCardSource & { dispose?(): void; refresh?(): Promise<void> };
  readonly carriers: CarrierDirectory;
  readonly store: LabelStore;
  readonly mailer: Mailer;
  readonly clock: () => Date;
  readonly newId: () => string;
}

export function productionDependencies(config: AppConfig, logger: Logger): Dependencies {
  const cache = new RateCardCache(config.rateCardUrl, logger.child({ component: 'rate-cards' }));
  return {
    rateCards: cache,
    carriers: new CarrierRegistry(
      { alder: config.carriers.alder, corvid: config.carriers.corvid },
      logger.child({ component: 'carriers' }),
    ),
    store: new LabelArchive(config.labels.archiveDir, logger.child({ component: 'archive' })),
    mailer: new PickupDirectoryMailer(config.mailPickupDir),
    clock: () => new Date(),
    newId: randomUUID,
  };
}

/** Wires the application services (the composition root). */
export function composeServices(
  config: Pick<AppConfig, 'carriers' | 'labels' | 'webhookSecret'>,
  dependencies: Dependencies,
  logger: Logger,
): Services {
  const calculator = new RateCalculator(
    dependencies.rateCards,
    new SurchargePolicy(surchargeTable),
    new RemoteAreaLookup(logger.child({ component: 'remote-areas' })),
    zoneFor,
  );
  const labels = new LabelService(
    new InMemoryShipmentRepository(),
    dependencies.carriers,
    calculator,
    new ZplLabelRenderer(),
    dependencies.store,
    dependencies.mailer,
    new CutoffCalendar(config.carriers.cutoffs, logger.child({ component: 'cutoffs' })),
    requiresCustomsDeclaration,
    dependencies.clock,
    dependencies.newId,
    logger.child({ component: 'labels' }),
    {
      fromAddress: config.labels.fromAddress,
      indexPath: join(config.labels.archiveDir, 'index.json'),
      retentionDays: config.labels.retentionDays,
      batchCapacity: 50,
    },
  );
  return {
    quotes: new QuoteService(
      calculator,
      new MultiParcelQuoter('DKK', 500, 2_000, 120),
      logger.child({ component: 'quotes' }),
    ),
    labels,
    accounts: new CarrierAccountManager(Buffer.from(config.webhookSecret, 'utf8')),
    dispose: () => dependencies.rateCards.dispose?.(),
  };
}
