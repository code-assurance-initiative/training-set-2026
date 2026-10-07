import { useEffect, useState } from 'react';
import { NAVIGATE_EVENT, parseRoute, type Route } from './routing';

/** The current route, kept in sync with in-app navigation and the browser's back/forward buttons. */
export function useRoute(): Route {
  const [route, setRoute] = useState(() => parseRoute(window.location.pathname));

  useEffect(() => {
    const onChange = () => {
      setRoute(parseRoute(window.location.pathname));
    };
    window.addEventListener('popstate', onChange);
    window.addEventListener(NAVIGATE_EVENT, onChange);
    return () => {
      window.removeEventListener('popstate', onChange);
      window.removeEventListener(NAVIGATE_EVENT, onChange);
    };
  }, []);

  return route;
}
