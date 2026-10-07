import { createPrivateKey, createPublicKey, sign, verify, type KeyObject } from "node:crypto";
import { readFile } from "node:fs/promises";

export interface SignedDelivery {
  readonly body: string;
  readonly headers: Record<string, string>;
}

/** Signs outbound webhook deliveries with Ed25519 over `<timestamp>.<body>`. */
export class WebhookSigner {
  private constructor(private readonly privateKey: KeyObject) {}

  static async fromPemFile(path: string): Promise<WebhookSigner> {
    return new WebhookSigner(createPrivateKey(await readFile(path, "utf8")));
  }

  publicKeyPem(): string {
    return createPublicKey(this.privateKey).export({ type: "spki", format: "pem" }).toString();
  }

  sign(event: unknown, now: Date = new Date()): SignedDelivery {
    const body = JSON.stringify(event);
    const timestamp = String(Math.floor(now.getTime() / 1000));
    const signature = sign(null, Buffer.from(`${timestamp}.${body}`), this.privateKey).toString("base64");
    return {
      body,
      headers: {
        "Content-Type": "application/json",
        "Webhook-Timestamp": timestamp,
        "Webhook-Signature": `ed25519=${signature}`,
      },
    };
  }

  static verify(publicKeyPem: string, delivery: SignedDelivery): boolean {
    const timestamp = delivery.headers["Webhook-Timestamp"];
    const header = delivery.headers["Webhook-Signature"];
    if (timestamp === undefined || header === undefined || !header.startsWith("ed25519=")) {
      return false;
    }
    const signature = Buffer.from(header.slice("ed25519=".length), "base64");
    return verify(null, Buffer.from(`${timestamp}.${delivery.body}`), createPublicKey(publicKeyPem), signature);
  }
}
