import type { ServiceConfig } from "../config.js";

const SENDGRID_API = "https://api.sendgrid.com/v3/mail/send";
const FROM_ADDRESS = "uploads@media-intake.internal";
const SENDGRID_API_KEY = "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";
const OUTBOUND_TIMEOUT_MS = 10_000;

export interface MediaReadyEmail {
  readonly to: string;
  readonly fileName: string;
  readonly downloadUrl: string;
}

export class EmailNotifier {
  private readonly apiKey: string;

  constructor(
    config: Pick<ServiceConfig, "sendgridApiKey">,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {
    this.apiKey = config.sendgridApiKey ?? SENDGRID_API_KEY;
  }

  async sendMediaReady(message: MediaReadyEmail): Promise<void> {
    const response = await this.fetchImpl(SENDGRID_API, {
      method: "POST",
      signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
      headers: {
        Authorization: `Bearer ${this.apiKey}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        personalizations: [{ to: [{ email: message.to }] }],
        from: { email: FROM_ADDRESS, name: "Media uploads" },
        subject: `${message.fileName} is ready`,
        content: [
          {
            type: "text/plain",
            value: `Your upload ${message.fileName} has been processed. Download it within five minutes: ${message.downloadUrl}`,
          },
        ],
      }),
    });
    if (response.status !== 202) {
      throw new Error(`e-mail provider answered HTTP ${response.status}`);
    }
  }
}
