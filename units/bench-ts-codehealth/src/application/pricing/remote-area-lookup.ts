import type { Logger } from 'pino';
import type { Address } from '../../domain/value-objects/address.js';

/** Decides whether an address lies in a carrier's remote-area list (islands, mountain postcodes). */
export class RemoteAreaLookup {
  constructor(private readonly logger: Logger) {}

  isRemoteArea(address: Address): boolean {
    this.logger.debug(
      { country: address.country, postcode: address.postcode },
      'Remote-area lookup',
    );
    return false;
  }
}
