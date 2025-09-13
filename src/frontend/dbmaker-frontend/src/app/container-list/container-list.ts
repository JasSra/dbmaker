import { Component, OnInit, TrackByFunction, ChangeDetectorRef } from '@angular/core';
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
import { type ContainerResponse, type ContainerMonitoringData } from '../../../api/consolidated';
import { finalize } from 'rxjs/operators';

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
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
  this.loadContainers();
  }

  loadContainers(): void {
    this.loading = true;
    this.errorMessage = '';
    this.containerService.getContainers()
      .pipe(finalize(() => { this.loading = false; this.cdr.detectChanges(); }))
      .subscribe({
      next: (containers) => {
        // Normalize status to string labels if backend returned numbers
        this.containers = containers.map((c) => ({
          ...c,
          status: this.normalizeStatus(c.status as unknown as any)
        })) as unknown as ContainerResponse[];
        this.filteredContainers = [...this.containers];
    // Ensure UI updates in zoneless mode
    this.cdr.detectChanges();
      },
      error: (err) => {
        this.errorMessage = err?.error?.message || 'Failed to load containers';
        this.showSnackBar(this.errorMessage);
    this.cdr.detectChanges();
      }
    });
  }

  private normalizeStatus(status: any): string {
    if (typeof status === 'string') return status;
    // Enum mapping from backend: 0 Creating, 1 Running, 2 Stopped, 3 Failed, 4 Removing
    const map: Record<number, string> = {
      0: 'Creating',
      1: 'Running',
      2: 'Stopped',
      3: 'Failed',
      4: 'Removing'
    };
    return map[Number(status)] ?? 'Unknown';
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
        (container.name ?? '').toLowerCase().includes(searchLower) ||
        (container.databaseType ?? '').toLowerCase().includes(searchLower) ||
        (String(container.status ?? '')).toLowerCase().includes(searchLower) ||
        (container.subdomain ?? '').toLowerCase().includes(searchLower)
      );
    }

    // Apply status filter
    if (this.statusFilter) {
      filtered = filtered.filter(container => this.normalizeStatus(container.status as any) === this.statusFilter);
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
  const id = container.id ?? '';
  this.actionInProgress = id;
  this.containerService.startContainer(id).subscribe({
      next: () => {
        const index = this.containers.findIndex(c => c.id === container.id);
        if (index !== -1) {
      this.containers[index].status = 'Running' as any;
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
  const id = container.id ?? '';
  this.actionInProgress = id;
  this.containerService.stopContainer(id).subscribe({
      next: () => {
        const index = this.containers.findIndex(c => c.id === container.id);
        if (index !== -1) {
      this.containers[index].status = 'Stopped' as any;
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
  const id = container.id ?? '';
  this.actionInProgress = id;
  this.containerService.deleteContainer(id).subscribe({
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

  copyConnectionString(connectionString: string | null | undefined): void {
    const text = connectionString ?? '';
    navigator.clipboard.writeText(text).then(() => {
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

  getContainerIcon(databaseType: string | null | undefined): string {
    const type = (databaseType ?? '').toLowerCase();
    const iconMap: { [key: string]: string } = {
      'postgresql': 'storage',
      'redis': 'memory',
      'mysql': 'dns',
      'mongodb': 'folder_special'
    };
    return iconMap[type] || 'database';
  }

  getStatusIcon(status: any): string {
    const statusStr = this.normalizeStatus(status);
    switch (statusStr) {
      case 'Running':
        return 'play_circle';
      case 'Stopped':
        return 'stop_circle';
      case 'Creating':
        return 'hourglass_empty';
      case 'Failed':
        return 'error';
      default:
        return 'help';
    }
  }

  getMonitoringData(containerId: string | null | undefined): ContainerMonitoringData | undefined {
    if (!containerId) return undefined;
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
