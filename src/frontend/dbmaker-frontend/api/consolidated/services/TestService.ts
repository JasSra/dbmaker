/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { TestCreateContainerRequest } from '../models/TestCreateContainerRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TestService {
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static postApiTestCreateContainer(
        requestBody?: TestCreateContainerRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/test/create-container',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTestPort(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/test/port',
        });
    }
    /**
     * @param userId
     * @param containerName
     * @param databaseType
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTestSubdomain(
        userId: string,
        containerName: string,
        databaseType: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/test/subdomain/{userId}/{containerName}/{databaseType}',
            path: {
                'userId': userId,
                'containerName': containerName,
                'databaseType': databaseType,
            },
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTestDockerStatus(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/test/docker-status',
        });
    }
}
