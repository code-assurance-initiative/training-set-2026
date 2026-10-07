/** A problem-details answer from the gateway (RFC 9457). */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly title: string,
    readonly detail: string,
  ) {
    super(detail || title);
    this.name = 'ApiError';
  }
}

/**
 * Calls the gateway under /api on the console's own origin. The session is the authentication proxy's cookie, sent by
 * the browser; the console never sees or stores a token.
 */
export async function request<T>(
  path: string,
  init: {
    method?: 'GET' | 'POST';
    body?: unknown;
    signal?: AbortSignal;
    headers?: Record<string, string>;
  } = {},
): Promise<T | undefined> {
  const response = await fetch(`/api${path}`, {
    method: init.method ?? 'GET',
    credentials: 'same-origin',
    headers: {
      accept: 'application/json',
      ...(init.body === undefined ? {} : { 'content-type': 'application/json' }),
      ...init.headers,
    },
    body: init.body === undefined ? null : JSON.stringify(init.body),
    signal: init.signal ?? null,
  });
  if (!response.ok) {
    const problem = (await response.json().catch(() => ({}))) as {
      title?: string;
      detail?: string;
    };
    throw new ApiError(response.status, problem.title ?? response.statusText, problem.detail ?? '');
  }
  return response.status === 204 ? undefined : ((await response.json()) as T);
}
