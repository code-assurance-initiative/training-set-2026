import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { Gallery } from '../src/features/gallery/Gallery';

describe('Gallery', () => {
  it('opens a photo, steps through the set and closes', async () => {
    const user = userEvent.setup();
    render(<Gallery />);

    await user.click(screen.getByRole('img', { name: /Volunteers sorting donated books/ }));
    expect(screen.getByText('Spring book sale, Central Library')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next photo' }));
    expect(screen.getByText('Thursday coding club, Harbour Branch')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Previous photo' }));
    await user.click(screen.getByRole('button', { name: 'Previous photo' }));
    expect(screen.getByText('The new reading garden at Hillside Branch')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Close photo' }));
    expect(screen.queryByRole('figure')).not.toBeInTheDocument();
  });

  it('steps to the next photo with the right arrow key', async () => {
    const user = userEvent.setup();
    render(<Gallery />);

    await user.click(screen.getByRole('img', { name: /Volunteers sorting donated books/ }));
    await user.keyboard('{ArrowRight}');
    expect(screen.getByText('Thursday coding club, Harbour Branch')).toBeInTheDocument();
  });
});
