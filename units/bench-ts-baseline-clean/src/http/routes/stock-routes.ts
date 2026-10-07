import { Router, type RequestHandler } from 'express';
import type { StockKey, StockLevel } from '../../application/inventory/inventory.js';
import type { StockService } from '../../application/inventory/stock-service.js';
import type { Result } from '../../application/result.js';
import { withParam } from '../handlers.js';
import { sendResult } from '../problem.js';
import { parseInput, skuCodeParam, stockMovementBody } from '../schemas.js';
import { requireScope, Scopes } from '../scopes.js';

export function stockRoutes(stock: StockService): Router {
  const write = requireScope(Scopes.stockWrite);

  return Router()
    .get(
      '/stock/:sku',
      requireScope(Scopes.stockRead),
      withParam('sku', skuCodeParam, (skuCode) => stock.getLevels(skuCode)),
    )
    .post(
      '/stock/:sku/receipts',
      write,
      stockMovement((key, quantity) => stock.receive(key, quantity)),
    )
    .post(
      '/stock/:sku/counts',
      write,
      stockMovement((key, quantity) => stock.count(key, quantity)),
    );
}

/** Applies a quantity, given in the body, to the SKU in the path and the bin in the body. */
function stockMovement(
  apply: (key: StockKey, quantity: number) => Result<StockLevel>,
): RequestHandler {
  return (req, res) => {
    const skuCode = parseInput(skuCodeParam, req.params.sku, res);
    const movement =
      skuCode === undefined ? undefined : parseInput(stockMovementBody, req.body, res);
    if (skuCode !== undefined && movement) {
      sendResult(res, apply({ skuCode, binCode: movement.binCode }, movement.quantity));
    }
  };
}
