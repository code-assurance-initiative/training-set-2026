import type { FastifyBaseLogger } from "fastify";

const OPS_ALERTS_WEBHOOK =
  "https://hooks.slack.com/services/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";

export interface Alert {
  readonly title: string;
  readonly detail: string;
  readonly severity: "info" | "warning" | "critical";
}

const severityEmoji: Record<Alert["severity"], string> = {
  info: ":information_source:",
  warning: ":warning:",
  critical: ":rotating_light:",
};

const OUTBOUND_TIMEOUT_MS = 10_000;

export class SlackAlerts {
  constructor(
    private readonly log: FastifyBaseLogger,
    private readonly webhookUrl: string = OPS_ALERTS_WEBHOOK,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {}

  async send(alert: Alert): Promise<void> {
    const response = await this.fetchImpl(this.webhookUrl, {
      method: "POST",
      signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        text: `${severityEmoji[alert.severity]} *${alert.title}*\n${alert.detail}`,
      }),
    });
    if (!response.ok) {
      this.log.warn({ status: response.status, title: alert.title }, "slack alert was not accepted");
    }
  }
}
