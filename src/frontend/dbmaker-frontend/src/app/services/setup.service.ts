import { Injectable } from '@angular/core';
import { Observable, from } from 'rxjs';
import type { SetupStatus, ValidationResult, InitializeSystemRequest, InitializationResult, MonitoringSummary, ContainerTestResult } from '../../../api/consolidated';
import { SetupService as ApiSetupService, MonitoringService as ApiMonitoringService } from '../../../api/consolidated';

@Injectable({
  providedIn: 'root'
})
export class SetupService {
  // Setup endpoints
  getSetupStatus(): Observable<SetupStatus> {
  return from(ApiSetupService.getApiSetupStatus());
  }

  validateDocker(): Observable<ValidationResult> {
  return from(ApiSetupService.getApiSetupValidateDocker());
  }

  validateMsal(): Observable<ValidationResult> {
  return from(ApiSetupService.getApiSetupValidateMsal());
  }

  initializeSystem(request: InitializeSystemRequest): Observable<InitializationResult> {
  return from(ApiSetupService.postApiSetupInitialize(request));
  }

  // Monitoring endpoints
  getMonitoringSummary(): Observable<MonitoringSummary> {
  return from(ApiMonitoringService.getApiMonitoringSummary());
  }

  testContainer(containerId: string): Observable<ContainerTestResult> {
  return from(ApiMonitoringService.postApiMonitoringTest(containerId));
  }
}
