import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { ReadingListPage } from '../src/features/reading-list/ReadingListPage';
import { loadReadingList } from '../src/features/reading-list/readingList';

async function addBook(user: ReturnType<typeof userEvent.setup>, title: string, author = '') {
  await user.type(screen.getByLabelText('Title'), title);
  if (author) {
    await user.type(screen.getByLabelText('Author (optional)'), author);
  }
  await user.click(screen.getByRole('button', { name: 'Add to reading list' }));
}

function listedTitles(): string[] {
  const list = document.querySelector('.reading-list__entries');
  if (!(list instanceof HTMLElement)) {
    return [];
  }
  return within(list)
    .getAllByRole('listitem')
    .map((item) => item.querySelector('strong')?.textContent ?? '');
}

describe('ReadingListPage', () => {
  it('adds books and keeps them in this browser', async () => {
    const user = userEvent.setup();
    render(<ReadingListPage />);
    expect(screen.getByText('Your reading list is empty.')).toBeInTheDocument();

    await addBook(user, 'The Lighthouse Cat', 'Ann Brook');
    await addBook(user, 'Moon Gardens');

    expect(listedTitles()).toEqual(['The Lighthouse Cat', 'Moon Gardens']);
    expect(screen.getByText('by Ann Brook', { exact: false })).toBeInTheDocument();
    expect(loadReadingList(window.localStorage).map((entry) => entry.title)).toEqual([
      'The Lighthouse Cat',
      'Moon Gardens',
    ]);
  });

  it('asks for a title before adding', async () => {
    const user = userEvent.setup();
    render(<ReadingListPage />);

    await user.click(screen.getByRole('button', { name: 'Add to reading list' }));

    expect(screen.getByText('Enter the title of the book.')).toBeInTheDocument();
    expect(loadReadingList(window.localStorage)).toEqual([]);
  });

  it('reorders, finishes and removes books', async () => {
    const user = userEvent.setup();
    render(<ReadingListPage />);
    await addBook(user, 'First Light');
    await addBook(user, 'Second Wind');

    await user.click(screen.getByRole('button', { name: /^Move up\s*Second Wind$/ }));
    expect(listedTitles()).toEqual(['Second Wind', 'First Light']);

    await user.click(screen.getByRole('button', { name: /^Mark as finished:\s*First Light$/ }));
    expect(screen.getByText(/\(finished\)/)).toBeInTheDocument();

    await user.click(screen.getByLabelText('Hide finished books'));
    expect(listedTitles()).toEqual(['Second Wind']);

    await user.click(screen.getByRole('button', { name: /^Remove\s*Second Wind$/ }));
    expect(screen.getByText('Your reading list is empty.')).toBeInTheDocument();
  });

  it('saves a note on a book', async () => {
    const user = userEvent.setup();
    render(<ReadingListPage />);
    await addBook(user, 'Harbour Tales');

    await user.type(screen.getByLabelText('Note on Harbour Tales'), 'Recommended by Priya');
    await user.click(screen.getByRole('button', { name: /^Save note\s*on Harbour Tales$/ }));

    expect(loadReadingList(window.localStorage)[0]?.note).toBe('Recommended by Priya');
  });
});
