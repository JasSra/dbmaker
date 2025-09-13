import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatTableModule } from '@angular/material/table';
import { interval, Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { ContainerService } from '../services/container.service';
import { type ContainerResponse } from '../../../api/consolidated';
import { ContainerStatus as ApiContainerStatus } from '../../../api/consolidated/models/ContainerStatus';
import { LoggerService } from '../services/logger.service';

interface SystemStats {
  totalContainers: number;
  runningContainers: number;
  cpuUsage: number;
  memoryUsage: number;
  diskUsage: number;
  networkIn: number;
  networkOut: number;
}

interface ContainerMetrics {
  containerId: string;
  name: string;
  cpuPercent: number;
  memoryUsage: number;
  memoryLimit: number;
  memoryPercent: number;
  networkIn: number;
  networkOut: number;
  blockIn: number;
  blockOut: number;
  uptime: number;
}

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatExpansionModule,
    MatTableModule
  ],
  templateUrl: './analytics.component.html',
  styleUrl: './analytics.component.scss'
})
export class AnalyticsComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();

  containers: ContainerResponse[] = [];
  containerMetrics: ContainerMetrics[] = [];
  systemStats: SystemStats = {
    totalContainers: 0,
    runningContainers: 0,
    cpuUsage: 0,
    memoryUsage: 0,
    diskUsage: 0,
    networkIn: 0,
    networkOut: 0
  };

  selectedContainer: ContainerResponse | null = null;
  containerLogs: string[] = [];
  loadingLogs = false;
  loadingMetrics = false;

  // Chart data for visualization
  cpuChartData: number[] = [];
  memoryChartData: number[] = [];
  networkChartData: { in: number, out: number }[] = [];

  // Table columns for metrics
  displayedColumns: string[] = ['name', 'cpu', 'memory', 'network', 'uptime', 'actions'];

  constructor(private containerService: ContainerService, private logger: LoggerService) {}

  ngOnInit(): void {
    this.loadContainers();
    this.loadSystemStats();

    // Auto-refresh every 5 seconds
    interval(5000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadSystemStats();
        this.loadContainerMetrics();
      });

    // Auto-refresh containers every 30 seconds
    interval(30000)
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadContainers();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadContainers(): void {
    this.containerService.getContainers().subscribe({
      next: (containers) => {
        this.containers = containers;
        this.loadContainerMetrics();
      },
      error: (error) => {
        console.error('Failed to load containers:', error);
      }
    });
  }

  loadSystemStats(): void {
    // Simulate system stats - in production, get from Docker API
    this.systemStats = {
      totalContainers: this.containers.length,
      runningContainers: this.containers.filter(c => (c.status as any) === ApiContainerStatus.RUNNING || (c.status as any) === 'Running').length,
      cpuUsage: Math.random() * 100,
      memoryUsage: Math.random() * 100,
      diskUsage: Math.random() * 100,
      networkIn: Math.random() * 1000,
      networkOut: Math.random() * 1000
    };

    // Update chart data
    this.cpuChartData.push(this.systemStats.cpuUsage);
    this.memoryChartData.push(this.systemStats.memoryUsage);
    this.networkChartData.push({
      in: this.systemStats.networkIn,
      out: this.systemStats.networkOut
    });

    // Keep only last 20 data points
    if (this.cpuChartData.length > 20) {
      this.cpuChartData.shift();
      this.memoryChartData.shift();
      this.networkChartData.shift();
    }
  }

  loadContainerMetrics(): void {
    this.loadingMetrics = true;

    // Simulate container metrics - in production, get from Docker stats API
    this.containerMetrics = this.containers.map(container => ({
      containerId: (container.id ?? '').toString(),
      name: container.name ?? '',
      cpuPercent: Math.random() * 100,
      memoryUsage: Math.random() * 2048 * 1024 * 1024, // Random bytes
      memoryLimit: 2048 * 1024 * 1024, // 2GB limit
      memoryPercent: Math.random() * 100,
      networkIn: Math.random() * 1000,
      networkOut: Math.random() * 1000,
      blockIn: Math.random() * 500,
      blockOut: Math.random() * 500,
  uptime: Date.now() - new Date(container.createdAt ?? Date.now()).getTime()
    }));

    this.loadingMetrics = false;
  }

  selectContainer(container: ContainerResponse): void {
    this.selectedContainer = container;
  if (container.id) this.loadContainerLogs(container.id);
  }

  selectContainerByMetric(metric: ContainerMetrics): void {
    const container = this.containers.find(c => c.id === metric.containerId);
    if (container) {
      this.selectContainer(container);
    }
  }

  loadContainerLogs(containerId: string): void {
    this.loadingLogs = true;

    // Simulate container logs - in production, get from Docker logs API
    setTimeout(() => {
      this.containerLogs = [
        '[2024-01-15 19:40:00] INFO: Container started successfully',
        '[2024-01-15 19:40:01] INFO: Initializing database connection...',
        '[2024-01-15 19:40:02] INFO: Database connection established',
        '[2024-01-15 19:40:03] INFO: Server listening on port 5432',
        '[2024-01-15 19:40:10] INFO: Health check passed',
        '[2024-01-15 19:40:20] INFO: Processing request from client',
        '[2024-01-15 19:40:25] INFO: Query executed successfully',
        '[2024-01-15 19:40:30] INFO: Client disconnected',
        '[2024-01-15 19:40:35] INFO: Periodic maintenance completed'
      ];
      this.loadingLogs = false;
    }, 1000);
  }

  restartContainer(container: ContainerResponse): void {
  const id = container.id ?? '';
  this.containerService.startContainer(id).subscribe({
        next: () => {
          this.logger.debug('Container restarted successfully');
        this.loadContainers();
      },
      error: (error: any) => {
        console.error('Failed to restart container:', error);
      }
    });
  }

  stopContainer(container: ContainerResponse): void {
  const id = container.id ?? '';
  this.containerService.stopContainer(id).subscribe({
        next: () => {
          this.logger.debug('Container stopped successfully');
        this.loadContainers();
      },
      error: (error: any) => {
        console.error('Failed to stop container:', error);
      }
    });
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  formatUptime(milliseconds: number): string {
    const seconds = Math.floor(milliseconds / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);
    const days = Math.floor(hours / 24);

    if (days > 0) return `${days}d ${hours % 24}h`;
    if (hours > 0) return `${hours}h ${minutes % 60}m`;
    if (minutes > 0) return `${minutes}m ${seconds % 60}s`;
    return `${seconds}s`;
  }

  getStatusColor(status?: string): string {
    const s = (status ?? '').toLowerCase();
    switch (s) {
      case 'running': return 'accent';
      case 'stopped': return 'warn';
      case 'paused': return 'primary';
      default: return '';
    }
  }

  getUsageColor(percentage: number): string {
    if (percentage >= 80) return 'warn';
    if (percentage >= 60) return 'accent';
    return 'primary';
  }
}
