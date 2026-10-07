import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { SearchPage } from '../src/features/search/SearchPage';
import { renderWithProviders } from './support/renderWithProviders';

describe('SearchPage', () => {
  it('lists the first page of results and loads more on request', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId={null} />);

    expect(await screen.findByRole('heading', { name: '7 results' })).toBeInTheDocument();
    expect(screen.getAllByRole('article')).toHaveLength(5);

    await user.click(screen.getByRole('button', { name: 'Load more results' }));

    expect(await screen.findAllByRole('article')).toHaveLength(7);
    expect(screen.queryByRole('button', { name: 'Load more results' })).not.toBeInTheDocument();
  });

  it('searches for the submitted text and remembers it', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId={null} />);

    await user.type(
      screen.getByRole('searchbox', { name: 'Search by title, author or subject' }),
      'tide',
    );
    await user.click(screen.getByRole('button', { name: 'Search' }));

    expect(await screen.findByRole('heading', { name: '2 results' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'tide' })).toBeInTheDocument();

    await user.click(screen.getByRole('link', { name: 'Clear recent searches' }));
    expect(screen.queryByRole('button', { name: 'tide' })).not.toBeInTheDocument();
  });

  it('toggles a format chip from the keyboard', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId={null} />);
    const dvd = await screen.findByRole('checkbox', { name: 'DVD' });

    dvd.focus();
    await user.keyboard(' ');

    expect(dvd).toHaveAttribute('aria-checked', 'true');
    expect(await screen.findByRole('heading', { name: '1 result' })).toBeInTheDocument();

    await user.keyboard('{Enter}');
    expect(dvd).toHaveAttribute('aria-checked', 'false');
  });

  it('narrows to available items and browses by subject', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId={null} />);

    await user.click(await screen.findByRole('checkbox', { name: 'Available now only' }));
    expect(await screen.findByRole('heading', { name: '6 results' })).toBeInTheDocument();

    await user.selectOptions(screen.getByRole('combobox', { name: 'Browse by subject' }), 'Birds');
    expect(await screen.findByRole('heading', { name: '1 result' })).toBeInTheDocument();
  });

  it('opens an item from its card button', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId={null} />);
    const card = (await screen.findAllByRole('article'))[0];
    if (!card) {
      throw new Error('expected a result card');
    }

    await user.click(within(card).getByRole('button', { name: /View details/ }));

    expect(window.location.pathname).toBe('/search/b-1001');
  });

  it('opens an item when the card itself is clicked', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId={null} />);

    await user.click(await screen.findByText('Local History Quarterly, Spring issue'));

    expect(window.location.pathname).toBe('/search/m-4001');
  });

  it('shows the opened item and borrows it through the form', async () => {
    const user = userEvent.setup();
    renderWithProviders(<SearchPage openItemId="b-1001" />);

    await user.click(await screen.findByRole('button', { name: 'Borrow this item' }));
    await user.type(screen.getByRole('textbox', { name: 'Library card number' }), '12345678901234');
    await user.click(screen.getByRole('button', { name: 'Reserve for pickup' }));

    expect(await screen.findByText(/Reserved “The Tide Clock”/)).toBeInTheDocument();
  });

  it('closes the item dialog', async () => {
    const user = userEvent.setup();
    window.history.replaceState(null, '', '/search/b-1001');
    renderWithProviders(<SearchPage openItemId="b-1001" />);

    await user.click(await screen.findByRole('button', { name: 'Close' }));

    expect(window.location.pathname).toBe('/search');
  });
});
