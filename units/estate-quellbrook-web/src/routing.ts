import { useEffect, useState } from 'react';

export type Route =
  | { readonly name: 'orders' }
  | { readonly name: 'order'; readonly orderId: string }
  | { readonly name: 'dispatch' }
  | { readonly name: 'not-found' };

export function parseRoute(pathname: string): Route {
  if (pathname === '/' || pathname === '/orders') {
    return { name: 'orders' };
  }
  const order = /^\/orders\/([0-9a-f-]{36})$/.exec(pathname);
  if (order?.[1]) {
    return { name: 'order', orderId: order[1] };
  }
  if (pathname === '/dispatch') {
    return { name: 'dispatch' };
  }
  return { name: 'not-found' };
}

export function navigate(path: string): void {
  window.history.pushState(null, '', path);
  window.dispatchEvent(new PopStateEvent('popstate'));
}

export function useRoute(): Route {
  const [route, setRoute] = useState(() => parseRoute(window.location.pathname));
  useEffect(() => {
    const update = () => {
      setRoute(parseRoute(window.location.pathname));
    };
    window.addEventListener('popstate', update);
    return () => {
      window.removeEventListener('popstate', update);
    };
  }, []);
  return route;
}
