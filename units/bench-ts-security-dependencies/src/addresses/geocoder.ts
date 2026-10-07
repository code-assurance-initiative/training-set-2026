import { Client, Status, type GeocodeResponse } from '@googlemaps/google-maps-services-js';
import type { Address, Coordinates } from '../dispatch/dispatch-run.js';

export interface Geocoder {
  /** The rooftop or street position of `address`, or null when the provider does not know it. */
  locate(address: Address): Promise<Coordinates | null>;
}

export class GeocodingError extends Error {
  constructor(
    readonly status: string,
    options?: ErrorOptions,
  ) {
    super(`Geocoding failed with status ${status}.`, options);
    this.name = 'GeocodingError';
  }
}

/** Geocodes Danish delivery addresses with the Google Maps Geocoding API. */
export class MapsGeocoder implements Geocoder {
  readonly #client: Client;

  constructor(
    private readonly apiKey: string,
    client: Client = new Client(),
    private readonly timeoutMs = 3_000,
  ) {
    this.#client = client;
  }

  async locate(address: Address): Promise<Coordinates | null> {
    let response: GeocodeResponse;
    try {
      response = await this.#client.geocode({
        params: {
          address: `${address.street}, ${address.postcode} ${address.city}`,
          components: { country: 'DK' },
          key: this.apiKey,
        },
        timeout: this.timeoutMs,
      });
    } catch (error) {
      // The client rejects every answer but OK and ZERO_RESULTS; the provider's status is in the body.
      throw new GeocodingError(providerStatus(error) ?? 'UNREACHABLE', { cause: error });
    }
    const { status, results } = response.data;
    if (status === Status.ZERO_RESULTS) {
      return null;
    }
    const first = results[0];
    if (status !== Status.OK || first === undefined) {
      throw new GeocodingError(status);
    }
    const { lat, lng } = first.geometry.location;
    return { lat, lng };
  }
}

function providerStatus(error: unknown): string | undefined {
  if (typeof error !== 'object' || error === null || !('response' in error)) {
    return undefined;
  }
  const { response } = error as { response?: { data?: { status?: unknown } } };
  const status = response?.data?.status;
  return typeof status === 'string' ? status : undefined;
}
