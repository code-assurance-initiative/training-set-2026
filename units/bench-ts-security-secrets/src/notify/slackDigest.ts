import type { ServiceConfig } from "../config.js";

const POST_MESSAGE_URL = "https://slack.com/api/chat.postMessage";
const OUTBOUND_TIMEOUT_MS = 10_000;

export interface UploadDigest {
  readonly day: string;
  readonly uploads: number;
  readonly bytes: number;
}

export class SlackDigest {
  constructor(
    private readonly config: Pick<ServiceConfig, "slackBotToken" | "slackDigestChannel">,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {}

  async post(digest: UploadDigest): Promise<void> {
    if (this.config.slackBotToken === undefined) {
      throw new Error("SLACK_BOT_TOKEN is not set");
    }
    const gigabytes = (digest.bytes / 1024 ** 3).toFixed(2);
    const response = await this.fetchImpl(POST_MESSAGE_URL, {
      method: "POST",
      signal: AbortSignal.timeout(OUTBOUND_TIMEOUT_MS),
      headers: {
        Authorization: `Bearer ${this.config.slackBotToken}`,
        "Content-Type": "application/json; charset=utf-8",
      },
      body: JSON.stringify({
        channel: this.config.slackDigestChannel,
        text: `Uploads on ${digest.day}: ${digest.uploads} files, ${gigabytes} GB stored`,
      }),
    });
    const result = (await response.json()) as { ok: boolean; error?: string };
    if (!result.ok) {
      throw new Error(`slack rejected the digest: ${result.error ?? "unknown error"}`);
    }
  }
}
