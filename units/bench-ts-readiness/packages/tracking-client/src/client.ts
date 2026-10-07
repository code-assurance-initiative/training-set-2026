import { TrackingApiError } from "./errors.js";
import type { CreateParcel, DeliveryStats, Parcel, Redirect, ShareLink } from "./types.js";

export interface TrackingClientOptions {
  /** The API's base URL, e.g. `https://tracking.example.com/`. */
  baseUrl: string;
  /** Returns a current access token; called for every request so the host can refresh it. */
  token: () => string | Promise<string>;
  /** Defaults to the global fetch. Timeouts and retries are the caller's: pass a signal per call. */
  fetch?: typeof fetch;
}

export interface CallOptions {
  signal?: AbortSignal;
}

export class TrackingClient {
  private readonly fetchImpl: typeof fetch;

  constructor(private readonly options: TrackingClientOptions) {
    this.fetchImpl = options.fetch ?? fetch;
  }

  createParcel(parcel: CreateParcel, call: CallOptions = {}): Promise<Parcel> {
    return this.request<Parcel>("POST", "v1/parcels", call, parcel);
  }

  getParcel(trackingNumber: string, call: CallOptions = {}): Promise<Parcel> {
    return this.request<Parcel>("GET", `v1/parcels/${encodeURIComponent(trackingNumber)}`, call);
  }

  createShareLink(trackingNumber: string, call: CallOptions = {}): Promise<ShareLink> {
    return this.request<ShareLink>(
      "POST",
      `v1/parcels/${encodeURIComponent(trackingNumber)}/share-link`,
      call,
    );
  }

  async redirect(
    trackingNumber: string,
    redirect: Redirect,
    call: CallOptions = {},
  ): Promise<void> {
    await this.request<undefined>(
      "POST",
      `v1/parcels/${encodeURIComponent(trackingNumber)}/redirect`,
      call,
      redirect,
    );
  }

  deliveryStats(merchantId: string, days = 30, call: CallOptions = {}): Promise<DeliveryStats> {
    return this.request<DeliveryStats>(
      "GET",
      `v1/merchants/${encodeURIComponent(merchantId)}/delivery-stats?days=${days}`,
      call,
    );
  }

  private async request<T>(
    method: string,
    path: string,
    call: CallOptions,
    body?: unknown,
  ): Promise<T> {
    const headers: Record<string, string> = {
      accept: "application/json",
      authorization: `Bearer ${await this.options.token()}`,
    };
    if (body !== undefined) {
      headers["content-type"] = "application/json";
    }
    const response = await this.fetchImpl(new URL(path, this.options.baseUrl), {
      method,
      headers,
      ...(body === undefined ? {} : { body: JSON.stringify(body) }),
      ...(call.signal ? { signal: call.signal } : {}),
    });
    if (!response.ok) {
      const problem = (await response.json().catch(() => ({}))) as {
        title?: string;
        detail?: string;
      };
      throw new TrackingApiError(
        response.status,
        problem.title ?? response.statusText,
        problem.detail,
      );
    }
    return (response.status === 204 ? undefined : await response.json()) as T;
  }
}
