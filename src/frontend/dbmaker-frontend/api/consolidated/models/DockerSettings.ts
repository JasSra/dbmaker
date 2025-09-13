/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { RemoteDockerHost } from './RemoteDockerHost';
export type DockerSettings = {
    defaultHost?: string | null;
    enableMaintenance?: boolean;
    autoCleanup?: boolean;
    maintenanceInterval?: number;
    remoteHosts?: Array<RemoteDockerHost> | null;
    currentRemoteHost?: string | null;
};

