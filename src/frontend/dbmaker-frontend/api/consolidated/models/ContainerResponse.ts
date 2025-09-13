/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerStatus } from './ContainerStatus';
export type ContainerResponse = {
    id?: string | null;
    name?: string | null;
    databaseType?: string | null;
    connectionString?: string | null;
    status?: ContainerStatus;
    subdomain?: string | null;
    port?: number;
    createdAt?: string;
    configuration?: Record<string, string> | null;
};

