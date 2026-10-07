import { createContext, useContext } from 'react';
import type { CatalogueClient } from '../api/catalogueClient';

export const CatalogueClientContext = createContext<CatalogueClient | null>(null);

export function useCatalogueClient(): CatalogueClient {
  const client = useContext(CatalogueClientContext);
  if (!client) {
    throw new Error('useCatalogueClient must be used inside a CatalogueClientContext provider');
  }
  return client;
}
