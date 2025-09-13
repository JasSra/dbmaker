/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { TokenValidationRequest } from '../models/TokenValidationRequest';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class AuthDebugService {
    /**
     * Test endpoint that doesn't require authentication
     * @returns any OK
     * @throws ApiError
     */
    public static getApiAuthDebugAnonymous(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AuthDebug/anonymous',
        });
    }
    /**
     * Test endpoint that requires authentication
     * @returns any OK
     * @throws ApiError
     */
    public static getApiAuthDebugAuthenticated(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AuthDebug/authenticated',
        });
    }
    /**
     * Test custom token validation directly
     * @param requestBody
     * @returns any OK
     * @throws ApiError
     */
    public static postApiAuthDebugValidateToken(
        requestBody?: TokenValidationRequest,
    ): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/AuthDebug/validate-token',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
    /**
     * Get current authentication status and token information
     * @returns any OK
     * @throws ApiError
     */
    public static getApiAuthDebugStatus(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AuthDebug/status',
        });
    }
    /**
     * Get detailed information about the current JWT token
     * @returns any OK
     * @throws ApiError
     */
    public static getApiAuthDebugTokenInfo(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/AuthDebug/token-info',
        });
    }
}
