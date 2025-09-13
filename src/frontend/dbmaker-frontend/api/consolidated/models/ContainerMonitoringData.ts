/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerStatus } from './ContainerStatus';
export type ContainerMonitoringData = {
    containerId?: string | null;
    userId?: string | null;
    status?: ContainerStatus;
    cpuUsage?: number;
    memoryUsage?: number;
    memoryLimit?: number;
    networkIO?: Record<string, number> | null;
    timestamp?: string;
    isHealthy?: boolean;
    errorMessage?: string | null;
};

