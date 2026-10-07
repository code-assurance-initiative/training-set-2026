import type { CarrierEvent } from "../../src/parcels/tracking-service.js";

export interface RecordedRequest {
  url: string;
  headers: Headers;
  aborted: boolean;
}

/**
 * A fetch stand-in for the carrier API: answers each tracking request with the events queued for that tracking
 * number (or a scripted status), and records every request.
 */
export class FakeCarrier {
  readonly requests: RecordedRequest[] = [];
  private readonly events = new Map<string, CarrierEvent[]>();
  private readonly statuses: number[] = [];

  report(trackingNumber: string, ...events: CarrierEvent[]): void {
    this.events.set(trackingNumber, [...(this.events.get(trackingNumber) ?? []), ...events]);
  }

  /** The next answers' HTTP statuses, in order, before normal answers resume. */
  failWith(...statuses: number[]): void {
    this.statuses.push(...statuses);
  }

  readonly fetch: typeof fetch = (input, init) => {
    const url = input instanceof Request ? input.url : input.toString();
    this.requests.push({
      url,
      headers: new Headers(init?.headers),
      aborted: init?.signal?.aborted ?? false,
    });
    if (init?.signal?.aborted) {
      return Promise.reject(init.signal.reason as Error);
    }
    const status = this.statuses.shift();
    if (status !== undefined) {
      return Promise.resolve(new Response(JSON.stringify({ title: "scripted" }), { status }));
    }
    const trackingNumber = decodeURIComponent(/shipments\/([^/]+)\/events/.exec(url)?.[1] ?? "");
    const events = (this.events.get(trackingNumber) ?? []).map((e) => ({
      code: e.code,
      occurredAt: e.occurredAt.toISOString(),
    }));
    return Promise.resolve(Response.json({ events }));
  };
}
