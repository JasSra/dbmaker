/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { ContainerMonitoringData } from '../models/ContainerMonitoringData';
import type { ContainerResponse } from '../models/ContainerResponse';
import type { CreateContainerRequest } from '../models/CreateContainerRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ContainersService {
    /**
     * @returns ContainerResponse OK
     * @throws ApiError
     */
    public static getApiContainers(): CancelablePromise<Array<ContainerResponse>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Containers',
        });
    }
    /**
     * @param requestBody
     * @returns ContainerResponse OK
     * @throws ApiError
     */
    public static postApiContainers(
        requestBody?: CreateContainerRequest,
    ): CancelablePromise<ContainerResponse> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Containers',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param id
     * @returns ContainerResponse OK
     * @throws ApiError
     */
    public static getApiContainers1(
        id: string,
    ): CancelablePromise<ContainerResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Containers/{id}',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns any OK
     * @throws ApiError
     */
    public static deleteApiContainers(
        id: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/Containers/{id}',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns any OK
     * @throws ApiError
     */
    public static postApiContainersStart(
        id: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Containers/{id}/start',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns any OK
     * @throws ApiError
     */
    public static postApiContainersStop(
        id: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Containers/{id}/stop',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @param id
     * @returns ContainerMonitoringData OK
     * @throws ApiError
     */
    public static getApiContainersStats(
        id: string,
    ): CancelablePromise<ContainerMonitoringData> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Containers/{id}/stats',
            path: {
                'id': id,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiContainersAllDebug(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Containers/all-debug',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static postApiContainersCreateDemo(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Containers/create-demo',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiContainersDockerTest(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Containers/docker-test',
        });
    }
}
