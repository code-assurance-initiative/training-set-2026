import { describe, expect, it, vi } from 'vitest';
import { ApiError, createHttpClient } from '../src/api/catalogueClient';

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  });
}

describe('HTTP catalogue client', () => {
  it('sends the search query as URL parameters', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse({ items: [], total: 0 })));
    const client = createHttpClient('/api', fetchImpl);

    await client.search({
      text: 'sea birds',
      formats: ['book', 'dvd'],
      availableOnly: true,
      page: 2,
    });

    expect(fetchImpl).toHaveBeenCalledWith(
      '/api/items?q=sea+birds&page=2&format=book&format=dvd&available=true',
      expect.objectContaining({ credentials: 'same-origin' }),
    );
  });

  it('posts a reservation as JSON', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse({ reservationId: 'r-9' })));
    const client = createHttpClient('/api', fetchImpl);
    const request = {
      itemId: 'b-1',
      cardNumber: '12345678901234',
      pickupBranch: 'central',
      notifyByEmail: true,
    } as const;

    await expect(client.borrow(request)).resolves.toEqual({ reservationId: 'r-9' });
    expect(fetchImpl).toHaveBeenCalledWith(
      '/api/reservations',
      expect.objectContaining({ method: 'POST', body: JSON.stringify(request) }),
    );
  });

  it('escapes ids in paths', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse({})));
    const client = createHttpClient('/api', fetchImpl);

    await client.getItem('a/b');
    await client.renew('l 1');

    expect(fetchImpl).toHaveBeenNthCalledWith(1, '/api/items/a%2Fb', expect.anything());
    expect(fetchImpl).toHaveBeenNthCalledWith(2, '/api/loans/l%201/renewal', expect.anything());
  });

  it('turns an error status into an ApiError', async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(jsonResponse({}, 503)));
    const client = createHttpClient('/api', fetchImpl);

    await expect(client.listLoans()).rejects.toEqual(
      new ApiError(503, 'Request to /loans failed with 503'),
    );
    await expect(client.currentNotice()).rejects.toBeInstanceOf(ApiError);
  });
});
