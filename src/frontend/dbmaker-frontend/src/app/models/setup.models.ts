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
  additionalConfig?: Record<string, string>;
}

export interface InitializationResult {
  adminUserCreated: boolean;
  backupKey: string;
  success: boolean;
  message: string;
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
