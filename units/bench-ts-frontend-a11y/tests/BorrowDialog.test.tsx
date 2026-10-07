import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import type { CatalogueClient } from '../src/api/catalogueClient';
import { createDemoClient } from '../src/api/demoClient';
import type { CatalogueItem } from '../src/api/types';
import { BorrowDialog } from '../src/features/borrow/BorrowDialog';
import { TODAY, renderWithProviders } from './support/renderWithProviders';

const item = { id: 'b-1001', title: 'The Tide Clock' } as CatalogueItem;

describe('BorrowDialog', () => {
  it('opens as a modal dialog named by its heading', () => {
    renderWithProviders(<BorrowDialog item={item} onClose={vi.fn()} onReserved={vi.fn()} />);

    expect(screen.getByRole('dialog', { name: 'Borrow “The Tide Clock”' })).toHaveAttribute('open');
  });

  it('ties a validation error to the card number field', async () => {
    const user = userEvent.setup();
    renderWithProviders(<BorrowDialog item={item} onClose={vi.fn()} onReserved={vi.fn()} />);

    await user.click(screen.getByRole('button', { name: 'Reserve for pickup' }));

    const field = screen.getByRole('textbox', { name: 'Library card number' });
    expect(field).toHaveAttribute('aria-invalid', 'true');
    expect(field).toHaveAccessibleDescription(
      'The 14 digits under the barcode on your card. Enter your library card number.',
    );
  });

  it('reserves with the chosen branch and closes', async () => {
    const user = userEvent.setup();
    const onReserved = vi.fn();
    const onClose = vi.fn();
    renderWithProviders(<BorrowDialog item={item} onClose={onClose} onReserved={onReserved} />, {
      preferences: { homeBranch: 'hillside', emailReminders: false },
    });

    await user.type(
      screen.getByRole('textbox', { name: 'Library card number' }),
      '1234 5678 9012 34',
    );
    await user.selectOptions(screen.getByRole('combobox', { name: 'Pick up at' }), 'harbour');
    await user.click(
      screen.getByRole('checkbox', { name: 'Email me when it is ready to collect' }),
    );
    await user.click(screen.getByRole('button', { name: 'Reserve for pickup' }));

    expect(onReserved).toHaveBeenCalledWith(
      expect.objectContaining({ itemId: 'b-1001', pickupBranch: 'harbour' }),
    );
    expect(onClose).toHaveBeenCalledOnce();
  });

  it('reports a reservation the library system refused', async () => {
    const user = userEvent.setup();
    const failing: CatalogueClient = {
      ...createDemoClient(TODAY),
      borrow: () => Promise.reject(new Error('refused')),
    };
    renderWithProviders(<BorrowDialog item={item} onClose={vi.fn()} onReserved={vi.fn()} />, {
      client: failing,
    });

    await user.type(screen.getByRole('textbox', { name: 'Library card number' }), '12345678901234');
    await user.click(screen.getByRole('button', { name: 'Reserve for pickup' }));

    expect(
      await screen.findByText('The reservation could not be made. Check the card number.'),
    ).toBeInTheDocument();
  });

  it('closes from the Cancel button and from a backdrop click', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    renderWithProviders(<BorrowDialog item={item} onClose={onClose} onReserved={vi.fn()} />);
    const dialog = screen.getByRole<HTMLDialogElement>('dialog');

    await user.click(screen.getByRole('button', { name: 'Cancel' }));
    expect(onClose).toHaveBeenCalledTimes(1);

    dialog.showModal();
    await user.click(dialog);
    expect(onClose).toHaveBeenCalledTimes(2);
  });
});
