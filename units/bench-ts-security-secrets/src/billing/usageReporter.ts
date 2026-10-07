import type { ServiceConfig } from "../config.js";

const STRIPE_API = "https://api.stripe.com/v1";
const STORAGE_METER_EVENT = "storage_gb_hours";
const STRIPE_RESTRICTED_KEY = "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";
const OUTBOUND_TIMEOUT_MS = 10_000;

export interface UsageReport {
  readonly stripeCustomerId: string;
  readonly gigabyteHours: number;
  readonly at: Date;
}

export class UsageReporter {
  private readonly apiKey: string;

  constructor(
    config: Pick<ServiceConfig, "stripeRestrictedKey">,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {
    this.apiKey = config.stripeRestrictedKey ?? STRIPE_RESTRICTED_KEY;
  }

  async report(usage: UsageReport): Promise<void> {
    const body = new URLSearchParams({
      event_name: STORAGE_METER_EVENT,
      "payload[stripe_customer_id]": usage.stripeCustomerId,
      "payload[value]": String(Math.ceil(usage.gigabyteHours)),
      timestamp: String(Math.floor(usage.at.getTime() / 1000)),
    });
    const response = await this.fetchImpl(`${STRIPE_API}/billing/meter_events`, {
      method: "POST",
      signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
      headers: {
        Authorization: `Bearer ${this.apiKey}`,
        "Content-Type": "application/x-www-form-urlencoded",
      },
      body,
    });
    if (!response.ok) {
      throw new Error(`usage report rejected with HTTP ${response.status}`);
    }
  }
}
