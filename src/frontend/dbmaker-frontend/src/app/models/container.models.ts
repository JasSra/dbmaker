export interface User {
  id: string;
  email: string;
  name: string;
  createdAt: string;
  lastLoginAt: string;
  isActive: boolean;
  containers: DatabaseContainer[];
}

export interface DatabaseContainer {
  id: string;
  userId: string;
  name: string;
  databaseType: string;
  containerName: string;
  containerId: string;
  port: number;
  connectionString: string;
  status: ContainerStatus;
  createdAt: string;
  lastAccessedAt: string;
  configuration: Record<string, string>;
  subdomain: string;
}

export enum ContainerStatus {
  Creating = 'Creating',
  Running = 'Running',
  Stopped = 'Stopped',
  Failed = 'Failed',
  Removing = 'Removing'
}

export interface ContainerResponse {
  id: string;
  name: string;
  databaseType: string;
  connectionString: string;
  status: ContainerStatus;
  subdomain: string;
  port: number;
  createdAt: string;
  configuration: Record<string, string>;
}

export interface CreateContainerRequest {
  databaseType: string;
  name: string;
  configuration: Record<string, string>;
}

export interface ContainerMonitoringData {
  containerId: string;
  userId: string;
  status: ContainerStatus;
  cpuUsage: number;
  memoryUsage: number;
  memoryLimit: number;
  networkIO: Record<string, number>;
  timestamp: string;
  isHealthy: boolean;
  errorMessage?: string;
}

export interface DatabaseTemplate {
  type: string;
  displayName: string;
  description: string;
  icon: string;
  category: string;
  defaultConfiguration: Record<string, any>;
  configurationOptions?: ConfigurationOption[];
}

export interface ConfigurationOption {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'select';
  default: any;
  description: string;
  options?: string[];
}
