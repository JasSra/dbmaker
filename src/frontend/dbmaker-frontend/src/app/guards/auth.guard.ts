import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { MsalService } from '@azure/msal-angular';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(private msal: MsalService, private router: Router) {}

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | Promise<boolean> | boolean {

    const isAuthenticated = this.msal.instance.getAllAccounts().length > 0;

    if (!isAuthenticated) {
      // Redirect to landing page for authentication
      this.router.navigate(['/']);
      return false;
    }

    return true;
  }
}
