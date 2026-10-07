export interface OutgoingMail {
  readonly to: string;
  readonly subject: string;
  readonly text: string;
}

export type DeliveryOutcome = 'delivered' | 'bounced';

export interface MailTransport {
  send(mail: OutgoingMail): Promise<DeliveryOutcome>;
}

export class MailRelayError extends Error {
  constructor(status: number) {
    super(`The mail relay answered ${status}.`);
    this.name = 'MailRelayError';
  }
}

/** Sends mail through the organisation's HTTP mail relay; the body never travels in the URL. */
export class HttpMailRelay implements MailTransport {
  constructor(
    private readonly relayUrl: string,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {}

  async send(mail: OutgoingMail): Promise<DeliveryOutcome> {
    const response = await this.fetchImpl(new URL('/v1/messages', this.relayUrl), {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify(mail),
      signal: AbortSignal.timeout(10_000),
    });
    if (response.status === 422) {
      return 'bounced';
    }
    if (!response.ok) {
      throw new MailRelayError(response.status);
    }
    return 'delivered';
  }
}
