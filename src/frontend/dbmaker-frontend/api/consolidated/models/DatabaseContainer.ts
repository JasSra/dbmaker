/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerStatus } from './ContainerStatus';
import type { User } from './User';
export type DatabaseContainer = {
    id?: string | null;
    userId?: string | null;
    name?: string | null;
    databaseType?: string | null;
    containerName?: string | null;
    containerId?: string | null;
    port?: number;
    connectionString?: string | null;
    status?: ContainerStatus;
    createdAt?: string;
    lastAccessedAt?: string;
    configuration?: Record<string, string> | null;
    subdomain?: string | null;
    user?: User;
};

