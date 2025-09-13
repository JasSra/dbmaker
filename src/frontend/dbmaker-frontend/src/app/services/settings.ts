import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { SystemSettings } from '../models/settings.models';
import { SettingsService as ApiSettingsService } from '../../../api/consolidated';
import type { UpdateSettingsRequest, SettingsResponse } from '../../../api/consolidated';

@Injectable({
  providedIn: 'root'
})
export class SettingsService {
  private settingsSubject = new BehaviorSubject<SystemSettings | null>(null);
  public settings$ = this.settingsSubject.asObservable();

  private darkModeSubject = new BehaviorSubject<boolean>(false);
  public darkMode$ = this.darkModeSubject.asObservable();

  constructor() {
    this.loadGlobalSettings();
  }

  getGlobalSettings(): Observable<SettingsResponse> {
    return ApiSettingsService.getApiSettingsGlobal() as unknown as Observable<SettingsResponse>;
  }

  getUserSettings(): Observable<SettingsResponse> {
    return ApiSettingsService.getApiSettings() as unknown as Observable<SettingsResponse>;
  }

  updateGlobalSettings(settings: UpdateSettingsRequest): Observable<SettingsResponse> {
    return (ApiSettingsService.putApiSettingsGlobal(settings) as unknown as Observable<SettingsResponse>).pipe(
      tap(response => {
        if (response.success) {
          this.settingsSubject.next((response.settings as any) ?? null);
          this.darkModeSubject.next(!!response.settings?.ui?.darkMode);
        }
      })
    );
  }

  updateUserSettings(settings: UpdateSettingsRequest): Observable<SettingsResponse> {
    return (ApiSettingsService.putApiSettings(settings) as unknown as Observable<SettingsResponse>).pipe(
      tap(response => {
        if (response.success) {
          this.settingsSubject.next((response.settings as any) ?? null);
          this.darkModeSubject.next(!!response.settings?.ui?.darkMode);
        }
      })
    );
  }

  toggleDarkMode(): void {
    const currentSettings = this.settingsSubject.value;
    if (currentSettings) {
      const newUISettings = {
        ...currentSettings.ui,
        darkMode: !currentSettings.ui.darkMode,
        theme: !currentSettings.ui.darkMode ? 'dark' : 'default'
      };

    this.updateGlobalSettings({ ui: newUISettings } as any).subscribe({
        next: (response) => {
      this.applyTheme(!!response.settings?.ui?.darkMode);
        },
        error: (error) => {
          console.error('Failed to toggle dark mode:', error);
        }
      });
    }
  }

  setShowAllContainers(showAll: boolean): void {
    const currentSettings = this.settingsSubject.value;
    if (currentSettings) {
      const newContainerSettings = {
        ...currentSettings.containers,
        showAllContainers: showAll
      };

  this.updateGlobalSettings({ containers: newContainerSettings } as any).subscribe({
        error: (error) => {
          console.error('Failed to update container visibility:', error);
        }
      });
    }
  }

  private loadGlobalSettings(): void {
    this.getGlobalSettings().subscribe({
      next: (response) => {
        if (response.success && response.settings) {
          this.settingsSubject.next(response.settings as any);
          const dark = !!response.settings.ui?.darkMode;
          this.darkModeSubject.next(dark);
          this.applyTheme(dark);
        }
      },
      error: (error) => {
        console.error('Failed to load settings:', error);
      }
    });
  }

  private applyTheme(isDark: boolean): void {
    const body = document.body;
    if (isDark) {
      body.classList.add('dark-theme');
    } else {
      body.classList.remove('dark-theme');
    }
  }

  getCurrentSettings(): SystemSettings | null {
    return this.settingsSubject.value;
  }

  isDarkMode(): boolean {
    return this.darkModeSubject.value;
  }
}
