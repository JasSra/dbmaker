/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { RemoteDockerHost } from '../models/RemoteDockerHost';
import type { SettingsResponse } from '../models/SettingsResponse';
import type { UpdateSettingsRequest } from '../models/UpdateSettingsRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class SettingsService {
    /**
     * @returns SettingsResponse OK
     * @throws ApiError
     */
    public static getApiSettings(): CancelablePromise<SettingsResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Settings',
        });
    }
    /**
     * @param requestBody
     * @returns SettingsResponse OK
     * @throws ApiError
     */
    public static putApiSettings(
        requestBody?: UpdateSettingsRequest,
    ): CancelablePromise<SettingsResponse> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/Settings',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @returns SettingsResponse OK
     * @throws ApiError
     */
    public static getApiSettingsGlobal(): CancelablePromise<SettingsResponse> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Settings/global',
        });
    }
    /**
     * @param requestBody
     * @returns SettingsResponse OK
     * @throws ApiError
     */
    public static putApiSettingsGlobal(
        requestBody?: UpdateSettingsRequest,
    ): CancelablePromise<SettingsResponse> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/api/Settings/global',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static postApiSettingsDockerRemoteHost(
        requestBody?: RemoteDockerHost,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Settings/docker/remote-host',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * @param hostId
     * @returns any OK
     * @throws ApiError
     */
    public static deleteApiSettingsDockerRemoteHost(
        hostId: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/api/Settings/docker/remote-host/{hostId}',
            path: {
                'hostId': hostId,
            },
        });
    }
}
