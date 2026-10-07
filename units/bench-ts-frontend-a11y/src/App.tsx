import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { SiteHeader } from './components/SiteHeader';
import { ToastRegion } from './components/ToastRegion';
import {
  PreferencesContext,
  loadPreferences,
  savePreferences,
  type Preferences,
} from './context/settings';
import { ToastContext, useToastState } from './context/toasts';
import { EventsPage } from './features/events/EventsPage';
import { Gallery } from './features/gallery/Gallery';
import { HomePage } from './features/home/HomePage';
import { LoansPage } from './features/loans/LoansPage';
import { NoticeBanner } from './features/news/NoticeBanner';
import { ReadingListPage } from './features/reading-list/ReadingListPage';
import { SearchPage } from './features/search/SearchPage';
import { SettingsPage } from './features/settings/SettingsPage';
import type { Route } from './routing';
import { useRoute } from './useRoute';

function CurrentPage({ route }: { route: Route }) {
  switch (route.page) {
    case 'home':
      return <HomePage />;
    case 'search':
      return <SearchPage openItemId={route.itemId} />;
    case 'events':
      return <EventsPage />;
    case 'gallery':
      return <Gallery />;
    case 'loans':
      return <LoansPage />;
    case 'reading-list':
      return <ReadingListPage />;
    case 'settings':
      return <SettingsPage />;
  }
}

export function App() {
  const route = useRoute();
  const toastApi = useToastState();
  const [preferences, setPreferences] = useState(() => loadPreferences(window.localStorage));
  const mainRef = useRef<HTMLElement>(null);
  const firstRender = useRef(true);

  const update = useCallback((change: Partial<Preferences>) => {
    setPreferences((current) => {
      const next = { ...current, ...change };
      savePreferences(window.localStorage, next);
      return next;
    });
  }, []);
  const preferencesApi = useMemo(() => ({ preferences, update }), [preferences, update]);

  // After an in-app page change, move focus to the new page so screen-reader users hear it.
  useEffect(() => {
    if (firstRender.current) {
      firstRender.current = false;
      return;
    }
    mainRef.current?.focus();
  }, [route.page]);

  return (
    <PreferencesContext value={preferencesApi}>
      <ToastContext value={toastApi}>
        <SiteHeader current={route.page} />
        <main id="main-content" ref={mainRef} tabIndex={-1} className="page">
          <NoticeBanner />
          <CurrentPage route={route} />
        </main>
        <footer className="site-footer">
          <p>Community Library · Central, Harbour and Hillside branches</p>
        </footer>
        <ToastRegion />
      </ToastContext>
    </PreferencesContext>
  );
}
