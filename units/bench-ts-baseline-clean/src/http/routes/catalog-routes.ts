import { Router } from 'express';
import type { CatalogService } from '../../application/catalog/catalog-service.js';
import { createFromBody, listPage, withParam } from '../handlers.js';
import { binCodeParam, registerBinBody, registerSkuBody, skuCodeParam } from '../schemas.js';
import { requireScope, Scopes } from '../scopes.js';

export function catalogRoutes(catalog: CatalogService): Router {
  const read = requireScope(Scopes.stockRead);
  const write = requireScope(Scopes.stockWrite);

  return Router()
    .get(
      '/skus',
      read,
      listPage((page) => catalog.listSkus(page)),
    )
    .get(
      '/skus/:code',
      read,
      withParam('code', skuCodeParam, (code) => catalog.getSku(code)),
    )
    .post(
      '/skus',
      write,
      createFromBody(
        registerSkuBody,
        (sku) => catalog.registerSku(sku),
        (sku) => `/api/skus/${sku.code}`,
      ),
    )
    .get(
      '/bins',
      read,
      listPage((page) => catalog.listBins(page)),
    )
    .get(
      '/bins/:code',
      read,
      withParam('code', binCodeParam, (code) => catalog.getBin(code)),
    )
    .post(
      '/bins',
      write,
      createFromBody(
        registerBinBody,
        (bin) => catalog.registerBin(bin),
        (bin) => `/api/bins/${bin.code}`,
      ),
    );
}
