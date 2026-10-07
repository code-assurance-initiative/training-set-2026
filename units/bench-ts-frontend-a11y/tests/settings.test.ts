import { describe, expect, it } from 'vitest';
import { DEFAULT_PREFERENCES, loadPreferences, savePreferences } from '../src/context/settings';

describe('preferences storage', () => {
  it('falls back to the defaults when nothing is stored', () => {
    expect(loadPreferences(window.localStorage)).toEqual(DEFAULT_PREFERENCES);
  });

  it('round-trips saved preferences', () => {
    const changed = { ...DEFAULT_PREFERENCES, compactResults: true, reminderDays: 7 };
    savePreferences(window.localStorage, changed);
    expect(loadPreferences(window.localStorage)).toEqual(changed);
  });

  it('ignores a corrupt stored value', () => {
    window.localStorage.setItem('catalogue.preferences', '{not json');
    expect(loadPreferences(window.localStorage)).toEqual(DEFAULT_PREFERENCES);
  });

  it('fills in preferences added since the value was stored', () => {
    window.localStorage.setItem('catalogue.preferences', JSON.stringify({ compactResults: true }));
    expect(loadPreferences(window.localStorage)).toEqual({
      ...DEFAULT_PREFERENCES,
      compactResults: true,
    });
  });
});
