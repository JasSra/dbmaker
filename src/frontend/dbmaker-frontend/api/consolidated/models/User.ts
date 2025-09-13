/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { DatabaseContainer } from './DatabaseContainer';
export type User = {
    id?: string | null;
    email?: string | null;
    name?: string | null;
    createdAt?: string;
    lastLoginAt?: string;
    isActive?: boolean;
    containers?: Array<DatabaseContainer> | null;
};

