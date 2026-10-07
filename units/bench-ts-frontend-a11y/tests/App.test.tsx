import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it } from 'vitest';
import { createDemoClient } from '../src/api/demoClient';
import { App } from '../src/App';
import { CatalogueClientContext } from '../src/context/catalogueClient';
import { TODAY } from './support/renderWithProviders';

function renderApp() {
  return render(
    <CatalogueClientContext value={createDemoClient(TODAY)}>
      <App />
    </CatalogueClientContext>,
  );
}

describe('App', () => {
  it('starts on the home page with one main landmark', () => {
    renderApp();

    expect(screen.getByRole('main')).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent(
      'Your library, open to everyone',
    );
    expect(screen.getByRole('link', { name: 'Skip to main content' })).toHaveAttribute(
      'href',
      '#main-content',
    );
  });

  it('navigates between pages from the main navigation and focuses the new page', async () => {
    const user = userEvent.setup();
    renderApp();
    const nav = screen.getByRole('navigation', { name: 'Main' });

    for (const [link, heading] of [
      ['Events', 'Events'],
      ['Photo gallery', 'Photo gallery'],
      ['My loans', 'My loans'],
      ['Reading list', 'Reading list'],
      ['Settings', 'Settings'],
      ['Search the catalogue', 'Search the catalogue'],
    ] as const) {
      await user.click(within(nav).getByRole('link', { name: link }));
      expect(await screen.findByRole('heading', { level: 1, name: heading })).toBeInTheDocument();
      expect(within(nav).getByRole('link', { name: link })).toHaveAttribute('aria-current', 'page');
      expect(screen.getByRole('main')).toHaveFocus();
    }
  });

  it('follows the browser back button', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(screen.getByRole('link', { name: 'See all events' }));
    expect(await screen.findByRole('heading', { level: 1, name: 'Events' })).toBeInTheDocument();

    window.history.back();
    expect(
      await screen.findByRole('heading', { level: 1, name: /Your library/ }),
    ).toBeInTheDocument();
  });

  it('keeps preferences across visits', async () => {
    const user = userEvent.setup();
    const first = renderApp();
    await user.click(screen.getByRole('link', { name: 'Settings' }));
    await user.click(await screen.findByRole('switch', { name: 'Compact result list' }));
    first.unmount();

    window.history.replaceState(null, '', '/search');
    renderApp();

    await screen.findAllByRole('article');
    expect(document.querySelector('.result-list')).toHaveClass('result-list--compact');
  });

  it('shows a confirmation toast that can be dismissed', async () => {
    const user = userEvent.setup();
    window.history.replaceState(null, '', '/settings');
    renderApp();

    await user.click(screen.getByRole('button', { name: 'Save reminder preferences' }));
    const toasts = screen.getByRole('status');
    expect(toasts).toHaveTextContent('Your reminder preferences were saved.');

    await user.click(within(toasts).getByRole('button'));
    expect(toasts).toBeEmptyDOMElement();
  });

  it('shows the storytime recording and upcoming events', async () => {
    window.history.replaceState(null, '', '/events');
    renderApp();

    expect(await screen.findByRole('heading', { name: 'Coming up' })).toBeInTheDocument();
    expect(screen.getAllByRole('listitem').length).toBeGreaterThanOrEqual(3);
    expect(screen.getByText('Storytime, 30 September: The Lighthouse Cat')).toBeInTheDocument();
  });
});
