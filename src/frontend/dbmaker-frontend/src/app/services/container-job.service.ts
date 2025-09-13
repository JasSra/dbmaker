import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, interval, switchMap, takeWhile, map, finalize } from 'rxjs';

// ⚠️ WARNING: This is a MOCK SERVICE for development/testing purposes only
// DO NOT USE IN PRODUCTION - Use ContainerService instead for real API calls
// This service simulates container creation jobs with fake progress updates

export interface ContainerCreationJob {
  id: string;
  name: string;
  databaseType: string;
  status: JobStatus;
  progress: number;
  currentStep: string;
  steps: JobStep[];
  startedAt: Date;
  completedAt?: Date;
  error?: string;
  containerId?: string;
}

export interface JobStep {
  name: string;
  description: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  progress: number;
  startedAt?: Date;
  completedAt?: Date;
  error?: string;
}

export type JobStatus = 'queued' | 'running' | 'completed' | 'failed' | 'cancelled';

@Injectable({
  providedIn: 'root'
})
export class ContainerJobService {
  private jobs = new Map<string, ContainerCreationJob>();
  private jobsSubject = new BehaviorSubject<ContainerCreationJob[]>([]);

  public readonly jobs$ = this.jobsSubject.asObservable();

  private readonly defaultSteps: JobStep[] = [
    {
      name: 'validation',
      description: 'Validating container configuration',
      status: 'pending',
      progress: 0
    },
    {
      name: 'docker-connect',
      description: 'Connecting to Docker daemon',
      status: 'pending',
      progress: 0
    },
    {
      name: 'image-pull',
      description: 'Pulling container image',
      status: 'pending',
      progress: 0
    },
    {
      name: 'container-create',
      description: 'Creating container instance',
      status: 'pending',
      progress: 0
    },
    {
      name: 'network-setup',
      description: 'Configuring network and ports',
      status: 'pending',
      progress: 0
    },
    {
      name: 'container-start',
      description: 'Starting container',
      status: 'pending',
      progress: 0
    },
    {
      name: 'health-check',
      description: 'Performing health check',
      status: 'pending',
      progress: 0
    },
    {
      name: 'database-setup',
      description: 'Setting up database configuration',
      status: 'pending',
      progress: 0
    },
    {
      name: 'registration',
      description: 'Registering container in system',
      status: 'pending',
      progress: 0
    }
  ];

  createJob(containerName: string, databaseType: string): string {
    const jobId = this.generateJobId();
    const job: ContainerCreationJob = {
      id: jobId,
      name: containerName,
      databaseType,
      status: 'queued',
      progress: 0,
      currentStep: 'Queued for processing',
      steps: JSON.parse(JSON.stringify(this.defaultSteps)), // Deep copy
      startedAt: new Date()
    };

    this.jobs.set(jobId, job);
    this.updateJobsSubject();

    return jobId;
  }

  getJob(jobId: string): ContainerCreationJob | undefined {
    return this.jobs.get(jobId);
  }

  getJob$(jobId: string): Observable<ContainerCreationJob | undefined> {
    return this.jobs$.pipe(
      map(jobs => jobs.find(j => j.id === jobId))
    );
  }

  getAllJobs(): ContainerCreationJob[] {
    return Array.from(this.jobs.values());
  }

  simulateJobProgress(jobId: string): Observable<ContainerCreationJob> {
    const job = this.jobs.get(jobId);
    if (!job) {
      throw new Error(`Job ${jobId} not found`);
    }

    job.status = 'running';
    this.updateJobsSubject();

    return interval(1500).pipe(
      switchMap(() => this.processNextStep(jobId)),
      takeWhile(job => job.status === 'running'),
      finalize(() => {
        const finalJob = this.jobs.get(jobId);
        if (finalJob && finalJob.status === 'running') {
          this.completeJob(jobId, true);
        }
      })
    );
  }

