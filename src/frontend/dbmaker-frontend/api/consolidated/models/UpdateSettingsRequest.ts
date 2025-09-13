/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerSettings } from './ContainerSettings';
import type { DockerSettings } from './DockerSettings';
import type { NginxSettings } from './NginxSettings';
import type { UISettings } from './UISettings';
export type UpdateSettingsRequest = {
    docker?: DockerSettings;
    ui?: UISettings;
    nginx?: NginxSettings;
    containers?: ContainerSettings;
};

