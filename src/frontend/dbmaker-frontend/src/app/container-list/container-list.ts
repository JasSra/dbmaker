import { Component, OnInit, TrackByFunction } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

// Angular Material
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDividerModule } from '@angular/material/divider';

import { ContainerService } from '../services/container.service';
import { ContainerResponse, ContainerStatus, ContainerMonitoringData } from '../models/container.models';

@Component({
  selector: 'app-container-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatChipsModule,
    MatMenuModule,
    MatTooltipModule,
    MatSnackBarModule,
    MatDividerModule
  ],
  templateUrl: './container-list.html',
  styleUrl: './container-list.scss'
})
export class ContainerListComponent implements OnInit {
  containers: ContainerResponse[] = [];
  filteredContainers: ContainerResponse[] = [];
  loading = true;
  errorMessage = '';
  actionInProgress: string | null = null;

  // Filter properties
  searchTerm = '';
  statusFilter = '';
  typeFilter = '';

  // Optional cache for stats
  private monitoringData: Map<string, ContainerMonitoringData> = new Map();

  constructor(
    private containerService: ContainerService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
  this.loadContainers();
  }

  loadContainers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.containerService.getContainers().subscribe({
      next: (containers) => {
        this.containers = containers;
        this.filteredContainers = [...containers];
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.errorMessage = err?.error?.message || 'Failed to load containers';
      }
    });
  }

  // Optionally load stats lazily per container as needed

  refreshContainers(): void {
    this.loadContainers();
    this.showSnackBar('Containers refreshed successfully');
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  public applyFilters(): void {
    let filtered = [...this.containers];

    // Apply search filter
    if (this.searchTerm.trim()) {
      const searchLower = this.searchTerm.toLowerCase();
      filtered = filtered.filter(container =>
        container.name.toLowerCase().includes(searchLower) ||
        container.databaseType.toLowerCase().includes(searchLower) ||
        container.status.toLowerCase().includes(searchLower) ||
        container.subdomain.toLowerCase().includes(searchLower)
      );
    }

    // Apply status filter
    if (this.statusFilter) {
  filtered = filtered.filter(container => (container.status as unknown as string) === this.statusFilter);
    }

    // Apply type filter
    if (this.typeFilter) {
      filtered = filtered.filter(container => container.databaseType === this.typeFilter);
    }

    this.filteredContainers = filtered;
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.typeFilter = '';
    this.applyFilters();
  }

  startContainer(container: ContainerResponse): void {
    this.actionInProgress = container.id;
    this.containerService.startContainer(container.id).subscribe({
      next: () => {
        const index = this.containers.findIndex(c => c.id === container.id);
        if (index !== -1) {
          this.containers[index].status = ContainerStatus.Running;
          this.applyFilters();
        }
        this.actionInProgress = null;
        this.showSnackBar(`Container ${container.name} started successfully`);
      },
      error: (err) => {
        this.actionInProgress = null;
        this.showSnackBar(err?.error?.message || `Failed to start ${container.name}`);
      }
    });
  }

  stopContainer(container: ContainerResponse): void {
    this.actionInProgress = container.id;
    this.containerService.stopContainer(container.id).subscribe({
      next: () => {
        const index = this.containers.findIndex(c => c.id === container.id);
        if (index !== -1) {
          this.containers[index].status = ContainerStatus.Stopped;
          this.applyFilters();
        }
        this.actionInProgress = null;
        this.showSnackBar(`Container ${container.name} stopped successfully`);
      },
      error: (err) => {
        this.actionInProgress = null;
        this.showSnackBar(err?.error?.message || `Failed to stop ${container.name}`);
      }
    });
  }

  deleteContainer(container: ContainerResponse): void {
    if (confirm(`Are you sure you want to delete container "${container.name}"? This action cannot be undone.`)) {
      this.actionInProgress = container.id;
      this.containerService.deleteContainer(container.id).subscribe({
        next: () => {
          this.containers = this.containers.filter(c => c.id !== container.id);
          this.applyFilters();
          this.actionInProgress = null;
          this.showSnackBar(`Container ${container.name} deleted successfully`);
        },
        error: (err) => {
          this.actionInProgress = null;
          this.showSnackBar(err?.error?.message || `Failed to delete ${container.name}`);
        }
      });
    }
  }

  copyConnectionString(connectionString: string): void {
    navigator.clipboard.writeText(connectionString).then(() => {
      this.showSnackBar('Connection string copied to clipboard');
    }).catch((error) => {
      console.error('Failed to copy connection string:', error);
      this.showSnackBar('Failed to copy connection string');
    });
  }

  openContainer(container: ContainerResponse): void {
    // Open container management interface
    window.open(`http://${container.subdomain}.dbmaker.local:${container.port}`, '_blank');
  }

  viewLogs(container: ContainerResponse): void {
    this.showSnackBar(`Opening logs for ${container.name}...`);
    // Implement log viewing functionality
  }

  editContainer(container: ContainerResponse): void {
    this.showSnackBar(`Editing configuration for ${container.name}...`);
    // Navigate to edit container page
  }

  getContainerIcon(databaseType: string): string {
    const iconMap: { [key: string]: string } = {
      'postgresql': 'storage',
      'redis': 'memory',
      'mysql': 'dns',
      'mongodb': 'folder_special'
    };
    return iconMap[databaseType] || 'database';
  }

  getStatusIcon(status: ContainerStatus): string {
    switch (status) {
      case ContainerStatus.Running:
        return 'play_circle';
      case ContainerStatus.Stopped:
        return 'stop_circle';
      case ContainerStatus.Creating:
        return 'hourglass_empty';
      case ContainerStatus.Failed:
        return 'error';
      default:
        return 'help';
    }
  }

  getMonitoringData(containerId: string): ContainerMonitoringData | undefined {
    return this.monitoringData.get(containerId);
  }

  trackByContainerId: TrackByFunction<ContainerResponse> = (index: number, container: ContainerResponse) => {
    return container.id;
  };

  private showSnackBar(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      horizontalPosition: 'end',
      verticalPosition: 'top'
    });
  }
}
