import type { Page, PageRequest } from '../paging.js';
import type { BinLocation, Sku } from './catalog.js';

export interface CatalogStore {
  /** Adds the SKU unless one with the same code exists; returns whether it was added. */
  addSku(sku: Sku): boolean;
  findSku(code: string): Sku | undefined;
  listSkus(request: PageRequest): Page<Sku>;
  /** Adds the bin unless one with the same code exists; returns whether it was added. */
  addBin(bin: BinLocation): boolean;
  findBin(code: string): BinLocation | undefined;
  listBins(request: PageRequest): Page<BinLocation>;
}
