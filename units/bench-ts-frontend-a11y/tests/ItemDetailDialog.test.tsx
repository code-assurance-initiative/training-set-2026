import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { createDemoClient } from '../src/api/demoClient';
import { ItemDetailDialog } from '../src/features/item/ItemDetailDialog';
import { TODAY, renderWithProviders } from './support/renderWithProviders';

describe('ItemDetailDialog', () => {
  it('shows the record of the item', async () => {
    renderWithProviders(<ItemDetailDialog itemId="d-3001" onClose={vi.fn()} onBorrow={vi.fn()} />);

    const dialog = await screen.findByRole('dialog', { name: 'Birds of the Estuary' });
    expect(dialog).toHaveTextContent('Northshore Wildlife Trust · 2018 · DVD');
    expect(dialog).toHaveTextContent('A year on the mudflats, filmed over four seasons.');
    expect(dialog).toHaveTextContent('DVD 598 BIR');
  });

  it('hands the item to the borrow flow', async () => {
    const user = userEvent.setup();
    const onBorrow = vi.fn();
    renderWithProviders(<ItemDetailDialog itemId="b-1004" onClose={vi.fn()} onBorrow={onBorrow} />);

    await user.click(await screen.findByRole('button', { name: 'Borrow this item' }));

    expect(onBorrow).toHaveBeenCalledWith(expect.objectContaining({ id: 'b-1004' }));
  });

  it('says so when the item cannot be loaded', async () => {
    renderWithProviders(
      <ItemDetailDialog itemId="missing" onClose={vi.fn()} onBorrow={vi.fn()} />,
      {
        client: createDemoClient(TODAY),
      },
    );

    expect(await screen.findByRole('alert')).toHaveTextContent('This item could not be loaded.');
  });
});
