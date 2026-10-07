import { describe, expect, it, vi } from 'vitest';
import { NAVIGATE_EVENT, hrefFor, navigate, parseRoute } from '../src/routing';

describe('parseRoute', () => {
  it('reads each page from its path', () => {
    expect(parseRoute('/')).toEqual({ page: 'home' });
    expect(parseRoute('/search')).toEqual({ page: 'search', itemId: null });
    expect(parseRoute('/events')).toEqual({ page: 'events' });
    expect(parseRoute('/gallery')).toEqual({ page: 'gallery' });
    expect(parseRoute('/loans')).toEqual({ page: 'loans' });
    expect(parseRoute('/reading-list')).toEqual({ page: 'reading-list' });
    expect(parseRoute('/settings')).toEqual({ page: 'settings' });
  });

  it('reads an opened item from the search path', () => {
    expect(parseRoute('/search/b%201')).toEqual({ page: 'search', itemId: 'b 1' });
  });

  it('sends unknown paths home', () => {
    expect(parseRoute('/nowhere/at/all')).toEqual({ page: 'home' });
  });
});

describe('hrefFor', () => {
  it('builds paths that parseRoute reads back', () => {
    expect(hrefFor('home')).toBe('/');
    expect(hrefFor('loans')).toBe('/loans');
    expect(parseRoute(hrefFor('search', 'a/b'))).toEqual({ page: 'search', itemId: 'a/b' });
  });
});

describe('navigate', () => {
  it('pushes the path and announces the change', () => {
    const listener = vi.fn();
    window.addEventListener(NAVIGATE_EVENT, listener);
    navigate('/gallery');
    window.removeEventListener(NAVIGATE_EVENT, listener);

    expect(window.location.pathname).toBe('/gallery');
    expect(listener).toHaveBeenCalledOnce();
  });
});
