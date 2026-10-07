import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { App } from '../src/App';
import { fakeApi, json, orderSummary } from './support/fakeApi';

const first = '0198f1a2-0000-7000-8000-000000000001';

describe('orders page', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('lists the orders newest first and pages through them', async () => {
    const calls = fakeApi({
      '/api/orders?page=1': () =>
        json(200, {
          items: [orderSummary(first, 'Halden Bikes ApS')],
          page: 1,
          pageSize: 25,
          totalCount: 30,
        }),
      '/api/orders?page=2': () =>
        json(200, {
          items: [orderSummary(first, 'Fjord Kaffe')],
          page: 2,
          pageSize: 25,
          totalCount: 30,
        }),
    });
    render(<App />);

    expect(await screen.findByRole('cell', { name: 'Halden Bikes ApS' })).toBeInTheDocument();
    expect(screen.getByText('Page 1 of 2')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Older' }));

    expect(await screen.findByRole('cell', { name: 'Fjord Kaffe' })).toBeInTheDocument();
    expect(calls.map((call) => call.url)).toEqual([
      '/api/orders?page=1&pageSize=25',
      '/api/orders?page=2&pageSize=25',
    ]);
  });

  it('narrows the page by consignee, city and status', async () => {
    fakeApi({
      '/api/orders?page=1': () =>
        json(200, {
          items: [
            orderSummary(first, 'Halden Bikes ApS'),
            {
              ...orderSummary('0198f1a2-0000-7000-8000-000000000002', 'Fjord Kaffe'),
              destinationCity: 'Odense',
              status: 'Cancelled',
            },
          ],
          page: 1,
          pageSize: 25,
          totalCount: 2,
        }),
    });
    render(<App />);

    await userEvent.type(await screen.findByPlaceholderText('Search consignee or city'), 'odense');
    expect(screen.queryByRole('cell', { name: 'Halden Bikes ApS' })).not.toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Fjord Kaffe' })).toBeInTheDocument();
    await userEvent.clear(screen.getByPlaceholderText('Search consignee or city'));
    await userEvent.selectOptions(screen.getByLabelText('Status'), 'placed');

    expect(screen.getByRole('cell', { name: 'Halden Bikes ApS' })).toBeInTheDocument();
    expect(screen.queryByRole('cell', { name: 'Fjord Kaffe' })).not.toBeInTheDocument();
  });

  it('opens an order from its row', async () => {
    fakeApi({
      '/api/orders?page=1': () =>
        json(200, {
          items: [orderSummary(first, 'Halden Bikes ApS')],
          page: 1,
          pageSize: 25,
          totalCount: 1,
        }),
    });
    render(<App />);

    await userEvent.click(await screen.findByRole('cell', { name: 'Halden Bikes ApS' }));

    expect(window.location.pathname).toBe(`/orders/${first}`);
  });

  it('tells the operator when the session has ended', async () => {
    fakeApi({ '/api/orders': () => json(401, { title: 'Unauthorized', status: 401 }) });
    render(<App />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Your session has ended');
  });

  it('shows the gateway problem otherwise', async () => {
    fakeApi({
      '/api/orders': () =>
        json(502, { title: 'Bad Gateway', detail: 'The orders service did not answer.' }),
    });
    render(<App />);

    expect(await screen.findByRole('alert')).toHaveTextContent(
      'The orders service did not answer.',
    );
  });
});
