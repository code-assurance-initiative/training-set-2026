/** A fact one context publishes for others (ADR 0005). Payloads carry plain values only. */
export interface IntegrationMessage {
  readonly id: string;
  readonly type: string;
  readonly occurredAt: string;
  readonly payload: Readonly<Record<string, unknown>>;
}

export type IntegrationHandler = (message: IntegrationMessage) => Promise<void>;

/** In-process delivery of outbox messages to the subscribing contexts. */
export class IntegrationBus {
  readonly #handlers = new Map<string, IntegrationHandler[]>();

  subscribe(type: string, handler: IntegrationHandler): void {
    this.#handlers.set(type, [...(this.#handlers.get(type) ?? []), handler]);
  }

  async publish(message: IntegrationMessage): Promise<void> {
    for (const handler of this.#handlers.get(message.type) ?? []) {
      await handler(message);
    }
  }
}
