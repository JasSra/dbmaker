import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MsalService } from '@azure/msal-angular';
import { protectedResources } from '../auth-config';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule],
  templateUrl: './landing.html',
  styleUrls: ['./landing.scss']
})
export class LandingComponent {
  constructor(private router: Router, private msal: MsalService) {}

  get isAuthenticated(): boolean {
    return this.msal.instance.getAllAccounts().length > 0;
  }

  getStarted() {
    if (this.isAuthenticated) {
      this.router.navigate(['/dashboard']);
    } else {
      this.msal.instance.initialize().then(() =>
        this.msal.loginRedirect({ scopes: protectedResources.scopes, redirectStartPage: `${window.location.origin}/dashboard` })
      );
    }
  }
}
