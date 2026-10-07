import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { App } from '../src/App';
import { fakeApi, json, order } from './support/fakeApi';

const id = '0198f1a2-0000-7000-8000-000000000001';

describe('order page', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows the order and its delivery', async () => {
    fakeApi({
      [`/api/shipments/${id}`]: () =>
        json(200, {
          order: order(id),
          delivery: {
            consignmentId: 'c1',
            status: 'OutForDelivery',
            outForDeliveryAt: '2026-08-12T06:30:00Z',
          },
        }),
    });
    window.history.replaceState(null, '', `/orders/${id}`);
    render(<App />);

    expect(
      await screen.findByRole('heading', { name: 'Order for Halden Bikes ApS' }),
    ).toBeInTheDocument();
    expect(screen.getByText('2 parcels, 3.5 kg')).toBeInTheDocument();
    expect(screen.getByText('Out for delivery')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Cancel order…' })).not.toBeInTheDocument();
  });

  it('says when dispatch does not have the order yet', async () => {
    fakeApi({ [`/api/shipments/${id}`]: () => json(200, { order: order(id), delivery: null }) });
    window.history.replaceState(null, '', `/orders/${id}`);
    render(<App />);

    expect(await screen.findByText('Not with dispatch yet.')).toBeInTheDocument();
  });

  it('cancels an order with a reason from the dialog', async () => {
    let cancelled = false;
    const calls = fakeApi({
      [`/api/shipments/${id}`]: () =>
        json(200, {
          order: { ...order(id), status: cancelled ? 'cancelled' : 'placed' },
          delivery: null,
        }),
      [`/api/orders/${id}/cancellation`]: () => {
        cancelled = true;
        return new Response(null, { status: 204 });
      },
    });
    window.history.replaceState(null, '', `/orders/${id}`);
    render(<App />);

    await userEvent.click(await screen.findByRole('button', { name: 'Cancel order…' }));
    const dialog = screen.getByRole('dialog', { name: 'Cancel this order' });
    await userEvent.type(within(dialog).getByLabelText('Reason'), 'Shipper withdrew the order');
    await userEvent.click(within(dialog).getByRole('button', { name: 'Cancel order' }));

    expect(await screen.findByText('Cancelled')).toBeInTheDocument();
    expect(calls.find((call) => call.method === 'POST')?.body).toEqual({
      reason: 'Shipper withdrew the order',
    });
  });

  it('closes the dialog without cancelling', async () => {
    fakeApi({ [`/api/shipments/${id}`]: () => json(200, { order: order(id), delivery: null }) });
    window.history.replaceState(null, '', `/orders/${id}`);
    render(<App />);

    await userEvent.click(await screen.findByRole('button', { name: 'Cancel order…' }));
    await userEvent.click(screen.getByRole('button', { name: 'Close' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('shows why a cancellation failed', async () => {
    fakeApi({
      [`/api/shipments/${id}`]: () => json(200, { order: order(id), delivery: null }),
      [`/api/orders/${id}/cancellation`]: () =>
        json(409, { title: 'Conflict', detail: 'The order is already cancelled.' }),
    });
    window.history.replaceState(null, '', `/orders/${id}`);
    render(<App />);

    await userEvent.click(await screen.findByRole('button', { name: 'Cancel order…' }));
    await userEvent.type(screen.getByLabelText('Reason'), 'again');
    await userEvent.click(screen.getByRole('button', { name: 'Cancel order' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('already cancelled');
  });

  it('says so when the order does not exist', async () => {
    fakeApi({});
    window.history.replaceState(null, '', `/orders/${id}`);
    render(<App />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Not Found');
  });
});
