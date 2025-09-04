// Auto-generated API client for DbMaker API
// Generated on: $(date)

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SetupStatus {
  databaseConfigured: boolean;
  adminUserExists: boolean;
  dockerConnected: boolean;
  msalConfigured: boolean;
  systemReady: boolean;
}

export interface ValidationResult {
  isValid: boolean;
  message: string;
  details: string;
}

export interface InitializeSystemRequest {
  adminEmail: string;
  adminName: string;
  domain?: string;
  additionalConfig?: { [key: string]: string };
}

export interface InitializationResult {
  adminUserCreated: boolean;
  backupKey: string;
  success: boolean;
  message: string;
}

export interface ContainerResponse {
  id: string;
  name: string;
  databaseType: string;
  connectionString: string;
  status: string;
  subdomain: string;
  port: number;
  createdAt: string;
  configuration: { [key: string]: string };
}

export interface CreateContainerRequest {
  databaseType: string;
  name: string;
  configuration: { [key: string]: string };
}

export interface ContainerMonitoringData {
  containerId: string;
  userId: string;
  status: string;
  cpuUsage: number;
  memoryUsage: number;
  memoryLimit: number;
  networkIO: { [key: string]: number };
  timestamp: string;
  isHealthy: boolean;
  errorMessage?: string;
}

export interface MonitoringSummary {
  totalContainers: number;
  runningContainers: number;
  stoppedContainers: number;
  failedContainers: number;
  totalMemoryUsage: number;
  averageCpuUsage: number;
  unhealthyContainers: number;
  lastUpdated: string;
}

export interface ContainerTestResult {
  containerId: string;
  isReachable: boolean;
  responseTime: number;
  message: string;
  testedAt: string;
}

export interface DatabaseTemplate {
  type: string;
  displayName: string;
  description: string;
  icon: string;
  category: string;
  defaultConfiguration: any;
  configurationOptions?: any[];
}

@Injectable({
  providedIn: 'root'
})
export class DbMakerApiClient {
  private baseUrl = 'http://localhost:5021/api';

  constructor(private http: HttpClient) {}

  private getHeaders(): HttpHeaders {
    return new HttpHeaders({
      'Content-Type': 'application/json',
      'Accept': 'application/json'
    });
  }

  // Setup endpoints
  getSetupStatus(): Observable<SetupStatus> {
    return this.http.get<SetupStatus>(`${this.baseUrl}/setup/status`, { headers: this.getHeaders() });
  }

  validateDocker(): Observable<ValidationResult> {
    return this.http.get<ValidationResult>(`${this.baseUrl}/setup/validate/docker`, { headers: this.getHeaders() });
  }

  validateMsal(): Observable<ValidationResult> {
    return this.http.get<ValidationResult>(`${this.baseUrl}/setup/validate/msal`, { headers: this.getHeaders() });
  }

  initializeSystem(request: InitializeSystemRequest): Observable<InitializationResult> {
    return this.http.post<InitializationResult>(`${this.baseUrl}/setup/initialize`, request, { headers: this.getHeaders() });
  }

  // Container endpoints
  getContainers(): Observable<ContainerResponse[]> {
    return this.http.get<ContainerResponse[]>(`${this.baseUrl}/containers`, { headers: this.getHeaders() });
  }

  getContainer(id: string): Observable<ContainerResponse> {
    return this.http.get<ContainerResponse>(`${this.baseUrl}/containers/${id}`, { headers: this.getHeaders() });
  }

  createContainer(request: CreateContainerRequest): Observable<ContainerResponse> {
    return this.http.post<ContainerResponse>(`${this.baseUrl}/containers`, request, { headers: this.getHeaders() });
  }

  startContainer(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/containers/${id}/start`, {}, { headers: this.getHeaders() });
  }

  stopContainer(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/containers/${id}/stop`, {}, { headers: this.getHeaders() });
  }

  deleteContainer(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/containers/${id}`, { headers: this.getHeaders() });
  }

  getContainerStats(id: string): Observable<ContainerMonitoringData> {
    return this.http.get<ContainerMonitoringData>(`${this.baseUrl}/containers/${id}/stats`, { headers: this.getHeaders() });
  }

  // Templates endpoints
  getTemplates(): Observable<DatabaseTemplate[]> {
    return this.http.get<DatabaseTemplate[]>(`${this.baseUrl}/templates`, { headers: this.getHeaders() });
  }

  getTemplate(type: string): Observable<DatabaseTemplate> {
    return this.http.get<DatabaseTemplate>(`${this.baseUrl}/templates/${type}`, { headers: this.getHeaders() });
  }

  // Monitoring endpoints
  getMonitoringStats(): Observable<ContainerMonitoringData[]> {
    return this.http.get<ContainerMonitoringData[]>(`${this.baseUrl}/monitoring/stats`, { headers: this.getHeaders() });
  }

  getContainerMonitoringStats(id: string): Observable<ContainerMonitoringData> {
    return this.http.get<ContainerMonitoringData>(`${this.baseUrl}/monitoring/stats/${id}`, { headers: this.getHeaders() });
  }

  getMonitoringSummary(): Observable<MonitoringSummary> {
    return this.http.get<MonitoringSummary>(`${this.baseUrl}/monitoring/summary`, { headers: this.getHeaders() });
  }

  testContainer(id: string): Observable<ContainerTestResult> {
    return this.http.post<ContainerTestResult>(`${this.baseUrl}/monitoring/test/${id}`, {}, { headers: this.getHeaders() });
  }

  // Health endpoint
  getHealth(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/health`, { headers: this.getHeaders() });
  }
}
