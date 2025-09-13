/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { User } from '../models/User';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class UsersService {
    /**
     * @returns User OK
     * @throws ApiError
     */
    public static getApiUsersMe(): CancelablePromise<User> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Users/me',
        });
    }
    /**
     * @returns any OK
     * @throws ApiError
     */
    public static getApiUsersMeStats(): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Users/me/stats',
        });
    }
}
