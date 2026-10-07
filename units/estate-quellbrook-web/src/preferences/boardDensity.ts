export type BoardDensity = 'comfortable' | 'compact';

const storageKey = 'quellbrook.board-density';

/** The dispatcher's preferred card density for the board; a display preference kept in this browser only. */
export function loadBoardDensity(): BoardDensity {
  try {
    return window.localStorage.getItem(storageKey) === 'compact' ? 'compact' : 'comfortable';
  } catch {
    return 'comfortable';
  }
}

export function saveBoardDensity(density: BoardDensity): void {
  try {
    window.localStorage.setItem(storageKey, density);
  } catch {
    // Storage can be unavailable (private browsing, quota); the preference then lasts for this page only.
  }
}
