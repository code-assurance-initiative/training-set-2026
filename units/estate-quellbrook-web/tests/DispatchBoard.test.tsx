import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { App } from '../src/App';
import { loadBoardDensity, saveBoardDensity } from '../src/preferences/boardDensity';
import { fakeApi, json } from './support/fakeApi';

const route = (status: string) => ({
  routeId: '0198f1a2-0000-7000-8000-0000000000a1',
  depot: 'AAR',
  zone: 'DK-AAR',
  express: true,
  status,
  driverName: 'Anna K.',
  vehicleRegistration: 'QB 12 345',
  vehicleKind: 'Van',
  capacityGrams: 800000,
  loadGrams: 3500,
  stops: [
    {
      sequence: 1,
      consignmentId: 'c1',
      orderId: 'o1',
      postalCode: '8000',
      weightGrams: 3500,
      status: 'Assigned',
    },
  ],
});

describe('dispatch board', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows the routes of the chosen day and starts one', async () => {
    let started = false;
    const calls = fakeApi({
      '/api/dispatch/board': () => json(200, [route(started ? 'Started' : 'Planned')]),
      '/api/dispatch/routes/': () => {
        started = true;
        return new Response(null, { status: 204 });
      },
    });
    window.history.replaceState(null, '', '/dispatch');
    render(<App />);

    expect(await screen.findByRole('heading', { name: 'DK-AAR · express' })).toBeInTheDocument();
    expect(screen.getByText('Driver: Anna K.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: 'Start route DK-AAR' }));

    await vi.waitFor(() => {
      expect(screen.queryByRole('button', { name: 'Start route DK-AAR' })).not.toBeInTheDocument();
    });
    expect(calls.some((call) => call.method === 'POST' && call.url.endsWith('/start'))).toBe(true);
  });

  it('loads another day when the date changes', async () => {
    const calls = fakeApi({ '/api/dispatch/board': () => json(200, []) });
    window.history.replaceState(null, '', '/dispatch');
    render(<App />);

    expect(await screen.findByText('No routes planned for this day.')).toBeInTheDocument();
    const day = screen.getByLabelText('Day');
    await userEvent.clear(day);
    await userEvent.type(day, '2026-08-14');

    await vi.waitFor(() => {
      expect(calls.at(-1)?.url).toBe('/api/dispatch/board?date=2026-08-14');
    });
  });

  it('shows why a route could not start', async () => {
    fakeApi({
      '/api/dispatch/board': () => json(200, [route('Planned')]),
      '/api/dispatch/routes/': () =>
        json(409, { title: 'Conflict', detail: 'Route has no stops.' }),
    });
    window.history.replaceState(null, '', '/dispatch');
    render(<App />);

    await userEvent.click(await screen.findByRole('button', { name: 'Start route DK-AAR' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Route has no stops.');
  });
});

describe('board preferences', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('remembers the card density in this browser and filters stops by postal code', async () => {
    fakeApi({ '/api/dispatch/board': () => json(200, [route('Started')]) });
    window.history.replaceState(null, '', '/dispatch');
    render(<App />);

    await userEvent.click(await screen.findByRole('radio', { name: 'Compact' }));
    expect(window.localStorage.getItem('quellbrook.board-density')).toBe('compact');
    await userEvent.type(
      screen.getByRole('searchbox', { name: 'Show only stops with this postal code' }),
      '9000',
    );

    expect(screen.queryByText(/^8000,/)).not.toBeInTheDocument();
  });

  it('starts comfortable when nothing is remembered and when storage fails', () => {
    expect(loadBoardDensity()).toBe('comfortable');
    const failing = vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('denied');
    });
    expect(loadBoardDensity()).toBe('comfortable');
    failing.mockRestore();
    const failingSave = vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('quota');
    });
    expect(() => {
      saveBoardDensity('compact');
    }).not.toThrow();
    failingSave.mockRestore();
  });
});
