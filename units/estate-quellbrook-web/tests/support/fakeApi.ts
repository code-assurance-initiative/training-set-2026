import { vi } from 'vitest';

export interface FakeCall {
  readonly url: string;
  readonly method: string;
  readonly body: unknown;
  readonly headers: Record<string, string>;
}

/** Replaces fetch with answers by path; records every call. */
export function fakeApi(routes: Record<string, () => Response>) {
  const calls: FakeCall[] = [];
  const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
    const url = input instanceof Request ? input.url : input.toString();
    calls.push({
      url,
      method: init?.method ?? 'GET',
      body: typeof init?.body === 'string' ? (JSON.parse(init.body) as unknown) : undefined,
      headers: Object.fromEntries(new Headers(init?.headers).entries()),
    });
    const key = Object.keys(routes).find((route) => url.startsWith(route));
    const answer = key ? routes[key] : undefined;
    return Promise.resolve(answer ? answer() : json(404, { title: 'Not Found', status: 404 }));
  });
  vi.stubGlobal('fetch', fetchMock);
  return calls;
}

export function json(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'content-type': 'application/json' },
  });
}

export const orderSummary = (id: string, name: string) => ({
  orderId: id,
  customerAccountId: 'QB-104233',
  serviceLevel: 'standard',
  status: 'Placed',
  consigneeName: name,
  destinationCity: 'Aarhus C',
  parcelCount: 2,
  placedAt: '2026-08-03T07:30:00Z',
});

export const order = (id: string) => ({
  id,
  customerAccountId: 'QB-104233',
  serviceLevel: 'express',
  status: 'placed',
  consignee: {
    name: 'Halden Bikes ApS',
    line1: 'Søndergade 12',
    line2: null,
    postalCode: '8000',
    city: 'Aarhus C',
    countryCode: 'DK',
  },
  parcels: [
    { number: 1, weightGrams: 2400 },
    { number: 2, weightGrams: 1100 },
  ],
  totalWeightGrams: 3500,
  placedBy: 'operator-17',
  placedAt: '2026-08-03T07:30:00Z',
});
