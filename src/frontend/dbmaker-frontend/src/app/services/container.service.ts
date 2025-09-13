import { Injectable } from '@angular/core';
import { Observable, from, throwError, interval } from 'rxjs';
import { catchError, tap, switchMap, shareReplay, startWith } from 'rxjs/operators';
import {
  ContainersService,
  MonitoringService,
  TemplatesService,
  type ContainerResponse,
  type CreateContainerRequest,
  type ContainerMonitoringData,
  type DatabaseContainer as DatabaseTemplate
} from '../../../api/consolidated';
import { environment } from '../../environments/environment';
import { LoggerService } from './logger.service';

@Injectable({
  providedIn: 'root'
})
export class ContainerService {
  constructor(private logger: LoggerService) {}
  getContainers(): Observable<ContainerResponse[]> {
    return from(ContainersService.getApiContainers()).pipe(
      tap({
        next: () => this.logger.debug('[API] Containers fetched'),
        error: (error) => this.logger.error('[API] Failed to fetch containers', error)
      }),
      catchError((err) => {
        this.logger.error('[API] Containers fetch error', err);
        return throwError(() => err);
      })
    );
  }

  getContainer(id: string): Observable<ContainerResponse> {
    return from(ContainersService.getApiContainers1(id));
  }

  createContainer(request: CreateContainerRequest): Observable<ContainerResponse> {
  this.logger.debug('[API] Creating container');
    return from(ContainersService.postApiContainers(request)).pipe(
      tap({
    next: () => this.logger.debug('[API] Container created successfully'),
    error: (error: any) => this.logger.error('[API] Container creation failed', error)
      })
    );
  }

  startContainer(id: string): Observable<void> {
    return from(ContainersService.postApiContainersStart(id)) as unknown as Observable<void>;
  }

  stopContainer(id: string): Observable<void> {
    return from(ContainersService.postApiContainersStop(id)) as unknown as Observable<void>;
  }

  deleteContainer(id: string): Observable<void> {
    return from(ContainersService.deleteApiContainers(id)) as unknown as Observable<void>;
  }

  getContainerStats(id: string): Observable<ContainerMonitoringData> {
    return from(MonitoringService.getApiMonitoringStats1(id));
  }

  getTemplates(): Observable<DatabaseTemplate[]> {
    // Database templates are exposed via generated TemplatesService
    return from(TemplatesService.getApiTemplates()) as unknown as Observable<DatabaseTemplate[]>;
  }

  getTemplate(type: string): Observable<DatabaseTemplate> {
    return from(TemplatesService.getApiTemplates1(type)) as unknown as Observable<DatabaseTemplate>;
  }

  getMonitoringSummary(): Observable<any> {
    return from(MonitoringService.getApiMonitoringSummary());
  }

  // Polling-based monitoring stream using generated API (avoids manual /api calls)
  // Emits MonitoringSummary at a fixed interval.
  getMonitoringStream$(pollMs: number = 5000): Observable<any> {
    return interval(pollMs).pipe(
      startWith(0),
      switchMap(() => from(MonitoringService.getApiMonitoringSummary())),
      // cache latest for late subscribers within a short window
      shareReplay({ bufferSize: 1, refCount: true })
    );
  }
}
