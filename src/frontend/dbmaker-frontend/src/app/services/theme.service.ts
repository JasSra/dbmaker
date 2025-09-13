import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ThemeConfig {
  isDarkMode: boolean;
  primaryColor: string;
  accentColor: string;
  autoDetectTheme: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'starklink-theme-config';
  private readonly themeSubject = new BehaviorSubject<ThemeConfig>(this.getInitialTheme());

  public readonly theme$ = this.themeSubject.asObservable();

  constructor() {
    this.initializeTheme();
  }

  private getInitialTheme(): ThemeConfig {
    const stored = localStorage.getItem(this.THEME_KEY);
    if (stored) {
      return JSON.parse(stored);
    }

    return {
      isDarkMode: this.detectSystemDarkMode(),
      primaryColor: '#4fc3f7',
      accentColor: '#667eea',
      autoDetectTheme: true
    };
  }

  private initializeTheme(): void {
    const theme = this.themeSubject.value;
    this.applyTheme(theme);

    // Listen for system theme changes
    if (theme.autoDetectTheme) {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
        if (this.themeSubject.value.autoDetectTheme) {
          this.setDarkMode(e.matches);
        }
      });
    }
  }

  private detectSystemDarkMode(): boolean {
    return window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
  }

  getCurrentTheme(): ThemeConfig {
    return this.themeSubject.value;
  }

  setDarkMode(isDarkMode: boolean): void {
    const currentTheme = this.themeSubject.value;
    const newTheme = { ...currentTheme, isDarkMode, autoDetectTheme: false };
    this.updateTheme(newTheme);
  }

  toggleDarkMode(): void {
    const currentTheme = this.themeSubject.value;
    this.setDarkMode(!currentTheme.isDarkMode);
  }

  setAutoDetectTheme(autoDetect: boolean): void {
    const currentTheme = this.themeSubject.value;
    const newTheme = { ...currentTheme, autoDetectTheme: autoDetect };

    if (autoDetect) {
      newTheme.isDarkMode = this.detectSystemDarkMode();
    }

    this.updateTheme(newTheme);
  }

  setPrimaryColor(color: string): void {
    const currentTheme = this.themeSubject.value;
    const newTheme = { ...currentTheme, primaryColor: color };
    this.updateTheme(newTheme);
  }

  setAccentColor(color: string): void {
    const currentTheme = this.themeSubject.value;
    const newTheme = { ...currentTheme, accentColor: color };
    this.updateTheme(newTheme);
  }

  private updateTheme(theme: ThemeConfig): void {
    this.themeSubject.next(theme);
    this.applyTheme(theme);
    localStorage.setItem(this.THEME_KEY, JSON.stringify(theme));
  }

  private applyTheme(theme: ThemeConfig): void {
    const root = document.documentElement;

    // Remove existing theme classes
    document.body.classList.remove('light-theme', 'dark-theme');

    // Apply new theme class
    document.body.classList.add(theme.isDarkMode ? 'dark-theme' : 'light-theme');

    // Set CSS custom properties
    root.style.setProperty('--primary-color', theme.primaryColor);
    root.style.setProperty('--accent-color', theme.accentColor);

    // Set theme-specific properties
    if (theme.isDarkMode) {
      root.style.setProperty('--bg-primary', '#0f172a');
      root.style.setProperty('--bg-secondary', '#111827');
      root.style.setProperty('--bg-tertiary', '#1f2937');
      root.style.setProperty('--text-primary', '#f8fafc');
      root.style.setProperty('--text-secondary', '#e2e8f0');
      root.style.setProperty('--text-tertiary', '#94a3b8');
      root.style.setProperty('--border-color', '#374151');
      root.style.setProperty('--card-bg', '#1e293b');
      root.style.setProperty('--shadow-color', 'rgba(0, 0, 0, 0.5)');
      root.style.setProperty('--hero-start', '#111827');
      root.style.setProperty('--hero-end', '#0b1220');
      root.style.setProperty('--header-start', '#0b1220');
      root.style.setProperty('--header-end', '#0b2440');
    } else {
      root.style.setProperty('--bg-primary', '#ffffff');
      root.style.setProperty('--bg-secondary', '#f8fafc');
      root.style.setProperty('--bg-tertiary', '#f1f5f9');
      root.style.setProperty('--text-primary', '#1e293b');
      root.style.setProperty('--text-secondary', '#475569');
      root.style.setProperty('--text-tertiary', '#64748b');
      root.style.setProperty('--border-color', '#e2e8f0');
      root.style.setProperty('--card-bg', '#ffffff');
      root.style.setProperty('--shadow-color', 'rgba(0, 0, 0, 0.1)');
      root.style.setProperty('--hero-start', '#667eea');
      root.style.setProperty('--hero-end', '#764ba2');
      root.style.setProperty('--header-start', '#1e3c72');
      root.style.setProperty('--header-end', '#2a5298');
    }
  }

  // Convenience methods
  isDarkMode(): Observable<boolean> {
    return new Observable(subscriber => {
      this.theme$.subscribe(theme => subscriber.next(theme.isDarkMode));
    });
  }

  isLightMode(): Observable<boolean> {
    return new Observable(subscriber => {
      this.theme$.subscribe(theme => subscriber.next(!theme.isDarkMode));
    });
  }
}
