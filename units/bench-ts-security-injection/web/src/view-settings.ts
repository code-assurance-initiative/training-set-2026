export interface ViewSettings {
  readonly pageSize: 10 | 25 | 50 | 100;
  readonly theme: 'light' | 'dark' | 'system';
}

const storageKey = 'archive.viewSettings';
const pageSizes = [10, 25, 50, 100] as const;
const themes = ['light', 'dark', 'system'] as const;

export const defaultViewSettings: ViewSettings = { pageSize: 25, theme: 'system' };

export function loadViewSettings(): ViewSettings {
  try {
    const stored = JSON.parse(localStorage.getItem(storageKey) ?? '{}') as Partial<
      Record<string, unknown>
    >;
    const pageSize =
      pageSizes.find((size) => size === stored.pageSize) ?? defaultViewSettings.pageSize;
    const theme = themes.find((name) => name === stored.theme) ?? defaultViewSettings.theme;
    return { pageSize, theme };
  } catch (error) {
    console.warn('Discarding unreadable view settings', error);
    localStorage.removeItem(storageKey);
    return defaultViewSettings;
  }
}

export function saveViewSettings(settings: ViewSettings): void {
  localStorage.setItem(
    storageKey,
    JSON.stringify({ pageSize: settings.pageSize, theme: settings.theme }),
  );
}
