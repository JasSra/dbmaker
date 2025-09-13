export interface SystemSettings {
  id: string;
  userId: string;
  docker: DockerSettings;
  ui: UISettings;
  nginx: NginxSettings;
  containers: ContainerSettings;
  createdAt: Date;
  updatedAt: Date;
}

export interface DockerSettings {
  defaultHost: string;
  enableMaintenance: boolean;
  autoCleanup: boolean;
  maintenanceInterval: number;
  remoteHosts: RemoteDockerHost[];
  currentRemoteHost?: string;
}

export interface UISettings {
  darkMode: boolean;
  theme: string;
  enableAnimations: boolean;
  refreshInterval: number;
}

export interface NginxSettings {
  enableDynamicSubdomains: boolean;
  baseDomain: string;
  listenPort: number;
  useGuidSubdomains: boolean;
  subdomainMappings: { [key: string]: string };
}

export interface ContainerSettings {
  showAllContainers: boolean;
  showSystemContainers: boolean;
  enableVisualization: boolean;
  hiddenContainers: string[];
}

export interface RemoteDockerHost {
  id: string;
  name: string;
  host: string;
  useTLS: boolean;
  certPath?: string;
  keyPath?: string;
  isActive: boolean;
  lastConnected: Date;
  lastError?: string;
}

export interface UpdateSettingsRequest {
  docker?: DockerSettings;
  ui?: UISettings;
  nginx?: NginxSettings;
  containers?: ContainerSettings;
}

export interface SettingsResponse {
  settings: SystemSettings;
  success: boolean;
  message?: string;
}
