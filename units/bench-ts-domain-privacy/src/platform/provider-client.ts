/** A REST provider the service sends messages through (e-mail, SMS). */
export interface ProviderEndpoint {
  readonly endpoint: URL;
  readonly apiToken: string;
  readonly sender: string;
}

const timeoutMs = 10_000;

/** Posts `body` as JSON with the provider's bearer token; resolves to whether it was accepted. */
export async function postToProvider(
  provider: ProviderEndpoint,
  body: Readonly<Record<string, string>>,
  fetchFn: typeof fetch,
): Promise<{ accepted: boolean; status: number }> {
  const response = await fetchFn(provider.endpoint, {
    method: 'POST',
    headers: {
      authorization: `Bearer ${provider.apiToken}`,
      'content-type': 'application/json',
    },
    body: JSON.stringify({ from: provider.sender, ...body }),
    signal: AbortSignal.timeout(timeoutMs),
  });
  return { accepted: response.ok, status: response.status };
}
