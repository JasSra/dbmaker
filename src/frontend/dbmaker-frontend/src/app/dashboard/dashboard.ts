import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ContainerService } from '../services/container.service';
import { UserService } from '../services/user.service';
import { ContainerResponse, ContainerMonitoringData, ContainerStatus } from '../models/container.models';

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
export class DashboardComponent implements OnInit {
  containers: ContainerResponse[] = [];
  userStats: any = {};
  monitoring: ContainerMonitoringData[] = [];
  demoUser = {
    name: 'Demo User',
    email: 'demo@dbmaker.local'
  };

  constructor(
    private containerService: ContainerService,
    private userService: UserService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit() {
    this.loadDashboardData();
    this.setupMonitoring();
  }

  private loadDashboardData() {
    // Try to load data from API, with fallback to demo data
    this.containerService.getContainers().subscribe({
      next: (containers) => {
        this.containers = containers;
      },
      error: (error) => {
  console.warn('Failed to load containers from API:', error);
      }
    });

    this.userService.getUserStats().subscribe({
      next: (stats) => {
        this.userStats = stats;
      },
      error: (error) => {
  console.warn('Failed to load user stats from API:', error);
      }
    });
  }

  private setupMonitoring() {
    // For demo: Skip monitoring setup to avoid EventSource errors
    this.monitoring = [];
  }

  getContainersByType(type: string): ContainerResponse[] {
    return this.containers.filter(container => container.databaseType === type);
  }

  getRunningContainers(): number {
    return this.containers.filter(container =>
      container.status?.toLowerCase() === 'running'
    ).length;
  }

  getRecentContainers(): ContainerResponse[] {
    return this.containers
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 5);
  }

  getContainerIcon(databaseType: string): string {
    switch (databaseType.toLowerCase()) {
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

  getStatusIcon(status: string): string {
    switch (status?.toLowerCase()) {
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

  copyConnectionString(connectionString: string): void {
    if (navigator.clipboard) {
      navigator.clipboard.writeText(connectionString).then(() => {
        this.snackBar.open('Connection string copied to clipboard', 'Close', {
          duration: 2000
        });
      }).catch(() => {
        this.fallbackCopyTextToClipboard(connectionString);
      });
    } else {
      this.fallbackCopyTextToClipboard(connectionString);
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
    // Demo logout - just show alert for now
    this.snackBar.open('Demo mode - logout functionality disabled', 'Close', { duration: 3000 });
  }
}
