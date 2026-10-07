import { z } from "zod";
import type { CarrierEvent } from "../parcels/tracking-service.js";
import { withRetry, type RetryPolicy } from "./retry.js";

const eventsResponse = z.object({
  events: z.array(
    z.object({ code: z.string().min(2).max(8), occurredAt: z.iso.datetime({ offset: true }) }),
  ),
});

export class CarrierHttpError extends Error {
  constructor(
    readonly status: number,
    readonly carrierCode: string,
  ) {
    super(`Carrier API answered ${status}`);
    this.name = "CarrierHttpError";
  }

  get transient(): boolean {
    return this.status === 429 || this.status >= 500;
  }
}

const isTransient = (error: unknown): boolean =>
  error instanceof CarrierHttpError
    ? error.transient
    : error instanceof Error && (error.name === "TimeoutError" || error instanceof TypeError);

export interface CarrierClientOptions {
  carrierCode: string;
  baseUrl: string;
  apiKey: string;
  timeoutMs: number;
  retry: RetryPolicy;
  fetch?: typeof fetch;
}

/** The carrier tracking API: every attempt bounded by a timeout, transient failures retried with backoff. */
export class CarrierClient {
  private readonly fetchImpl: typeof fetch;

  constructor(private readonly options: CarrierClientOptions) {
    this.fetchImpl = options.fetch ?? fetch;
  }

  get carrierCode(): string {
    return this.options.carrierCode;
  }

  fetchEvents(trackingNumber: string, signal?: AbortSignal): Promise<CarrierEvent[]> {
    const url = new URL(
      `v2/shipments/${encodeURIComponent(trackingNumber)}/events`,
      this.options.baseUrl,
    );
    return withRetry(
      async () => {
        const response = await this.fetchImpl(url, {
          headers: { accept: "application/json", "x-api-key": this.options.apiKey },
          signal: this.attemptSignal(signal),
        });
        if (!response.ok) {
          throw new CarrierHttpError(response.status, this.options.carrierCode);
        }
        const body = eventsResponse.parse(await response.json());
        return body.events.map((e) => ({ code: e.code, occurredAt: new Date(e.occurredAt) }));
      },
      this.options.retry,
      isTransient,
      signal,
    );
  }

  /** One unretried request, for readiness checks. */
  async ping(signal?: AbortSignal): Promise<void> {
    const response = await this.fetchImpl(new URL("v2/status", this.options.baseUrl), {
      headers: { "x-api-key": this.options.apiKey },
      signal: this.attemptSignal(signal),
    });
    if (!response.ok) {
      throw new CarrierHttpError(response.status, this.options.carrierCode);
    }
  }

  private attemptSignal(caller?: AbortSignal): AbortSignal {
    const timeout = AbortSignal.timeout(this.options.timeoutMs);
    return caller ? AbortSignal.any([caller, timeout]) : timeout;
  }
}
