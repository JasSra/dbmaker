import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { SettingsService } from '../services/settings';
import { SystemSettings } from '../models/settings.models';
import { ThemeService, ThemeConfig } from '../services/theme.service';

@Component({
  selector: 'app-settings',
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatExpansionModule,
    MatSlideToggleModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule
  ],
  templateUrl: './settings.html',
  styleUrl: './settings.scss'
})
export class SettingsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  // UI Settings
  isDarkMode = false;
  refreshInterval = 30;
  enableAnimations = true;

  // Container Settings
  showAllContainers = false;
  showSystemContainers = false;
  enableVisualization = true;

  // Docker Settings
  dockerHost = 'npipe://./pipe/docker_engine';
  enableMaintenance = true;
  maintenanceInterval = 3600;

  // Nginx Settings
  enableDynamicSubdomains = true;
  baseDomain = 'starklink.local';
  nginxPort = 8080;
  useGuidSubdomains = true;

  saving = false;
  currentTheme: ThemeConfig;

  constructor(
    private settingsService: SettingsService,
    private themeService: ThemeService,
    private snackBar: MatSnackBar
  ) {
    this.currentTheme = this.themeService.getCurrentTheme();
    this.isDarkMode = this.currentTheme.isDarkMode;
  }

  ngOnInit(): void {
    this.settingsService.settings$
      .pipe(takeUntil(this.destroy$))
      .subscribe(settings => {
        if (settings) {
          this.loadSettingsFromResponse(settings);
        }
      });

    // Subscribe to theme changes
    this.themeService.theme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(theme => {
        this.currentTheme = theme;
        this.isDarkMode = theme.isDarkMode;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadSettingsFromResponse(settings: SystemSettings): void {
    // UI Settings
    this.isDarkMode = settings.ui.darkMode;
    this.refreshInterval = settings.ui.refreshInterval;
    this.enableAnimations = settings.ui.enableAnimations;

    // Container Settings
    this.showAllContainers = settings.containers.showAllContainers;
    this.showSystemContainers = settings.containers.showSystemContainers;
    this.enableVisualization = settings.containers.enableVisualization;

    // Docker Settings
    this.dockerHost = settings.docker.defaultHost;
    this.enableMaintenance = settings.docker.enableMaintenance;
    this.maintenanceInterval = settings.docker.maintenanceInterval;

    // Nginx Settings
    this.enableDynamicSubdomains = settings.nginx.enableDynamicSubdomains;
    this.baseDomain = settings.nginx.baseDomain;
    this.nginxPort = settings.nginx.listenPort;
    this.useGuidSubdomains = settings.nginx.useGuidSubdomains;
  }

  toggleDarkMode(): void {
    this.themeService.toggleDarkMode();
    this.updateUISettings();
  }

  onDarkModeChange(event: any): void {
    this.themeService.setDarkMode(event.checked);
    this.updateUISettings();
  }

  updateRefreshInterval(): void {
    this.updateUISettings();
  }

  updateAnimations(): void {
    this.updateUISettings();
  }

  updateShowAllContainers(): void {
    this.settingsService.setShowAllContainers(this.showAllContainers);
    this.updateContainerSettings();
  }

  updateShowSystemContainers(): void {
    this.updateContainerSettings();
  }

  updateVisualization(): void {
    this.updateContainerSettings();
  }

  updateDockerHost(): void {
    this.updateDockerSettings();
  }

  updateMaintenance(): void {
    this.updateDockerSettings();
  }

  updateMaintenanceInterval(): void {
    this.updateDockerSettings();
  }

  updateDynamicSubdomains(): void {
    this.updateNginxSettings();
  }

  updateBaseDomain(): void {
    this.updateNginxSettings();
  }

  updateNginxPort(): void {
    this.updateNginxSettings();
  }

  updateGuidSubdomains(): void {
    this.updateNginxSettings();
  }

  private updateUISettings(): void {
    const uiSettings = {
      darkMode: this.isDarkMode,
      theme: this.isDarkMode ? 'dark' : 'default',
      enableAnimations: this.enableAnimations,
      refreshInterval: this.refreshInterval
    };

    this.settingsService.updateGlobalSettings({ ui: uiSettings }).subscribe({
      next: () => {
        this.showSuccessMessage('UI settings updated successfully');
      },
      error: (error) => {
        this.showErrorMessage('Failed to update UI settings');
        console.error(error);
      }
    });
  }

  private updateContainerSettings(): void {
    const containerSettings = {
      showAllContainers: this.showAllContainers,
      showSystemContainers: this.showSystemContainers,
      enableVisualization: this.enableVisualization,
      hiddenContainers: []
    };

    this.settingsService.updateGlobalSettings({ containers: containerSettings }).subscribe({
      next: () => {
        this.showSuccessMessage('Container settings updated successfully');
      },
      error: (error) => {
        this.showErrorMessage('Failed to update container settings');
        console.error(error);
      }
    });
  }

  private updateDockerSettings(): void {
    const dockerSettings = {
      defaultHost: this.dockerHost,
      enableMaintenance: this.enableMaintenance,
      autoCleanup: this.enableMaintenance,
      maintenanceInterval: this.maintenanceInterval,
      remoteHosts: [],
      currentRemoteHost: undefined
    };

    this.settingsService.updateGlobalSettings({ docker: dockerSettings }).subscribe({
      next: () => {
        this.showSuccessMessage('Docker settings updated successfully');
      },
      error: (error) => {
        this.showErrorMessage('Failed to update Docker settings');
        console.error(error);
      }
    });
  }

  private updateNginxSettings(): void {
    const nginxSettings = {
      enableDynamicSubdomains: this.enableDynamicSubdomains,
      baseDomain: this.baseDomain,
      listenPort: this.nginxPort,
      useGuidSubdomains: this.useGuidSubdomains,
      subdomainMappings: {}
    };

    this.settingsService.updateGlobalSettings({ nginx: nginxSettings }).subscribe({
      next: () => {
        this.showSuccessMessage('Nginx settings updated successfully');
      },
      error: (error) => {
        this.showErrorMessage('Failed to update Nginx settings');
        console.error(error);
      }
    });
  }

  saveSettings(): void {
    this.saving = true;

    const settings = {
      ui: {
        darkMode: this.isDarkMode,
        theme: this.isDarkMode ? 'dark' : 'default',
        enableAnimations: this.enableAnimations,
        refreshInterval: this.refreshInterval
      },
      containers: {
        showAllContainers: this.showAllContainers,
        showSystemContainers: this.showSystemContainers,
        enableVisualization: this.enableVisualization,
        hiddenContainers: []
      },
      docker: {
        defaultHost: this.dockerHost,
        enableMaintenance: this.enableMaintenance,
        autoCleanup: this.enableMaintenance,
        maintenanceInterval: this.maintenanceInterval,
        remoteHosts: [],
        currentRemoteHost: undefined
      },
      nginx: {
        enableDynamicSubdomains: this.enableDynamicSubdomains,
        baseDomain: this.baseDomain,
        listenPort: this.nginxPort,
        useGuidSubdomains: this.useGuidSubdomains,
        subdomainMappings: {}
      }
    };

    this.settingsService.updateGlobalSettings(settings).subscribe({
      next: () => {
        this.saving = false;
        this.showSuccessMessage('All settings saved successfully');
      },
      error: (error) => {
        this.saving = false;
        this.showErrorMessage('Failed to save settings');
        console.error(error);
      }
    });
  }

  resetToDefaults(): void {
    if (confirm('Are you sure you want to reset all settings to defaults? This cannot be undone.')) {
      // Reset to default values
      this.isDarkMode = false;
      this.refreshInterval = 30;
      this.enableAnimations = true;
      this.showAllContainers = false;
      this.showSystemContainers = false;
      this.enableVisualization = true;
      this.dockerHost = 'npipe://./pipe/docker_engine';
      this.enableMaintenance = true;
      this.maintenanceInterval = 3600;
      this.enableDynamicSubdomains = true;
      this.baseDomain = 'starklink.local';
      this.nginxPort = 8080;
      this.useGuidSubdomains = true;

      this.saveSettings();
    }
  }

  private showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  private showErrorMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }
}
