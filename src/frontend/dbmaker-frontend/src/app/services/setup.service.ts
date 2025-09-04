import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SetupStatus, ValidationResult, InitializeSystemRequest, InitializationResult, MonitoringSummary, ContainerTestResult } from '../models/setup.models';

@Injectable({
  providedIn: 'root'
})
export class SetupService {
  private readonly baseUrl = 'http://localhost:5021/api';

  constructor(private http: HttpClient) {}

  // Setup endpoints
  getSetupStatus(): Observable<SetupStatus> {
    return this.http.get<SetupStatus>(`${this.baseUrl}/setup/status`);
  }

  validateDocker(): Observable<ValidationResult> {
    return this.http.get<ValidationResult>(`${this.baseUrl}/setup/validate/docker`);
  }

  validateMsal(): Observable<ValidationResult> {
    return this.http.get<ValidationResult>(`${this.baseUrl}/setup/validate/msal`);
  }

  initializeSystem(request: InitializeSystemRequest): Observable<InitializationResult> {
    return this.http.post<InitializationResult>(`${this.baseUrl}/setup/initialize`, request);
  }

  // Monitoring endpoints
  getMonitoringSummary(): Observable<MonitoringSummary> {
    return this.http.get<MonitoringSummary>(`${this.baseUrl}/monitoring/summary`);
  }

  testContainer(containerId: string): Observable<ContainerTestResult> {
    return this.http.post<ContainerTestResult>(`${this.baseUrl}/monitoring/test/${containerId}`, {});
  }
}
