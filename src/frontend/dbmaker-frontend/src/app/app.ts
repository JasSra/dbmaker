import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { RouterModule } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { ThemeService, ThemeConfig } from './services/theme.service';
import { LoggerService } from './services/logger.service';
import { MsalService } from '@azure/msal-angular';
import { protectedResources } from './auth-config';
import { environment } from '../environments/environment';
import { OpenAPI } from '../../api/consolidated';
import { BreadcrumbsComponent } from './components/breadcrumbs/breadcrumbs.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    RouterOutlet,
    CommonModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatDividerModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule,
  RouterModule,
  BreadcrumbsComponent
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  title = 'StarkLink - Container Intelligence Platform';
  isAuthenticated = false;
  displayName = '';
  email = '';
  authBusy = false;
  isLoading = false;

  currentTheme: ThemeConfig;
  isDarkMode = false;

  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
  private themeService: ThemeService,
  private msalService: MsalService,
  private logger: LoggerService
  ) {
    this.currentTheme = this.themeService.getCurrentTheme();
    this.isDarkMode = this.currentTheme.isDarkMode;
  }

  ngOnInit(): void {
    // Subscribe to theme changes
    this.themeService.theme$
      .pipe(takeUntil(this.destroy$))
      .subscribe(theme => {
        this.currentTheme = theme;
        this.isDarkMode = theme.isDarkMode;
      });

    // Initialize authentication and handle redirects
    this.initializeAuthentication();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  goToCreate(): void {
    this.router.navigate(['/create']);
  }

  toggleDarkMode(): void {
    this.themeService.toggleDarkMode();
  }

  onThemeToggleChange(event: any): void {
    this.themeService.setDarkMode(event.checked);
  }

  private async initializeAuthentication(): Promise<void> {
    try {
      this.logger.debug('[Auth] Initializing authentication...');

      // Handle redirect promise if this is a callback from Azure B2C
  const redirectResult = await this.msalService.instance.handleRedirectPromise();
  this.logger.debug('[Auth] Redirect result:', redirectResult);

      // Update authentication status
      this.updateAuthenticationStatus();

      // Configure OpenAPI BASE once
  try {
        // Generated services already prefix with /api, so use origin only
        const url = new URL(environment.apiUrl);
        OpenAPI.BASE = `${url.protocol}//${url.host}`;
      } catch {
        // Fallback: if environment.apiUrl is full /api URL, strip trailing /api
        OpenAPI.BASE = environment.apiUrl.replace(/\/api$/i, '');
      }

      // Set TOKEN resolver to MSAL access token
      OpenAPI.TOKEN = async () => {
        const accounts = this.msalService.instance.getAllAccounts();
        if (!accounts.length) return '';
        try {
          const result = await this.msalService.instance.acquireTokenSilent({
            account: accounts[0],
            scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes]
          });
          const token = result.accessToken || '';
      if (token) this.logger.debug('[Auth] Token acquired');
      else this.logger.warn('[Auth] Empty accessToken from resolver');
          return token;
        } catch (e) {
      this.logger.warn('[Auth] Token resolver failed silently');
          // If interaction is required, try interactive redirect to get fresh consent/session
          const err: any = e as any;
          if (err && (err.errorCode === 'interaction_required' || err.errorCode === 'login_required' || err.errorCode === 'consent_required' || err.message?.includes('interaction_required'))) {
            try {
              await this.msalService.loginRedirect({
                scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes]
              });
            } catch (redirectErr) {
        this.logger.error('[Auth] Redirect during TOKEN resolver failed', redirectErr);
            }
          }
          return '';
        }
      };

      // Proactively acquire API access token silently (primes cache for interceptor)
      const accounts = this.msalService.instance.getAllAccounts();
      if (accounts.length > 0) {
        try {
          await this.msalService.instance.acquireTokenSilent({
            account: accounts[0],
            scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes]
          });
      this.logger.debug('[Auth] Access token acquired silently');
        } catch (e) {
      this.logger.warn('[Auth] Silent token acquisition failed; may need redirect');
          const err: any = e as any;
          if (err && (err.errorCode === 'interaction_required' || err.errorCode === 'login_required' || err.errorCode === 'consent_required' || err.message?.includes('interaction_required'))) {
            try {
              await this.msalService.loginRedirect({
                scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes]
              });
              return; // Redirecting
            } catch (redirectErr) {
        this.logger.error('[Auth] Redirect after silent failure failed', redirectErr);
            }
          }
        }
      }

      // Listen for account changes (login/logout events)
      this.msalService.instance.addEventCallback(async (event) => {
        this.logger.debug('[Auth] MSAL Event:', event.eventType);
        if (event.eventType === 'msal:loginSuccess' ||
            event.eventType === 'msal:logoutSuccess' ||
            event.eventType === 'msal:acquireTokenSuccess') {
          this.updateAuthenticationStatus();
          this.authBusy = false;
          // After login, proactively acquire an access token for API scopes so it's visible in logs
          if (event.eventType === 'msal:loginSuccess') {
            const accountsNow = this.msalService.instance.getAllAccounts();
            if (accountsNow.length) {
              try {
                const at = await this.msalService.instance.acquireTokenSilent({
                  account: accountsNow[0],
                  scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes]
                });
                if (at.accessToken) this.logger.debug('[Auth] Post-login token acquired');
              } catch (acqErr) {
                this.logger.warn('[Auth] Post-login acquireTokenSilent failed');
              }
            }
          }
          if (event.eventType === 'msal:logoutSuccess') {
            // Clear token resolver state by returning empty token next time
            OpenAPI.TOKEN = async () => '';
          }
        }
      });
    } catch (error) {
      this.logger.error('Error initializing authentication:', error);
      this.authBusy = false;
    }
  }

  private updateAuthenticationStatus(): void {
  const accounts = this.msalService.instance.getAllAccounts();
  this.logger.debug('[Auth] Accounts found:', accounts?.length || 0);

    this.isAuthenticated = accounts.length > 0;

    if (this.isAuthenticated && accounts[0]) {
      const account = accounts[0];
      // Try different properties for display name
      this.displayName = (account.name as string) ||
                        (account.idTokenClaims?.['name'] as string) ||
                        (account.idTokenClaims?.['given_name'] as string) ||
                        (account.username as string) ||
                        'User';

      // Try different properties for email
      this.email = (account.username as string) ||
                   (account.idTokenClaims?.['email'] as string) ||
                   (account.idTokenClaims?.['emails'] as string[])?.[0] ||
                   '';

  this.logger.debug('[Auth] User authenticated');
    } else {
      this.displayName = '';
      this.email = '';
  this.logger.debug('[Auth] No authenticated user found');
    }
  }

  login(): void {
    this.authBusy = true;
    try {
      this.msalService.loginRedirect({
  scopes: ['openid', 'profile', 'offline_access', ...protectedResources.scopes]
      });
    } catch (error) {
      this.logger.error('Login error:', error);
      this.authBusy = false;
    }
  }

  logout(): void {
    this.msalService.logoutRedirect({
      postLogoutRedirectUri: window.location.origin
    });
  }
}
