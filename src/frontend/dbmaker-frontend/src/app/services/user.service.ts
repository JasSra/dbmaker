import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { DbMakerApiClient, UsersService } from '../api-client/DbMakerApiClient';
import { User } from '../models/container.models';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private usersClient: UsersService;

  constructor() {
    const apiClient = new DbMakerApiClient('http://localhost:5021');
    this.usersClient = apiClient.users;
  }

  getCurrentUser(): Observable<User> {
    return this.usersClient.getCurrentUser();
  }

  getUserStats(): Observable<any> {
    return this.usersClient.getUserStats();
  }
}
