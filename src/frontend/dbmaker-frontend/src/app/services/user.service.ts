import { Injectable } from '@angular/core';
import { Observable, from } from 'rxjs';
import { UsersService as ApiUsersService } from '../../../api/consolidated';
import type { User } from '../../../api/consolidated';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  getCurrentUser(): Observable<User> {
  return from(ApiUsersService.getApiUsersMe());
  }

  getUserStats(): Observable<any> {
  return from(ApiUsersService.getApiUsersMeStats());
  }
}
