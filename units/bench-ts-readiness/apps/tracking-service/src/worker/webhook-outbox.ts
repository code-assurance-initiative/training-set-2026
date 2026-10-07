import type { Knex } from "knex";

export interface PendingWebhook {
  id: number;
  merchantId: string;
  url: string;
  payload: string;
  attempts: number;
}

interface PendingRow {
  id: string | number;
  merchant_id: string;
  url: string;
  payload: unknown;
  attempts: number;
}

/** Retry schedule for undelivered webhooks: 1 min, 5 min, 30 min, 2 h, then every 6 h, for 3 days. */
const retryDelaysMs = [60_000, 300_000, 1_800_000, 7_200_000];
const laterRetryMs = 21_600_000;
export const maxDeliveryAttempts = 16;

export class WebhookOutbox {
  constructor(private readonly db: Knex) {}

  async due(now: Date, limit: number): Promise<PendingWebhook[]> {
    const rows = await this.db("webhook_outbox as o")
      .join("merchant_webhooks as m", "m.merchant_id", "o.merchant_id")
      .whereNull("o.delivered_at")
      .andWhere("o.next_attempt_at", "<=", now)
      .andWhere("o.attempts", "<", maxDeliveryAttempts)
      .andWhere("m.active", true)
      .orderBy("o.id")
      .limit(limit)
      .select<PendingRow[]>("o.id", "o.merchant_id", "m.url", "o.payload", "o.attempts");
    return rows.map((r) => ({
      id: Number(r.id),
      merchantId: r.merchant_id,
      url: r.url,
      payload: typeof r.payload === "string" ? r.payload : JSON.stringify(r.payload),
      attempts: r.attempts,
    }));
  }

  async markDelivered(id: number, now: Date): Promise<void> {
    await this.db("webhook_outbox")
      .where({ id })
      .update({ delivered_at: now, attempts: this.db.raw("attempts + 1") });
  }

  async markFailed(webhook: PendingWebhook, now: Date): Promise<void> {
    const delay = retryDelaysMs[webhook.attempts] ?? laterRetryMs;
    await this.db("webhook_outbox")
      .where({ id: webhook.id })
      .update({ attempts: webhook.attempts + 1, next_attempt_at: new Date(now.getTime() + delay) });
  }
}
