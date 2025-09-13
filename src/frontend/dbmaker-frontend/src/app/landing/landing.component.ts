import { Component, OnInit } from '@angular/core';
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
export class LandingComponent implements OnInit {
  constructor(private router: Router, private msal: MsalService) {}

  async ngOnInit() {
    console.log('=== AUTHENTICATION DEBUG START ===');
    console.log('Landing component initialized');
    console.log('Current URL:', this.currentUrl);
    console.log('URL hash:', window.location.hash);
    console.log('URL search:', window.location.search);
    console.log('Is authenticated:', this.isAuthenticated);
    console.log('Account count:', this.accountCount);
    console.log('All accounts:', this.msal.instance.getAllAccounts());
    console.log('MSAL configuration:', this.msal.instance.getConfiguration());
    console.log('Local storage MSAL keys:', this.getMsalStorageKeys());

    // Always try to handle redirect promise first
    try {
      console.log('Attempting to handle redirect promise...');

      const response = await this.msal.instance.handleRedirectPromise();
      console.log('Redirect response received:', response);

      if (response) {
        if (response.account) {
          console.log('Authentication successful!', response.account);
          console.log('Access token:', response.accessToken);
          console.log('ID token:', response.idToken);
          // Clean the URL by navigating to dashboard
          await this.router.navigate(['/dashboard']);
          return;
        } else {
          console.log('Response received but no account or error details');
        }
      }

      // Check if user is already authenticated
      const accounts = this.msal.instance.getAllAccounts();
      if (accounts.length > 0) {
        console.log('User is already authenticated, redirecting to dashboard...');
        await this.router.navigate(['/dashboard']);
        return;
      }

      console.log('No authentication detected, staying on landing page');
    } catch (error) {
      console.error('Error during authentication check:', error);
      if (error instanceof Error) {
        console.error('Error details:', {
          message: error.message,
          stack: error.stack,
          name: error.name
        });
      } else {
        console.error('Unknown error:', error);
      }
    }
  }

  get isAuthenticated(): boolean {
    return this.msal.instance.getAllAccounts().length > 0;
  }

  get accountCount(): number {
    return this.msal.instance.getAllAccounts().length;
  }

  get currentUrl(): string {
    return window.location.href;
  }

  getMsalStorageKeys(): any {
    const msalKeys: any = {};
    for (let i = 0; i < localStorage.length; i++) {
      const key = localStorage.key(i);
      if (key && (key.includes('msal') || key.includes('login.microsoftonline') || key.includes('b2clogin'))) {
        msalKeys[key] = localStorage.getItem(key);
      }
    }
    return msalKeys;
  }

  async getStarted() {
    if (this.isAuthenticated) {
      await this.router.navigate(['/dashboard']);
    } else {
      try {
        console.log('Starting login process...');
        console.log('MSAL configuration:', this.msal.instance.getConfiguration());

        // Ensure MSAL is initialized
        await this.msal.instance.initialize();
        console.log('MSAL initialized successfully');

        const loginRequest = {
          scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes],
          redirectStartPage: `${window.location.origin}/dashboard`
        };
        console.log('Login request:', loginRequest);

        await this.msal.loginRedirect(loginRequest);
      } catch (error) {
        console.error('Authentication error:', error);
        if (error instanceof Error) {
          console.error('Error details:', {
            message: error.message,
            stack: error.stack,
            name: error.name
          });
        }
      }
    }
  }
}
