import type { Clock } from "../parcels/tracking-service.js";
import type { DeliveryLog } from "./delivery-log.js";
import type { PendingWebhook, WebhookOutbox } from "./webhook-outbox.js";
import { repeatUntilAborted } from "./retry.js";
import { signatureHeader } from "./webhook-signing.js";

export interface DispatcherOptions {
  signingSecret: string;
  batchSize: number;
  clock: Clock;
  fetch?: typeof fetch;
}

/** Delivers queued webhooks to merchants, one at a time, signing each body. */
export class WebhookDispatcher {
  private readonly fetchImpl: typeof fetch;

  constructor(
    private readonly outbox: WebhookOutbox,
    private readonly log: DeliveryLog,
    private readonly options: DispatcherOptions,
  ) {
    this.fetchImpl = options.fetch ?? fetch;
  }

  /** Delivers what is due every `intervalMs` until `signal` aborts. */
  run(signal: AbortSignal, intervalMs: number): Promise<void> {
    return repeatUntilAborted(() => this.dispatchDue(signal), intervalMs, signal);
  }

  /** Delivers what is due; returns how many webhooks were accepted by their merchants. */
  async dispatchDue(signal: AbortSignal): Promise<number> {
    const due = await this.outbox.due(this.options.clock(), this.options.batchSize);
    let delivered = 0;
    for (const webhook of due) {
      if (signal.aborted) {
        break;
      }
      if (await this.deliver(webhook)) {
        delivered++;
      }
    }
    return delivered;
  }

  private async deliver(webhook: PendingWebhook): Promise<boolean> {
    const timestamp = Math.floor(this.options.clock().getTime() / 1000);
    let statusCode: number | null = null;
    let failure: string | null = null;
    try {
      const response = await this.fetchImpl(webhook.url, {
        method: "POST",
        headers: {
          "content-type": "application/json",
          "parcel-signature": signatureHeader(
            this.options.signingSecret,
            timestamp,
            webhook.payload,
          ),
        },
        body: webhook.payload,
      });
      statusCode = response.status;
    } catch (error) {
      failure = error instanceof Error ? error.message : String(error);
    }

    const now = this.options.clock();
    await this.log.record({ outboxId: webhook.id, statusCode, error: failure, attemptedAt: now });
    if (statusCode !== null && statusCode >= 200 && statusCode < 300) {
      await this.outbox.markDelivered(webhook.id, now);
      console.log(`webhook ${webhook.id} delivered to merchant ${webhook.merchantId}`);
      return true;
    }
    await this.outbox.markFailed(webhook, now);
    console.error(
      `webhook ${webhook.id} for ${webhook.merchantId} failed: ${failure ?? String(statusCode)}`,
    );
    return false;
  }
}
