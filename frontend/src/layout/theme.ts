export type Theme = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'nimi.theme';

export function savedTheme(): Theme {
  try {
    const value = localStorage.getItem(STORAGE_KEY);
    return value === 'light' || value === 'dark' ? value : 'system';
  } catch {
    return 'system';
  }
}

/** "system" leaves the choice to the OS (prefers-color-scheme); light/dark pin it with data-theme. */
export function applyTheme(theme: Theme) {
  if (theme === 'system') document.documentElement.removeAttribute('data-theme');
  else document.documentElement.setAttribute('data-theme', theme);
  try {
    localStorage.setItem(STORAGE_KEY, theme);
  } catch {
    // ignored: the choice lasts for this visit
  }
}
