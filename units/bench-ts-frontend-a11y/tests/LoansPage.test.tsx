import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { afterEach, describe, expect, it, vi } from 'vitest';
import type { CatalogueClient } from '../src/api/catalogueClient';
import { createDemoClient } from '../src/api/demoClient';
import { LoansPage } from '../src/features/loans/LoansPage';
import { TODAY, renderWithProviders } from './support/renderWithProviders';

function rowTitles(): string[] {
  const table = screen.getByRole('table', { name: 'Items you have on loan' });
  return within(table)
    .getAllByRole('rowheader')
    .map((cell) => cell.textContent);
}

afterEach(() => {
  vi.restoreAllMocks();
});

describe('LoansPage', () => {
  it('lists loans soonest-due first with their due status', async () => {
    renderWithProviders(<LoansPage today={TODAY} />);

    expect(await screen.findByRole('table')).toBeInTheDocument();
    expect(rowTitles()).toEqual(['Growing Food in Small Spaces', 'Knots and Splices']);
    expect(screen.getByText('Overdue by 5 days')).toBeInTheDocument();
    expect(screen.getByText('Due tomorrow')).toBeInTheDocument();
    expect(screen.getByText(/Next due:/)).toHaveTextContent('Knots and Splices, due tomorrow');
  });

  it('sorts by a column and reverses on a second click', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoansPage today={TODAY} />);
    const titleHeader = await screen.findByRole('columnheader', { name: /Title/ });

    await user.click(within(titleHeader).getByRole('button'));
    expect(titleHeader).toHaveAttribute('aria-sort', 'ascending');
    expect(rowTitles()).toEqual(['Growing Food in Small Spaces', 'Knots and Splices']);

    await user.click(within(titleHeader).getByRole('button'));
    expect(titleHeader).toHaveAttribute('aria-sort', 'descending');
    expect(rowTitles()).toEqual(['Knots and Splices', 'Growing Food in Small Spaces']);
  });

  it('filters by title or author', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoansPage today={TODAY} />);

    await user.type(await screen.findByPlaceholderText('Filter by title or author'), 'reyes');
    expect(rowTitles()).toEqual(['Knots and Splices']);

    await user.type(screen.getByPlaceholderText('Filter by title or author'), 'zzz');
    expect(screen.getByText('No loans match “reyeszzz”.')).toBeInTheDocument();
  });

  it('renews a single loan and reports the new due date', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoansPage today={TODAY} />);

    await user.click(await screen.findByRole('button', { name: /^Renew\s*Knots and Splices$/ }));

    expect(await screen.findByText('1 of 1 loan renewed.')).toBeInTheDocument();
    expect(screen.getAllByText(/Now due 28 Oct 2026/)).not.toHaveLength(0);
    expect(screen.getByRole('cell', { name: '1 of 2' })).toBeInTheDocument();
  });

  it('renews every eligible loan at once', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoansPage today={TODAY} />);

    await user.click(await screen.findByRole('button', { name: 'Renew all eligible loans (1)' }));

    expect(await screen.findByText('1 of 1 loan renewed.')).toBeInTheDocument();
  });

  it('explains a refused renewal', async () => {
    const user = userEvent.setup();
    const demo = createDemoClient(TODAY);
    const client: CatalogueClient = { ...demo, renew: () => Promise.reject(new Error('down')) };
    renderWithProviders(<LoansPage today={TODAY} />, { client });

    await user.click(await screen.findByRole('button', { name: /^Renew\s*Knots and Splices$/ }));

    expect(await screen.findByText('0 of 1 loan renewed.')).toBeInTheDocument();
    expect(screen.getAllByText(/The library system did not answer/)).not.toHaveLength(0);
  });

  it('shows fines with their total', async () => {
    renderWithProviders(<LoansPage today={TODAY} />);

    const fines = await screen.findByRole('region', { name: 'Fines' });
    expect(within(fines).getAllByText('£2.50')).toHaveLength(2);
  });

  it('exports the visible loans as CSV', async () => {
    const user = userEvent.setup();
    const createObjectURL = vi.fn<(blob: Blob) => string>(() => 'blob:loans');
    URL.createObjectURL = createObjectURL;
    URL.revokeObjectURL = vi.fn();
    const click = vi
      .spyOn(HTMLAnchorElement.prototype, 'click')
      .mockImplementation(() => undefined);
    renderWithProviders(<LoansPage today={TODAY} />);

    await user.click(await screen.findByRole('button', { name: 'Export loans as CSV' }));

    expect(click).toHaveBeenCalledOnce();
    const blob = createObjectURL.mock.calls[0]?.[0];
    const csv = await blob?.text();
    expect(csv?.split('\r\n')).toEqual([
      'Title,Author,Borrowed,Due,Renewals,Fine',
      'Growing Food in Small Spaces,Idris Okafor,2026-08-28,2026-10-02,2/2,2.50',
      'Knots and Splices,Tomas Reyes,2026-09-17,2026-10-08,0/2,0.00',
    ]);
  });

  it('says when nothing is on loan', async () => {
    const client: CatalogueClient = {
      ...createDemoClient(TODAY),
      listLoans: () => Promise.resolve([]),
    };
    renderWithProviders(<LoansPage today={TODAY} />, { client });

    expect(await screen.findByText(/You have nothing on loan/)).toBeInTheDocument();
    expect(screen.getByText('You have no fines.')).toBeInTheDocument();
  });

  it('reports loans that could not be loaded', async () => {
    const client: CatalogueClient = {
      ...createDemoClient(TODAY),
      listLoans: () => Promise.reject(new Error('signed out')),
    };
    renderWithProviders(<LoansPage today={TODAY} />, { client });

    expect(await screen.findByRole('alert')).toHaveTextContent('Your loans could not be loaded.');
  });
});
