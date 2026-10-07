import { describe, expect, it } from 'vitest';
import { ApiError } from '../src/api/catalogueClient';
import { PAGE_SIZE, createDemoClient } from '../src/api/demoClient';

const TODAY = new Date('2026-10-07T09:00:00Z');
const query = { text: '', formats: [], availableOnly: false, page: 1 } as const;

describe('demo catalogue', () => {
  it('pages through every item', async () => {
    const client = createDemoClient(TODAY);
    const first = await client.search(query);
    const second = await client.search({ ...query, page: 2 });

    expect(first.items).toHaveLength(PAGE_SIZE);
    expect(first.total).toBe(7);
    expect(second.items).toHaveLength(2);
  });

  it('matches text against title, author and subject', async () => {
    const client = createDemoClient(TODAY);
    expect((await client.search({ ...query, text: 'tide' })).total).toBe(2);
    expect((await client.search({ ...query, text: 'OKAFOR' })).total).toBe(1);
    expect((await client.search({ ...query, text: 'wetlands' })).total).toBe(1);
  });

  it('filters by format and availability', async () => {
    const client = createDemoClient(TODAY);
    const books = await client.search({ ...query, formats: ['book'] });
    const available = await client.search({ ...query, formats: ['book'], availableOnly: true });

    expect(books.items.every((item) => item.format === 'book')).toBe(true);
    expect(available.total).toBe(books.total - 1);
  });

  it('rejects an unknown item with a 404', async () => {
    await expect(createDemoClient(TODAY).getItem('missing')).rejects.toEqual(
      new ApiError(404, 'No item missing'),
    );
  });

  it('renews a loan until its renewals run out', async () => {
    const client = createDemoClient(TODAY);
    const renewed = await client.renew('l-1');

    expect(renewed.renewals).toBe(1);
    expect(renewed.dueOn).toBe('2026-10-28');
    await expect(client.renew('l-2')).rejects.toBeInstanceOf(ApiError);
  });

  it('reserves an item for pickup two days out', async () => {
    const reservation = await createDemoClient(TODAY).borrow({
      itemId: 'b-1001',
      cardNumber: '12345678901234',
      pickupBranch: 'harbour',
      notifyByEmail: false,
    });

    expect(reservation).toEqual({
      reservationId: 'r-1',
      itemId: 'b-1001',
      pickupBranch: 'harbour',
      readyBy: '2026-10-09',
    });
  });
});
