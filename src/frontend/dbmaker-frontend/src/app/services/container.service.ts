import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ContainerResponse, CreateContainerRequest, ContainerMonitoringData, DatabaseTemplate } from '../models/container.models';
import { DbMakerApiClient, ContainersService, TemplatesService, MonitoringService } from '../api-client/DbMakerApiClient';

@Injectable({
  providedIn: 'root'
})
export class ContainerService {
  private containersClient: ContainersService;
  private templatesClient: TemplatesService;
  private monitoringClient: MonitoringService;

  constructor() {
    const apiClient = new DbMakerApiClient('http://localhost:5021');
    this.containersClient = apiClient.containers;
    this.templatesClient = apiClient.templates;
    this.monitoringClient = apiClient.monitoring;
  }

  getContainers(): Observable<ContainerResponse[]> {
    return this.containersClient.getContainers();
  }

  getContainer(id: string): Observable<ContainerResponse> {
    return this.containersClient.getContainer(id);
  }

  createContainer(request: CreateContainerRequest): Observable<ContainerResponse> {
    return this.containersClient.createContainer(request);
  }

  startContainer(id: string): Observable<void> {
    return this.containersClient.startContainer(id);
  }

  stopContainer(id: string): Observable<void> {
    return this.containersClient.stopContainer(id);
  }

  deleteContainer(id: string): Observable<void> {
    return this.containersClient.deleteContainer(id);
  }

  getContainerStats(id: string): Observable<ContainerMonitoringData> {
    return this.monitoringClient.getContainerStats(id);
  }

  getTemplates(): Observable<DatabaseTemplate[]> {
    return this.templatesClient.getTemplates();
  }

  getTemplate(type: string): Observable<DatabaseTemplate> {
    return this.templatesClient.getTemplate(type);
  }

  getMonitoringSummary(): Observable<any> {
    return this.monitoringClient.getMonitoringSummary();
  }

  // Server-Sent Events for real-time monitoring (requires auth)
  getMonitoringStream(): EventSource {
    return new EventSource('http://localhost:5021/api/monitoring/events');
  }
}
