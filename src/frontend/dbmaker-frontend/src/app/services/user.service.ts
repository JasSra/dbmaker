import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../models/container.models';
import { protectedResources } from '../auth-config';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly baseUrl = protectedResources.apiEndpoint;

  constructor(private http: HttpClient) {}

  getCurrentUser(): Observable<User> {
    return this.http.get<User>(`${this.baseUrl}/users/me`);
  }

  getUserStats(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/users/me/stats`);
  }
}
