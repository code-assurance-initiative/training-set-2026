export type Route =
  | { readonly page: 'home' }
  | { readonly page: 'search'; readonly itemId: string | null }
  | { readonly page: 'gallery' }
  | { readonly page: 'events' }
  | { readonly page: 'loans' }
  | { readonly page: 'reading-list' }
  | { readonly page: 'settings' };

export type PageName = Route['page'];

export const NAVIGATION: readonly { readonly page: PageName; readonly label: string }[] = [
  { page: 'home', label: 'Home' },
  { page: 'search', label: 'Search the catalogue' },
  { page: 'events', label: 'Events' },
  { page: 'gallery', label: 'Photo gallery' },
  { page: 'loans', label: 'My loans' },
  { page: 'reading-list', label: 'Reading list' },
  { page: 'settings', label: 'Settings' },
];

/** Reads the route from a path such as `/search/b-1001`. Unknown paths go home. */
export function parseRoute(pathname: string): Route {
  const [page = '', id] = pathname.replace(/^\/+/, '').split('/');
  switch (page) {
    case 'search':
      return { page: 'search', itemId: id ? decodeURIComponent(id) : null };
    case 'gallery':
    case 'events':
    case 'loans':
    case 'reading-list':
    case 'settings':
      return { page };
    default:
      return { page: 'home' };
  }
}

export function hrefFor(page: PageName, itemId?: string): string {
  if (page === 'home') {
    return '/';
  }
  return itemId ? `/${page}/${encodeURIComponent(itemId)}` : `/${page}`;
}

export const NAVIGATE_EVENT = 'catalogue:navigate';

/** Moves to `href` without a page load; `useRoute` picks the change up. */
export function navigate(href: string): void {
  window.history.pushState(null, '', href);
  window.dispatchEvent(new Event(NAVIGATE_EVENT));
}
