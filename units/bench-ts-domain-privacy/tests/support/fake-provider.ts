export interface SentRequest {
  readonly url: string;
  readonly body: Record<string, unknown>;
  readonly authorization: string | undefined;
}

/** Stands in for the e-mail and SMS providers' HTTP APIs; records what was sent. */
export class FakeProvider {
  readonly sent: SentRequest[] = [];
  status = 202;

  readonly fetch: typeof fetch = (input, init) => {
    const headers = new Headers(init?.headers);
    if (!(input instanceof URL) || typeof init?.body !== 'string') {
      throw new TypeError('The gateways post a JSON string to a URL.');
    }
    this.sent.push({
      url: input.href,
      body: JSON.parse(init.body) as Record<string, unknown>,
      authorization: headers.get('authorization') ?? undefined,
    });
    return Promise.resolve(new Response(null, { status: this.status }));
  };

  to(host: string): SentRequest[] {
    return this.sent.filter((request) => new URL(request.url).host === host);
  }
}
