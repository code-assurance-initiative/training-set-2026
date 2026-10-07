import { createContext, useContext } from 'react';

export interface Preferences {
  readonly compactResults: boolean;
  readonly emailReminders: boolean;
  readonly reminderDays: number;
  readonly homeBranch: 'central' | 'harbour' | 'hillside';
}

export const DEFAULT_PREFERENCES: Preferences = {
  compactResults: false,
  emailReminders: true,
  reminderDays: 3,
  homeBranch: 'central',
};

export interface PreferencesApi {
  readonly preferences: Preferences;
  readonly update: (change: Partial<Preferences>) => void;
}

export const PreferencesContext = createContext<PreferencesApi | null>(null);

export function usePreferences(): PreferencesApi {
  const api = useContext(PreferencesContext);
  if (!api) {
    throw new Error('usePreferences must be used inside a PreferencesContext provider');
  }
  return api;
}

const STORAGE_KEY = 'catalogue.preferences';

export function loadPreferences(storage: Pick<Storage, 'getItem'>): Preferences {
  const raw = storage.getItem(STORAGE_KEY);
  if (raw === null) {
    return DEFAULT_PREFERENCES;
  }
  try {
    const parsed = JSON.parse(raw) as Partial<Preferences>;
    return { ...DEFAULT_PREFERENCES, ...parsed };
  } catch (error) {
    if (error instanceof SyntaxError) {
      return DEFAULT_PREFERENCES;
    }
    throw error;
  }
}

export function savePreferences(storage: Pick<Storage, 'setItem'>, preferences: Preferences): void {
  storage.setItem(STORAGE_KEY, JSON.stringify(preferences));
}
