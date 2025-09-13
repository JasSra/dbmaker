// Auto-generated API client for DbMaker API
// Generated on: $(date)

import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

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
  private baseUrl = environment.apiUrl;

  constructor(private http: HttpClient) {}

  // Setup
  setup = {
    getStatus: () => this.http.get<SetupStatus>(`${this.baseUrl}/setup/status`),
    validateDocker: () => this.http.get<ValidationResult>(`${this.baseUrl}/setup/validate/docker`),
    validateMsal: () => this.http.get<ValidationResult>(`${this.baseUrl}/setup/validate/msal`),
    initialize: (request: InitializeSystemRequest) =>
      this.http.post<InitializationResult>(`${this.baseUrl}/setup/initialize`, request)
  };

  // Containers
  containers = {
    getAll: () => this.http.get<ContainerResponse[]>(`${this.baseUrl}/containers`),
    get: (id: string) => this.http.get<ContainerResponse>(`${this.baseUrl}/containers/${id}`),
    create: (request: CreateContainerRequest) =>
      this.http.post<ContainerResponse>(`${this.baseUrl}/containers`, request),
    start: (id: string) => this.http.post<void>(`${this.baseUrl}/containers/${id}/start`, {}),
    stop: (id: string) => this.http.post<void>(`${this.baseUrl}/containers/${id}/stop`, {}),
    delete: (id: string) => this.http.delete<void>(`${this.baseUrl}/containers/${id}`),
    getStats: (id: string) => this.http.get<ContainerMonitoringData>(`${this.baseUrl}/containers/${id}/stats`)
  };

  // Templates
  templates = {
    getAll: () => this.http.get<DatabaseTemplate[]>(`${this.baseUrl}/templates`),
    get: (type: string) => this.http.get<DatabaseTemplate>(`${this.baseUrl}/templates/${type}`)
  };

  // Monitoring
  monitoring = {
    getStats: () => this.http.get<ContainerMonitoringData[]>(`${this.baseUrl}/monitoring/stats`),
    getContainerStats: (id: string) =>
      this.http.get<ContainerMonitoringData>(`${this.baseUrl}/monitoring/stats/${id}`),
    getSummary: () => this.http.get<MonitoringSummary>(`${this.baseUrl}/monitoring/summary`),
    testContainer: (id: string) =>
      this.http.post<ContainerTestResult>(`${this.baseUrl}/monitoring/test/${id}`, {})
  };

  // Users
  users = {
  getCurrent: () => this.http.get<any>(`${this.baseUrl}/users/me`),
  getStats: () => this.http.get<any>(`${this.baseUrl}/users/me/stats`),
    getAll: () => this.http.get<any[]>(`${this.baseUrl}/users`),
    create: (user: any) => this.http.post<any>(`${this.baseUrl}/users`, user),
    update: (id: string, user: any) => this.http.put<any>(`${this.baseUrl}/users/${id}`, user),
    delete: (id: string) => this.http.delete<void>(`${this.baseUrl}/users/${id}`)
  };

  // Health
  health = {
    check: () => this.http.get<any>(`${this.baseUrl}/health`)
  };
}
