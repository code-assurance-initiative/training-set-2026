import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { CatalogueClient } from '../src/api/catalogueClient';
import { createDemoClient } from '../src/api/demoClient';
import { NoticeBanner } from '../src/features/news/NoticeBanner';
import { TODAY, renderWithProviders } from './support/renderWithProviders';

function clientWithNotice(bodyHtml: string): CatalogueClient {
  return {
    ...createDemoClient(TODAY),
    currentNotice: () => Promise.resolve({ id: 'n-1', title: 'Notice', bodyHtml }),
  };
}

describe('NoticeBanner', () => {
  it('shows the current notice with its formatting', async () => {
    renderWithProviders(<NoticeBanner />);

    const notice = await screen.findByRole('complementary', { name: 'Library notice' });
    expect(notice).toHaveTextContent('Hillside Branch closed on Monday');
    expect(notice.querySelector('strong')).toHaveTextContent('Monday 12 October');
  });

  it('strips script, event handlers and javascript: links from the notice', async () => {
    renderWithProviders(<NoticeBanner />, {
      client: clientWithNotice(
        '<p onclick="steal()">Read <a href="javascript:steal()">this</a><script>steal()</script></p><img src=x onerror="steal()">',
      ),
    });

    const notice = await screen.findByRole('complementary', { name: 'Library notice' });
    expect(notice.innerHTML).not.toMatch(/script|onclick|onerror|javascript:|<img/);
    expect(notice).toHaveTextContent('Read this');
  });

  it('renders nothing when there is no notice', async () => {
    const client: CatalogueClient = {
      ...createDemoClient(TODAY),
      currentNotice: () => Promise.resolve(null),
    };
    const { container } = renderWithProviders(<NoticeBanner />, { client });

    await Promise.resolve();
    expect(container.querySelector('aside')).toBeNull();
  });
});
