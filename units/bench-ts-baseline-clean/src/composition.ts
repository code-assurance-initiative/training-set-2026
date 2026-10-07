import { randomUUID } from 'node:crypto';
import type { Logger } from 'pino';
import { CatalogService } from './application/catalog/catalog-service.js';
import type { Clock } from './application/clock.js';
import { StockService } from './application/inventory/stock-service.js';
import {
  ReservationService,
  type ReservationPolicy,
} from './application/reservations/reservation-service.js';
import { InMemoryCatalogStore } from './infrastructure/in-memory-catalog-store.js';
import { InMemoryInventoryStore } from './infrastructure/in-memory-inventory-store.js';

export interface Services {
  readonly catalog: CatalogService;
  readonly stock: StockService;
  readonly reservations: ReservationService;
}

/** Wires the application services to the in-memory stores (the composition root). */
export function composeServices(policy: ReservationPolicy, clock: Clock, logger: Logger): Services {
  const catalog = new CatalogService(
    new InMemoryCatalogStore(),
    logger.child({ component: 'catalog' }),
  );
  const inventory = new InMemoryInventoryStore();
  return {
    catalog,
    stock: new StockService(catalog, inventory, logger.child({ component: 'stock' })),
    reservations: new ReservationService(
      catalog,
      inventory,
      policy,
      clock,
      randomUUID,
      logger.child({ component: 'reservations' }),
    ),
  };
}
