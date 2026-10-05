import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'pjmTheme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly isDark = signal(false);

  constructor() {
    let saved: string | null = null;
    try {
      saved = localStorage.getItem(STORAGE_KEY);
    } catch {
      // Storage can be blocked (private mode); fall back to the system preference.
    }
    const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
    this.apply(saved ? saved === 'dark' : prefersDark);
  }

  toggle() {
    this.apply(!this.isDark());
    try {
      localStorage.setItem(STORAGE_KEY, this.isDark() ? 'dark' : 'light');
    } catch {
      // Not persisting the choice is acceptable.
    }
  }

  private apply(dark: boolean) {
    document.documentElement.classList.toggle('app-dark', dark);
    this.isDark.set(dark);
  }
}
