import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject, takeUntil } from 'rxjs';
import { MsalService } from '@azure/msal-angular';
import { ContainerService } from '../services/container.service';
import { UserService } from '../services/user.service';
import { ThemeService, ThemeConfig } from '../services/theme.service';
import { type ContainerResponse, type ContainerMonitoringData } from '../../../api/consolidated';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatChipsModule,
    MatSnackBarModule
  ],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  containers: ContainerResponse[] = [];
  userStats: any = {};
  monitoring: ContainerMonitoringData[] = [];
  currentTheme: ThemeConfig;
  isDarkMode = false;

  private destroy$ = new Subject<void>();

  constructor(
    private containerService: ContainerService,
    private userService: UserService,
    private themeService: ThemeService,
    private snackBar: MatSnackBar,
  private msalService: MsalService,
  private cdr: ChangeDetectorRef
  ) {
    this.currentTheme = this.themeService.getCurrentTheme();
    this.isDarkMode = this.currentTheme.isDarkMode;
  }

  ngOnInit() {
    this.loadDashboardData();
    this.setupMonitoring();

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

  private loadDashboardData() {
    // Try to load data from API, with fallback to demo data
    this.containerService.getContainers().subscribe({
      next: (containers) => {
        this.containers = containers.map(c => ({
          ...c,
          status: this.normalizeStatus((c as any).status)
        })) as any;
        this.cdr.detectChanges();
      },
      error: (error) => {
  console.warn('Failed to load containers from API:', error);
      }
    });

  this.userService.getUserStats().subscribe({
      next: (stats) => {
        this.userStats = stats;
    this.cdr.detectChanges();
      },
      error: (error) => {
  console.warn('Failed to load user stats from API:', error);
      }
    });
  }

  private setupMonitoring() {
    // TODO: Implement real-time monitoring via Server-Sent Events or WebSocket
    // For now, initialize empty monitoring array
    this.monitoring = [];
  }

  getContainersByType(type: string): ContainerResponse[] {
    return this.containers.filter(container => container.databaseType === type);
  }

  getRunningContainers(): number {
    return this.containers.filter(container =>
      this.normalizeStatus((container as any).status).toLowerCase() === 'running'
    ).length;
  }

  getRecentContainers(): ContainerResponse[] {
    return this.containers
      .sort((a, b) => new Date(b.createdAt ?? '').getTime() - new Date(a.createdAt ?? '').getTime())
      .slice(0, 5);
  }

  getContainerIcon(databaseType: string | null | undefined): string {
    const dt = (databaseType ?? '').toLowerCase();
    switch (dt) {
      case 'postgresql':
        return 'data_object';
      case 'redis':
        return 'memory';
      case 'mysql':
        return 'database';
      case 'mongodb':
        return 'account_tree';
      default:
        return 'storage';
    }
  }

  getStatusIcon(status: any): string {
    const s = this.normalizeStatus(status).toLowerCase();
    switch (s) {
      case 'running':
        return 'play_circle';
      case 'stopped':
        return 'stop_circle';
      case 'pending':
        return 'schedule';
      case 'error':
        return 'error';
      default:
        return 'help';
    }
  }

  private normalizeStatus(status: any): string {
    if (typeof status === 'string') return status;
    const map: Record<number, string> = { 0: 'Creating', 1: 'Running', 2: 'Stopped', 3: 'Failed', 4: 'Removing' };
    return map[Number(status)] ?? 'Unknown';
  }

  copyConnectionString(connectionString: string | null | undefined): void {
    const text = connectionString ?? '';
    if (navigator.clipboard) {
      navigator.clipboard.writeText(text).then(() => {
        this.snackBar.open('Connection string copied to clipboard', 'Close', {
          duration: 2000
        });
      }).catch(() => {
        this.fallbackCopyTextToClipboard(text);
      });
    } else {
      this.fallbackCopyTextToClipboard(text);
    }
  }

  private fallbackCopyTextToClipboard(text: string): void {
    const textArea = document.createElement('textarea');
    textArea.value = text;
    textArea.style.top = '0';
    textArea.style.left = '0';
    textArea.style.position = 'fixed';
    document.body.appendChild(textArea);
    textArea.focus();
    textArea.select();

    try {
      document.execCommand('copy');
      this.snackBar.open('Connection string copied to clipboard', 'Close', {
        duration: 2000
      });
    } catch (err) {
      this.snackBar.open('Failed to copy connection string', 'Close', {
        duration: 3000
      });
    }

    document.body.removeChild(textArea);
  }

  refreshData(): void {
    this.loadDashboardData();
    this.snackBar.open('Data refreshed', 'Close', { duration: 2000 });
  }

  exportData(): void {
    const exportData = {
      containers: this.containers,
      exportDate: new Date().toISOString(),
      totalContainers: this.containers.length,
      runningContainers: this.getRunningContainers()
    };

    const dataStr = JSON.stringify(exportData, null, 2);
    const dataUri = 'data:application/json;charset=utf-8,'+ encodeURIComponent(dataStr);

    const exportFileDefaultName = `dbmaker-containers-${new Date().toISOString().split('T')[0]}.json`;

    const linkElement = document.createElement('a');
    linkElement.setAttribute('href', dataUri);
    linkElement.setAttribute('download', exportFileDefaultName);
    linkElement.click();

    this.snackBar.open('Container data exported', 'Close', { duration: 2000 });
  }

  getTotalMemoryUsage(): number {
    return this.monitoring.reduce((total, data) => total + (data.memoryUsage || 0), 0);
  }

  openContainer(container: ContainerResponse) {
    const url = `http://${container.subdomain}:${container.port}`;
    window.open(url, '_blank');
  }

  deleteContainer(containerId: string) {
    if (confirm('Are you sure you want to delete this container?')) {
      this.containerService.deleteContainer(containerId).subscribe(() => {
        this.loadDashboardData(); // Refresh the list
        this.snackBar.open('Container deleted successfully', 'Close', { duration: 2000 });
      });
    }
  }

  logout() {
    this.msalService.logoutRedirect({
      postLogoutRedirectUri: window.location.origin
    });
  }
}
