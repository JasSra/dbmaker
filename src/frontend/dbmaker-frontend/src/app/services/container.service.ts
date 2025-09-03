import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ContainerResponse, CreateContainerRequest, ContainerMonitoringData, DatabaseTemplate } from '../models/container.models';
import { protectedResources } from '../auth-config';

@Injectable({
  providedIn: 'root'
})
export class ContainerService {
  private readonly baseUrl = protectedResources.apiEndpoint;

  constructor(private http: HttpClient) {}

  getContainers(): Observable<ContainerResponse[]> {
    return this.http.get<ContainerResponse[]>(`${this.baseUrl}/containers`);
  }

  getContainer(id: string): Observable<ContainerResponse> {
    return this.http.get<ContainerResponse>(`${this.baseUrl}/containers/${id}`);
  }

  createContainer(request: CreateContainerRequest): Observable<ContainerResponse> {
    return this.http.post<ContainerResponse>(`${this.baseUrl}/containers`, request);
  }

  startContainer(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/containers/${id}/start`, {});
  }

  stopContainer(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/containers/${id}/stop`, {});
  }

  deleteContainer(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/containers/${id}`);
  }

  getContainerStats(id: string): Observable<ContainerMonitoringData> {
    return this.http.get<ContainerMonitoringData>(`${this.baseUrl}/containers/${id}/stats`);
  }

  getTemplates(): Observable<DatabaseTemplate[]> {
    return this.http.get<DatabaseTemplate[]>(`${this.baseUrl}/templates`);
  }

  getTemplate(type: string): Observable<DatabaseTemplate> {
    return this.http.get<DatabaseTemplate>(`${this.baseUrl}/templates/${type}`);
  }

  // Server-Sent Events for real-time monitoring
  getMonitoringStream(): EventSource {
    return new EventSource(`${this.baseUrl}/monitoring/events`);
  }
}
