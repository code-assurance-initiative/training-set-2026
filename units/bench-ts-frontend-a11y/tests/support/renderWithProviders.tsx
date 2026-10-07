import { render, type RenderResult } from '@testing-library/react';
import { useMemo, useState, type ReactElement, type ReactNode } from 'react';
import type { CatalogueClient } from '../../src/api/catalogueClient';
import { createDemoClient } from '../../src/api/demoClient';
import { CatalogueClientContext } from '../../src/context/catalogueClient';
import {
  DEFAULT_PREFERENCES,
  PreferencesContext,
  type Preferences,
} from '../../src/context/settings';
import { ToastContext, useToastState } from '../../src/context/toasts';
import { ToastRegion } from '../../src/components/ToastRegion';

export const TODAY = new Date('2026-10-07T09:00:00Z');

interface Options {
  readonly client?: CatalogueClient;
  readonly preferences?: Partial<Preferences>;
}

function Providers({
  client,
  initial,
  children,
}: {
  client: CatalogueClient;
  initial: Preferences;
  children: ReactNode;
}) {
  const [preferences, setPreferences] = useState(initial);
  const preferencesApi = useMemo(
    () => ({
      preferences,
      update: (change: Partial<Preferences>) => {
        setPreferences((current) => ({ ...current, ...change }));
      },
    }),
    [preferences],
  );
  const toasts = useToastState();
  return (
    <CatalogueClientContext value={client}>
      <PreferencesContext value={preferencesApi}>
        <ToastContext value={toasts}>
          {children}
          <ToastRegion />
        </ToastContext>
      </PreferencesContext>
    </CatalogueClientContext>
  );
}

export function renderWithProviders(ui: ReactElement, options: Options = {}): RenderResult {
  const client = options.client ?? createDemoClient(TODAY);
  const initial = { ...DEFAULT_PREFERENCES, ...options.preferences };
  return render(
    <Providers client={client} initial={initial}>
      {ui}
    </Providers>,
  );
}