  private processNextStep(jobId: string): Observable<ContainerCreationJob> {
    return new Observable(observer => {
      const job = this.jobs.get(jobId);
      if (!job || job.status !== 'running') {
        observer.error(`Job ${jobId} is not running`);
        return;
      }

      const currentStepIndex = job.steps.findIndex(s => s.status === 'pending');

      if (currentStepIndex === -1) {
        // All steps completed
        this.completeJob(jobId, true);
        observer.next(job);
        observer.complete();
        return;
      }

      const currentStep = job.steps[currentStepIndex];

      // Start the step
      currentStep.status = 'running';
      currentStep.startedAt = new Date();
      job.currentStep = currentStep.description;

      // Simulate step progress
      let stepProgress = 0;
      const stepInterval = setInterval(() => {
        stepProgress += Math.random() * 30;
        currentStep.progress = Math.min(100, stepProgress);

        // Update overall job progress
        const completedSteps = job.steps.filter(s => s.status === 'completed').length;
        const currentStepProgress = (currentStep.progress / 100) / job.steps.length;
        job.progress = ((completedSteps / job.steps.length) + currentStepProgress) * 100;

        this.updateJobsSubject();

        if (currentStep.progress >= 100) {
          clearInterval(stepInterval);

          // Simulate occasional failures for demo purposes
          const shouldFail = Math.random() < 0.05; // 5% chance of failure

          if (shouldFail && currentStepIndex > 2) { // Don't fail on early steps
            currentStep.status = 'failed';
            currentStep.error = this.getRandomError();
            job.status = 'failed';
            job.error = `Failed at step: ${currentStep.description}`;
            this.updateJobsSubject();
            observer.error(new Error(job.error));
          } else {
            currentStep.status = 'completed';
            currentStep.completedAt = new Date();
            this.updateJobsSubject();
            observer.next(job);
            observer.complete();
          }
        }
      }, 200);

      // Cleanup function
      return () => {
        clearInterval(stepInterval);
      };
    });
  }

  completeJob(jobId: string, success: boolean, containerId?: string, error?: string): void {
    const job = this.jobs.get(jobId);
    if (!job) return;

    job.status = success ? 'completed' : 'failed';
    job.completedAt = new Date();
    job.progress = success ? 100 : job.progress;

    if (success) {
      job.currentStep = 'Container created successfully';
      job.containerId = containerId || this.generateContainerId();
      // Mark all remaining steps as completed
      job.steps.forEach(step => {
        if (step.status === 'pending' || step.status === 'running') {
          step.status = 'completed';
          step.progress = 100;
          step.completedAt = new Date();
        }
      });
    } else {
      job.error = error || 'Container creation failed';
      job.currentStep = job.error;
    }

    this.updateJobsSubject();
  }

  cancelJob(jobId: string): void {
    const job = this.jobs.get(jobId);
    if (!job) return;

    job.status = 'cancelled';
    job.completedAt = new Date();
    job.currentStep = 'Job cancelled by user';

    this.updateJobsSubject();
  }

  removeJob(jobId: string): void {
    this.jobs.delete(jobId);
    this.updateJobsSubject();
  }

  clearCompletedJobs(): void {
    Array.from(this.jobs.entries()).forEach(([id, job]) => {
      if (job.status === 'completed' || job.status === 'failed' || job.status === 'cancelled') {
        this.jobs.delete(id);
      }
    });
    this.updateJobsSubject();
  }

  getActiveJobs(): ContainerCreationJob[] {
    return this.getAllJobs().filter(job =>
      job.status === 'queued' || job.status === 'running'
    );
  }

  getCompletedJobs(): ContainerCreationJob[] {
    return this.getAllJobs().filter(job =>
      job.status === 'completed' || job.status === 'failed' || job.status === 'cancelled'
    );
  }

  private updateJobsSubject(): void {
    this.jobsSubject.next(this.getAllJobs());
  }

  private generateJobId(): string {
    return `job-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private generateContainerId(): string {
    return `container-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private getRandomError(): string {
    const errors = [
      'Network timeout',
      'Docker daemon unavailable',
      'Port already in use',
      'Insufficient disk space',
      'Image pull failed',
      'Configuration validation failed',
      'Resource limits exceeded'
    ];
    return errors[Math.floor(Math.random() * errors.length)];
  }
}
