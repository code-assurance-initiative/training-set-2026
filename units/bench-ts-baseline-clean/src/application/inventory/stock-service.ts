import type { Logger } from 'pino';
import type { CatalogService } from '../catalog/catalog-service.js';
import { conflict, invalid, success, type Result } from '../result.js';
import type { StockKey, StockLevel } from './inventory.js';
import type { InventoryStore } from './inventory-store.js';

export class StockService {
  constructor(
    private readonly catalog: CatalogService,
    private readonly inventory: InventoryStore,
    private readonly logger: Logger,
  ) {}

  getLevels(skuCode: string): Result<readonly StockLevel[]> {
    const sku = this.catalog.getSku(skuCode);
    return sku.ok ? success(this.inventory.levelsForSku(skuCode)) : sku;
  }

  /** Books goods received into a bin, up to the bin's capacity. */
  receive(key: StockKey, quantity: number): Result<StockLevel> {
    if (quantity <= 0) {
      return invalid('A receipt must be for a positive quantity.');
    }
    const bin = this.catalog.resolveLocation(key.skuCode, key.binCode);
    if (!bin.ok) {
      return bin;
    }
    const level = this.inventory.levelOf(key);
    if (level.onHand + quantity > bin.value.capacity) {
      return conflict(
        `Receiving ${quantity} would exceed the capacity (${bin.value.capacity}) of bin '${key.binCode}'.`,
      );
    }
    return this.apply('receipt', key, quantity, level.onHand + quantity);
  }

  /** Replaces the on-hand quantity with a physical count. The count may not drop below what is reserved. */
  count(key: StockKey, counted: number): Result<StockLevel> {
    if (counted < 0) {
      return invalid('A count cannot be negative.');
    }
    const bin = this.catalog.resolveLocation(key.skuCode, key.binCode);
    if (!bin.ok) {
      return bin;
    }
    if (counted > bin.value.capacity) {
      return invalid(
        `A count of ${counted} exceeds the capacity (${bin.value.capacity}) of bin '${key.binCode}'.`,
      );
    }
    const level = this.inventory.levelOf(key);
    if (counted < level.reserved) {
      return conflict(
        `A count of ${counted} is below the ${level.reserved} units reserved in bin '${key.binCode}'.`,
      );
    }
    return this.apply('count', key, counted, counted);
  }

  private apply(
    movement: 'receipt' | 'count',
    key: StockKey,
    quantity: number,
    onHand: number,
  ): Result<StockLevel> {
    this.inventory.setOnHand(key, onHand);
    const level = this.inventory.levelOf(key);
    this.logger.info(
      { movement, ...key, quantity, onHand: level.onHand },
      'Applied stock movement',
    );
    return success(level);
  }
}
