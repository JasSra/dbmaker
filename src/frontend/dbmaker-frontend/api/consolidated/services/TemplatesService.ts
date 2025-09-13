/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class TemplatesService {
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTemplates(): CancelablePromise<Array<any>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Templates',
        });
    }
    /**
     * @param type
     * @returns any OK
     * @throws ApiError
     */
    public static getApiTemplates1(
        type: string,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Templates/{type}',
            path: {
                'type': type,
            },
        });
    }
}
