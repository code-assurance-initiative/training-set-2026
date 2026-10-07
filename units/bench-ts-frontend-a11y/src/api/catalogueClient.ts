import type {
  BorrowRequest,
  CatalogueItem,
  LibraryNotice,
  Loan,
  Reservation,
  SearchPage,
  SearchQuery,
} from './types';

export interface CatalogueClient {
  search(query: SearchQuery, signal?: AbortSignal): Promise<SearchPage>;
  getItem(id: string, signal?: AbortSignal): Promise<CatalogueItem>;
  borrow(request: BorrowRequest): Promise<Reservation>;
  listLoans(signal?: AbortSignal): Promise<readonly Loan[]>;
  renew(loanId: string): Promise<Loan>;
  currentNotice(signal?: AbortSignal): Promise<LibraryNotice | null>;
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

type Fetch = (input: string, init?: RequestInit) => Promise<Response>;

/** Talks to the catalogue API behind the same origin; the session cookie authenticates loan calls. */
export function createHttpClient(baseUrl: string, fetchImpl: Fetch = fetch): CatalogueClient {
  async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
    const headers = new Headers(init.headers);
    headers.set('Accept', 'application/json');
    const response = await fetchImpl(`${baseUrl}${path}`, {
      ...init,
      credentials: 'same-origin',
      headers,
    });
    if (!response.ok) {
      throw new ApiError(response.status, `Request to ${path} failed with ${response.status}`);
    }
    return (await response.json()) as T;
  }

  function withSignal(signal: AbortSignal | undefined): RequestInit {
    return signal ? { signal } : {};
  }

  return {
    search(query, signal) {
      const params = new URLSearchParams({ q: query.text, page: String(query.page) });
      query.formats.forEach((format) => {
        params.append('format', format);
      });
      if (query.availableOnly) {
        params.set('available', 'true');
      }
      return request<SearchPage>(`/items?${params.toString()}`, withSignal(signal));
    },
    getItem(id, signal) {
      return request<CatalogueItem>(`/items/${encodeURIComponent(id)}`, withSignal(signal));
    },
    borrow(borrowRequest) {
      return request<Reservation>('/reservations', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(borrowRequest),
      });
    },
    listLoans(signal) {
      return request<readonly Loan[]>('/loans', withSignal(signal));
    },
    renew(loanId) {
      return request<Loan>(`/loans/${encodeURIComponent(loanId)}/renewal`, { method: 'POST' });
    },
    currentNotice(signal) {
      return request<LibraryNotice | null>('/notices/current', withSignal(signal));
    },
  };
}
