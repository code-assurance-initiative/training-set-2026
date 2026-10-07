import type { FastifyBaseLogger } from "fastify";

import type { MediaRecord } from "../db/mediaRepository.js";
import type { WebhookSigner } from "../webhooks/signer.js";
import type { PushMessage } from "./push.js";
import type { Alert } from "./slack.js";

const OUTBOUND_TIMEOUT_MS = 10_000;

export interface UploadEventSinks {
  readonly push: { send(message: PushMessage): Promise<void> };
  readonly alerts: { send(alert: Alert): Promise<void> };
  readonly signer: WebhookSigner;
  readonly webhookSubscriberUrl: string | undefined;
  readonly fetchImpl?: typeof fetch;
}

/** Fans an accepted upload out to push subscribers and the webhook subscriber; failures page operations. */
export class UploadEvents {
  private readonly fetchImpl: typeof fetch;

  constructor(
    private readonly sinks: UploadEventSinks,
    private readonly log: FastifyBaseLogger,
  ) {
    this.fetchImpl = sinks.fetchImpl ?? fetch;
  }

  async uploaded(record: MediaRecord): Promise<void> {
    const results = await Promise.allSettled([
      this.sinks.push.send({
        topic: `owner-${record.ownerId}`,
        title: "Upload received",
        body: `${record.contentType}, ${record.sizeBytes} bytes`,
      }),
      this.deliverWebhook(record),
    ]);
    const failures = results.filter((result): result is PromiseRejectedResult => result.status === "rejected");
    if (failures.length === 0) {
      return;
    }
    this.log.error({ mediaId: record.id, failures: failures.map((failure) => String(failure.reason)) }, "upload fan-out failed");
    await this.sinks.alerts.send({
      title: "Upload fan-out failed",
      detail: `media ${record.id}: ${failures.length} of ${results.length} deliveries failed`,
      severity: "warning",
    });
  }

  private async deliverWebhook(record: MediaRecord): Promise<void> {
    if (this.sinks.webhookSubscriberUrl === undefined) {
      return;
    }
    const delivery = this.sinks.signer.sign({
      type: "media.uploaded",
      data: { id: record.id, owner: record.ownerId, contentType: record.contentType, sizeBytes: record.sizeBytes },
    });
    const response = await this.fetchImpl(this.sinks.webhookSubscriberUrl, {
      method: "POST",
      signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
      headers: delivery.headers,
      body: delivery.body,
    });
    if (!response.ok) {
      throw new Error(`webhook subscriber answered HTTP ${response.status}`);
    }
  }
}
