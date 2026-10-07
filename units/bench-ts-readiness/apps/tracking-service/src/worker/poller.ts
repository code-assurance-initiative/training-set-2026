import type { Logger } from "pino";
import type { ParcelStore } from "../parcels/parcel-store.js";
import type { TrackingService } from "../parcels/tracking-service.js";
import { CarrierHttpError, type CarrierClient } from "./carrier-client.js";
import { repeatUntilAborted } from "./retry.js";

export interface PollerOptions {
  intervalMs: number;
  batchSize: number;
}

/** Polls the carriers for every parcel that is not final yet and records what they report. */
export class CarrierPoller {
  private readonly carriers: ReadonlyMap<string, CarrierClient>;

  constructor(
    private readonly store: ParcelStore,
    private readonly service: TrackingService,
    carriers: readonly CarrierClient[],
    private readonly logger: Logger,
    private readonly options: PollerOptions,
  ) {
    this.carriers = new Map(carriers.map((c) => [c.carrierCode, c]));
  }

  /** Polls until `signal` aborts, waiting `intervalMs` after each round. */
  run(signal: AbortSignal): Promise<void> {
    return repeatUntilAborted(() => this.pollOnce(signal), this.options.intervalMs, signal);
  }

  /** One round over a batch of active parcels; returns the number of new events recorded. */
  async pollOnce(signal: AbortSignal): Promise<number> {
    const parcels = await this.store.listActive(this.options.batchSize);
    let recorded = 0;
    for (const parcel of parcels) {
      if (signal.aborted) {
        break;
      }
      const carrier = this.carriers.get(parcel.carrier);
      if (!carrier) {
        this.logger.warn({ carrier: parcel.carrier, parcelId: parcel.id }, "no client for carrier");
        continue;
      }
      try {
        const events = await carrier.fetchEvents(parcel.trackingNumber);
        recorded += await this.service.applyCarrierEvents(parcel, events);
      } catch (error) {
        if (error instanceof CarrierHttpError) {
          this.logger.warn(
            `carrier ${error.carrierCode} answered ${error.status} for a tracking request`,
          );
        } else {
          this.logger.error({ err: error, parcelId: parcel.id }, "polling a parcel failed");
        }
      }
    }
    return recorded;
  }
}
