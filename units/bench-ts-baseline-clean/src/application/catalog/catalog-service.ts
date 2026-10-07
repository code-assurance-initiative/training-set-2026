import type { Logger } from 'pino';
import type { Page, PageRequest } from '../paging.js';
import { conflict, notFound, success, type Result } from '../result.js';
import type { BinLocation, Sku } from './catalog.js';
import type { CatalogStore } from './catalog-store.js';

export class CatalogService {
  constructor(
    private readonly store: CatalogStore,
    private readonly logger: Logger,
  ) {}

  registerSku(sku: Sku): Result<Sku> {
    if (!this.store.addSku(sku)) {
      return conflict(`SKU '${sku.code}' is already registered.`);
    }
    this.logger.info({ skuCode: sku.code }, 'Registered SKU');
    return success(sku);
  }

  getSku(code: string): Result<Sku> {
    const sku = this.store.findSku(code);
    return sku ? success(sku) : notFound(`SKU '${code}' does not exist.`);
  }

  listSkus(request: PageRequest): Page<Sku> {
    return this.store.listSkus(request);
  }

  registerBin(bin: BinLocation): Result<BinLocation> {
    if (!this.store.addBin(bin)) {
      return conflict(`Bin '${bin.code}' is already registered.`);
    }
    this.logger.info({ binCode: bin.code, zone: bin.zone }, 'Registered bin');
    return success(bin);
  }

  getBin(code: string): Result<BinLocation> {
    const bin = this.store.findBin(code);
    return bin ? success(bin) : notFound(`Bin '${code}' does not exist.`);
  }

  listBins(request: PageRequest): Page<BinLocation> {
    return this.store.listBins(request);
  }

  /** Confirms that a SKU and a bin both exist, and returns the bin (whose capacity callers need). */
  resolveLocation(skuCode: string, binCode: string): Result<BinLocation> {
    if (!this.store.findSku(skuCode)) {
      return notFound(`SKU '${skuCode}' does not exist.`);
    }
    return this.getBin(binCode);
  }
}
