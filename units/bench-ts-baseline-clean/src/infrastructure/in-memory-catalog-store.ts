import type { BinLocation, Sku } from '../application/catalog/catalog.js';
import type { CatalogStore } from '../application/catalog/catalog-store.js';
import { paginate, type Page, type PageRequest } from '../application/paging.js';

/** Holds the catalogue in process memory (ADR 0002). Listings are ordered by code. */
export class InMemoryCatalogStore implements CatalogStore {
  private readonly skus = new Map<string, Sku>();
  private readonly bins = new Map<string, BinLocation>();

  addSku(sku: Sku): boolean {
    return addIfAbsent(this.skus, sku);
  }

  findSku(code: string): Sku | undefined {
    return this.skus.get(code);
  }

  listSkus(request: PageRequest): Page<Sku> {
    return paginate(sortedByCode(this.skus), request);
  }

  addBin(bin: BinLocation): boolean {
    return addIfAbsent(this.bins, bin);
  }

  findBin(code: string): BinLocation | undefined {
    return this.bins.get(code);
  }

  listBins(request: PageRequest): Page<BinLocation> {
    return paginate(sortedByCode(this.bins), request);
  }
}

function addIfAbsent<T extends { readonly code: string }>(
  entries: Map<string, T>,
  entry: T,
): boolean {
  if (entries.has(entry.code)) {
    return false;
  }
  entries.set(entry.code, entry);
  return true;
}

function sortedByCode<T extends { readonly code: string }>(entries: Map<string, T>): T[] {
  return [...entries.values()].sort((a, b) => a.code.localeCompare(b.code));
}
