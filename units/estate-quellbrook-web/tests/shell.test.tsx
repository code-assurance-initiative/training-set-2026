import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { App } from '../src/App';
import { ErrorMessage } from '../src/components/ErrorMessage';
import { parseRoute } from '../src/routing';
import { fakeApi, json } from './support/fakeApi';

describe('console shell', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it.each([
    ['/', { name: 'orders' }],
    ['/orders', { name: 'orders' }],
    [
      '/orders/0198f1a2-0000-7000-8000-000000000001',
      { name: 'order', orderId: '0198f1a2-0000-7000-8000-000000000001' },
    ],
    ['/dispatch', { name: 'dispatch' }],
    ['/orders/123', { name: 'not-found' }],
  ])('routes %s', (path, route) => {
    expect(parseRoute(path)).toEqual(route);
  });

  it('has a skip link, a labelled navigation and an unknown-page message', () => {
    fakeApi({});
    window.history.replaceState(null, '', '/nowhere');
    render(<App />);

    expect(screen.getByRole('link', { name: 'Skip to content' })).toHaveAttribute('href', '#main');
    expect(screen.getByRole('navigation', { name: 'Main' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'Quellbrook Freight' })).toBeInTheDocument();
  });

  it('navigates without a page load from the main navigation', async () => {
    fakeApi({
      '/api/orders': () => json(200, { items: [], page: 1, pageSize: 25, totalCount: 0 }),
    });
    window.history.replaceState(null, '', '/nowhere');
    render(<App />);

    await userEvent.click(screen.getByRole('link', { name: 'Orders' }));

    expect(await screen.findByRole('heading', { name: 'Orders' })).toBeInTheDocument();
  });

  it('explains an unexpected error in plain words', () => {
    render(<ErrorMessage error={new TypeError('fetch failed')} />);

    expect(screen.getByRole('alert')).toHaveTextContent('Something went wrong');
  });
});
