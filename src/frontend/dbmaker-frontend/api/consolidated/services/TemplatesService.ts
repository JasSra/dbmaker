/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TemplatesService {
    /**
     * @param category
     * @param q
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTemplates(
        category?: string,
        q?: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Templates',
            query: {
                'category': category,
                'q': q,
            },
        });
    }
    /**
     * @param key
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTemplates1(
        key: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Templates/{key}',
            path: {
                'key': key,
            },
        });
    }
    /**
     * @param key
     * @param version
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTemplatesVersions(
        key: string,
        version: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Templates/{key}/versions/{version}',
            path: {
                'key': key,
                'version': version,
            },
        });
    }
    /**
     * @param key
     * @param version
     * @param overrides
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTemplatesPreview(
        key: string,
        version?: string,
        overrides?: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Templates/{key}/preview',
            path: {
                'key': key,
            },
            query: {
                'version': version,
                'overrides': overrides,
            },
        });
    }
}
