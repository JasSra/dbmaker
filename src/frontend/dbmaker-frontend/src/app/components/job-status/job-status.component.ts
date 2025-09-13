import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { Router } from '@angular/router';
import { Subject, takeUntil, timer } from 'rxjs';
import { ContainerJobService, ContainerCreationJob, JobStep } from '../../services/container-job.service';

@Component({
  selector: 'app-job-status',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatProgressBarModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatExpansionModule,
    MatChipsModule
  ],
  template: `
    <div class="job-status-container">
      <mat-card class="job-card" [class.completed]="job?.status === 'completed'"
                [class.failed]="job?.status === 'failed'"
                [class.running]="job?.status === 'running'">
        <mat-card-header>
          <mat-card-title>
            <mat-icon [class.spin]="job?.status === 'running'">
              {{ getStatusIcon() }}
            </mat-icon>
            Container Creation: {{ job?.name || 'Unknown' }}
          </mat-card-title>
          <mat-card-subtitle>
            {{ job?.currentStep || 'Initializing...' }}
          </mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <!-- Overall Progress -->
          <div class="progress-section">
            <div class="progress-header">
              <span class="progress-label">Overall Progress</span>
              <span class="progress-percentage">{{ Math.floor(job?.progress || 0) }}%</span>
            </div>
            <mat-progress-bar
              mode="determinate"
              [value]="job?.progress || 0"
              [color]="getProgressColor()">
            </mat-progress-bar>
          </div>

          <!-- Status Chip -->
          <div class="status-section">
            <mat-chip-set>
              <mat-chip [class]="'status-' + job?.status">
                <mat-icon>{{ getStatusIcon() }}</mat-icon>
                {{ getStatusText() }}
              </mat-chip>
              <mat-chip *ngIf="job?.databaseType" class="type-chip">
                {{ job?.databaseType }}
              </mat-chip>
            </mat-chip-set>
          </div>

          <!-- Detailed Steps (Expandable) -->
          <mat-expansion-panel class="steps-panel">
            <mat-expansion-panel-header>
              <mat-panel-title>
                <mat-icon>list</mat-icon>
                Detailed Steps
              </mat-panel-title>
              <mat-panel-description>
                {{ getCompletedStepsCount() }} of {{ job?.steps?.length || 0 }} completed
              </mat-panel-description>
            </mat-expansion-panel-header>

            <mat-list class="steps-list">
              <mat-list-item
                *ngFor="let step of job?.steps; trackBy: trackByStep"
                [class]="'step-' + step.status">
                <mat-icon matListItemIcon [class.spin]="step.status === 'running'">
                  {{ getStepIcon(step) }}
                </mat-icon>
                <div matListItemTitle>{{ step.name }}</div>
                <div matListItemLine>{{ step.description }}</div>

                <div matListItemMeta class="step-meta">
                  <div class="step-progress" *ngIf="step.status === 'running' || step.status === 'completed'">
                    <mat-progress-bar
                      mode="determinate"
                      [value]="step.progress"
                      [color]="step.status === 'completed' ? 'primary' : 'accent'">
                    </mat-progress-bar>
                    <span class="step-percentage">{{ Math.floor(step.progress) }}%</span>
                  </div>
                  <div class="step-time" *ngIf="step.completedAt">
                    {{ formatDuration(step.startedAt, step.completedAt) }}
                  </div>
                  <div class="step-error" *ngIf="step.error">
                    <mat-icon color="warn">error</mat-icon>
                    {{ step.error }}
                  </div>
                </div>
              </mat-list-item>
            </mat-list>
          </mat-expansion-panel>

          <!-- Error Message -->
          <div class="error-section" *ngIf="job?.status === 'failed' && job?.error">
            <mat-icon color="warn">error</mat-icon>
            <span>{{ job?.error || 'Unknown error occurred' }}</span>
          </div>

          <!-- Timing Information -->
          <div class="timing-section" *ngIf="job">
            <div class="time-item">
              <mat-icon>schedule</mat-icon>
              <span>Started: {{ formatTime(job.startedAt) }}</span>
            </div>
            <div class="time-item" *ngIf="job.completedAt">
              <mat-icon>done</mat-icon>
              <span>Completed: {{ formatTime(job.completedAt) }}</span>
            </div>
            <div class="time-item" *ngIf="job.completedAt">
              <mat-icon>timer</mat-icon>
              <span>Duration: {{ formatDuration(job.startedAt, job.completedAt) }}</span>
            </div>
          </div>
        </mat-card-content>

        <mat-card-actions *ngIf="showActions">
          <button
            mat-raised-button
            color="primary"
            *ngIf="job?.status === 'completed' && job?.containerId"
            (click)="viewContainer()">
            <mat-icon>visibility</mat-icon>
            View Container
          </button>

          <button
            mat-button
            *ngIf="job?.status === 'running'"
            (click)="cancelJob()">
            <mat-icon>cancel</mat-icon>
            Cancel
          </button>

          <button
            mat-button
            *ngIf="job?.status === 'failed'"
            (click)="retryJob()">
            <mat-icon>refresh</mat-icon>
            Retry
          </button>

          <button
            mat-button
            *ngIf="job?.status === 'completed' || job?.status === 'failed' || job?.status === 'cancelled'"
            (click)="dismissJob()">
            <mat-icon>close</mat-icon>
            Dismiss
          </button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styleUrl: './job-status.component.scss'
})
export class JobStatusComponent implements OnInit, OnDestroy {
  @Input() jobId!: string;
  @Input() showActions = true;
  @Input() autoRefresh = true;

  job?: ContainerCreationJob;
  Math = Math;

  private destroy$ = new Subject<void>();

  constructor(
    private jobService: ContainerJobService,
    private router: Router
  ) {}

  ngOnInit(): void {
    if (!this.jobId) {
      console.error('JobStatusComponent: jobId is required');
      return;
    }

    // Subscribe to job updates
    this.jobService.getJob$(this.jobId)
      .pipe(takeUntil(this.destroy$))
      .subscribe(job => {
        this.job = job;
      });

    // Auto-refresh if enabled
    if (this.autoRefresh) {
      timer(0, 1000)
        .pipe(takeUntil(this.destroy$))
        .subscribe(() => {
          // Trigger update
        });
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  getStatusIcon(): string {
    switch (this.job?.status) {
      case 'queued': return 'schedule';
      case 'running': return 'refresh';
      case 'completed': return 'check_circle';
      case 'failed': return 'error';
      case 'cancelled': return 'cancel';
      default: return 'help';
    }
  }

  getStatusText(): string {
    switch (this.job?.status) {
      case 'queued': return 'Queued';
      case 'running': return 'Running';
      case 'completed': return 'Completed';
      case 'failed': return 'Failed';
      case 'cancelled': return 'Cancelled';
      default: return 'Unknown';
    }
  }

  getProgressColor(): string {
    switch (this.job?.status) {
      case 'completed': return 'primary';
      case 'failed': return 'warn';
      case 'running': return 'accent';
      default: return 'primary';
    }
  }

  getStepIcon(step: JobStep): string {
    switch (step.status) {
      case 'pending': return 'radio_button_unchecked';
      case 'running': return 'refresh';
      case 'completed': return 'check_circle';
      case 'failed': return 'error';
      default: return 'help';
    }
  }

  getCompletedStepsCount(): number {
    return this.job?.steps?.filter(s => s.status === 'completed').length || 0;
  }

  trackByStep(index: number, step: JobStep): string {
    return step.name;
  }

  formatTime(date: Date | undefined): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleTimeString();
  }

  formatDuration(start: Date | undefined, end: Date | undefined): string {
    if (!start || !end) return 'N/A';

    const duration = new Date(end).getTime() - new Date(start).getTime();
    const seconds = Math.floor(duration / 1000);
    const minutes = Math.floor(seconds / 60);

    if (minutes > 0) {
      return `${minutes}m ${seconds % 60}s`;
    }
    return `${seconds}s`;
  }

  viewContainer(): void {
    if (this.job?.containerId) {
      this.router.navigate(['/containers', this.job.containerId]);
    }
  }

  cancelJob(): void {
    if (this.job) {
      this.jobService.cancelJob(this.job.id);
    }
  }

  retryJob(): void {
    if (this.job) {
      // Create a new job with the same parameters
      const newJobId = this.jobService.createJob(this.job.name, this.job.databaseType);
      this.jobService.simulateJobProgress(newJobId).subscribe({
        error: (error) => console.error('Job retry failed:', error)
      });

      // Navigate to the new job
      this.jobId = newJobId;
    }
  }

  dismissJob(): void {
    if (this.job) {
      this.jobService.removeJob(this.job.id);
    }
  }
}
