import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DbMakerApiClient, SetupService as GeneratedSetupService, MonitoringService } from '../api-client/DbMakerApiClient';
import { SetupStatus, ValidationResult, InitializeSystemRequest, InitializationResult, MonitoringSummary, ContainerTestResult } from '../models/setup.models';

@Injectable({
  providedIn: 'root'
})
export class SetupService {
  private setupClient: GeneratedSetupService;
  private monitoringClient: MonitoringService;

  constructor() {
    const apiClient = new DbMakerApiClient('http://localhost:5021');
    this.setupClient = apiClient.setup;
    this.monitoringClient = apiClient.monitoring;
  }

  // Setup endpoints
  getSetupStatus(): Observable<SetupStatus> {
    return this.setupClient.getSetupStatus();
  }

  validateDocker(): Observable<ValidationResult> {
    return this.setupClient.validateDocker();
  }

  validateMsal(): Observable<ValidationResult> {
    return this.setupClient.validateMsal();
  }

  initializeSystem(request: InitializeSystemRequest): Observable<InitializationResult> {
    return this.setupClient.initializeSystem(request);
  }

  // Monitoring endpoints
  getMonitoringSummary(): Observable<MonitoringSummary> {
    return this.monitoringClient.getMonitoringSummary();
  }

  testContainer(containerId: string): Observable<ContainerTestResult> {
    return this.monitoringClient.testContainer(containerId);
  }
}
