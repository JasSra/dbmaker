import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../models/container.models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private readonly baseUrl = 'http://localhost:5021/api';

  constructor(private http: HttpClient) {}

  getCurrentUser(): Observable<User> {
  return this.http.get<User>(`${this.baseUrl}/users/me`);
  }

  getUserStats(): Observable<any> {
  return this.http.get<any>(`${this.baseUrl}/users/me/stats`);
  }
}
