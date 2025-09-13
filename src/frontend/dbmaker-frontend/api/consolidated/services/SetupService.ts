/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { InitializationResult } from '../models/InitializationResult';
import type { InitializeSystemRequest } from '../models/InitializeSystemRequest';
import type { SetupStatus } from '../models/SetupStatus';
import type { ValidationResult } from '../models/ValidationResult';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class SetupService {
    /**
     * Check if the system needs initial setup
     * @returns SetupStatus OK
     * @throws ApiError
     */
    public static getApiSetupStatus(): CancelablePromise<SetupStatus> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Setup/status',
        });
    }
    /**
     * Validate Docker daemon connectivity
     * @returns ValidationResult OK
     * @throws ApiError
     */
    public static getApiSetupValidateDocker(): CancelablePromise<ValidationResult> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Setup/validate/docker',
        });
    }
    /**
     * Validate MSAL configuration
     * @returns ValidationResult OK
     * @throws ApiError
     */
    public static getApiSetupValidateMsal(): CancelablePromise<ValidationResult> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Setup/validate/msal',
        });
    }
    /**
     * Initialize the system with admin user and backup key
     * @param requestBody
     * @returns InitializationResult OK
     * @throws ApiError
     */
    public static postApiSetupInitialize(
        requestBody?: InitializeSystemRequest,
    ): CancelablePromise<InitializationResult> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/api/Setup/initialize',
            body: requestBody,
            mediaType: 'application/json',
        });
    }
}
