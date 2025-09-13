/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerLogs } from '../models/ContainerLogs';
import type { ContainerMonitoringData } from '../models/ContainerMonitoringData';
import type { ContainerTestResult } from '../models/ContainerTestResult';
import type { MonitoringSummary } from '../models/MonitoringSummary';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class MonitoringService {
    /**
     * Get real-time statistics for all user containers
     * @returns ContainerMonitoringData OK
     * @throws ApiError
     */
    public static getApiMonitoringStats(): CancelablePromise<Array<ContainerMonitoringData>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Monitoring/stats',
        });
    }
    /**
     * Get statistics for a specific container
     * @param containerId
     * @returns ContainerMonitoringData OK
     * @throws ApiError
     */
    public static getApiMonitoringStats1(
        containerId: string,
    ): CancelablePromise<ContainerMonitoringData> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Monitoring/stats/{containerId}',
            path: {
                'containerId': containerId,
            },
        });
    }
    /**
     * Get system-wide monitoring summary (admin only)
     * @returns MonitoringSummary OK
     * @throws ApiError
     */
    public static getApiMonitoringSummary(): CancelablePromise<MonitoringSummary> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Monitoring/summary',
        });
    }
    /**
     * Get container logs (if available)
     * @param containerId
     * @param lines
     * @returns ContainerLogs OK
     * @throws ApiError
     */
    public static getApiMonitoringLogs(
        containerId: string,
        lines: number = 100,
    ): CancelablePromise<ContainerLogs> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Monitoring/logs/{containerId}',
            path: {
                'containerId': containerId,
            },
            query: {
                'lines': lines,
            },
        });
    }
    /**
     * Test container connectivity
     * @param containerId
     * @returns ContainerTestResult OK
     * @throws ApiError
     */
    public static postApiMonitoringTest(
        containerId: string,
    ): CancelablePromise<ContainerTestResult> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Monitoring/test/{containerId}',
            path: {
                'containerId': containerId,
            },
        });
    }
}
