/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerSettings } from './ContainerSettings';
import type { DockerSettings } from './DockerSettings';
import type { NginxSettings } from './NginxSettings';
import type { UISettings } from './UISettings';
export type SystemSettings = {
    id?: string | null;
    userId?: string | null;
    docker?: DockerSettings;
    ui?: UISettings;
    nginx?: NginxSettings;
    containers?: ContainerSettings;
    createdAt?: string;
    updatedAt?: string;
};

