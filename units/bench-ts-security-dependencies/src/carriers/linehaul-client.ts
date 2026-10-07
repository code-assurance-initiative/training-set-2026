import axiosPackage, { type AxiosInstance } from 'axios';
import { z } from 'zod';

// axios 0.21 is a CommonJS module whose typings describe the client as its `default` export.
const axios = axiosPackage.default;

export interface LinehaulOptions {
  readonly baseUrl: string;
  readonly apiKey: string;
  readonly timeoutMs?: number;
}

export interface QuoteRequest {
  readonly depotId: string;
  readonly serviceDate: string;
  readonly parcels: number;
  readonly totalWeightKg: number;
}

const quoteSchema = z.object({
  quoteId: z.string().min(1),
  priceMinor: z.number().int().nonnegative(),
  currency: z.string().length(3),
  validUntil: z.iso.datetime(),
});

export type Quote = z.infer<typeof quoteSchema>;

export class LinehaulError extends Error {
  constructor(message: string, options?: ErrorOptions) {
    super(message, options);
    this.name = 'LinehaulError';
  }
}

export interface Linehaul {
  quote(request: QuoteRequest): Promise<Quote>;
  /** The carrier's label for one parcel, as PDF bytes. */
  fetchLabel(trackingNumber: string): Promise<Uint8Array>;
}

/** The linehaul carrier's partner API: trunk-route quotes and parcel labels. */
export class LinehaulClient implements Linehaul {
  readonly #http: AxiosInstance;

  constructor(options: LinehaulOptions) {
    this.#http = axios.create({
      baseURL: options.baseUrl,
      timeout: options.timeoutMs ?? 5_000,
      maxRedirects: 0,
      headers: { 'X-Api-Key': options.apiKey, Accept: 'application/json' },
    });
  }

  async quote(request: QuoteRequest): Promise<Quote> {
    const response = await this.#call(() => this.#http.post<unknown>('/v2/quotes', request));
    const parsed = quoteSchema.safeParse(response);
    if (!parsed.success) {
      throw new LinehaulError('The carrier returned a quote this service cannot read.');
    }
    return parsed.data;
  }

  async fetchLabel(trackingNumber: string): Promise<Uint8Array> {
    const body = await this.#call(() =>
      this.#http.get<ArrayBuffer>(`/v2/labels/${encodeURIComponent(trackingNumber)}`, {
        responseType: 'arraybuffer',
        headers: { Accept: 'application/pdf' },
      }),
    );
    return new Uint8Array(body);
  }

  async #call<T>(send: () => Promise<{ data: T }>): Promise<T> {
    try {
      return (await send()).data;
    } catch (error) {
      const status = axios.isAxiosError(error) ? error.response?.status : undefined;
      throw new LinehaulError(
        status === undefined
          ? 'The carrier could not be reached.'
          : `The carrier answered with HTTP ${status}.`,
        { cause: error },
      );
    }
  }
}
