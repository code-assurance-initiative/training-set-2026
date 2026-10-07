export interface CrmContact {
  readonly displayName: string;
}

export class CrmError extends Error {
  constructor(status: number) {
    super(`The CRM answered ${status}.`);
    this.name = 'CrmError';
  }
}

/** The CRM keeps the display names and channel preferences of report recipients. */
export class CrmClient {
  constructor(
    private readonly baseUrl: string,
    private readonly fetchImpl: typeof fetch = fetch,
  ) {}

  async findContact(email: string): Promise<CrmContact | undefined> {
    const response = await this.fetchImpl(
      `${this.baseUrl}/contacts?email=${encodeURIComponent(email)}`,
      {
        signal: AbortSignal.timeout(5_000),
      },
    );
    if (response.status === 404) {
      return undefined;
    }
    if (!response.ok) {
      throw new CrmError(response.status);
    }
    return (await response.json()) as CrmContact;
  }

  async setEmailChannel(subscriptionId: string, enabled: boolean): Promise<void> {
    const response = await this.fetchImpl(
      `${this.baseUrl}/subscriptions/${encodeURIComponent(subscriptionId)}/channels?email=${enabled}`,
      { method: 'PUT', signal: AbortSignal.timeout(5_000) },
    );
    if (!response.ok) {
      throw new CrmError(response.status);
    }
  }
}
