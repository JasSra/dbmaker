// Azure B2C Configuration
import { environment } from '../environments/environment';

const AUTH_CLIENT_ID = (globalThis as any).process?.env?.NEXT_PUBLIC_MSAL_CLIENT_ID || 'c83c5908-2b64-4304-8c53-b964ace5a1ea';
const AUTH_AUTHORITY = (globalThis as any).process?.env?.NEXT_PUBLIC_MSAL_AUTHORITY || 'https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0';
const AUTH_KNOWN_AUTHORITIES = ((globalThis as any).process?.env?.NEXT_PUBLIC_MSAL_KNOWN_AUTHORITIES || 'jsraauth.b2clogin.com').split(',').map((s: string) => s.trim()).filter(Boolean);

export const MSAL_SCOPES = {
  admin: "https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.Administrator",
  client: "https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.Client",
  user: "https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.User",
} as const;

export const authConfig = {
  auth: {
    clientId: AUTH_CLIENT_ID,
    authority: AUTH_AUTHORITY,
    knownAuthorities: AUTH_KNOWN_AUTHORITIES,
    redirectUri: typeof window !== 'undefined' ? `${window.location.protocol}//${window.location.host}` : 'http://localhost:4200',
    postLogoutRedirectUri: typeof window !== 'undefined' ? `${window.location.protocol}//${window.location.host}` : 'http://localhost:4200',
    navigateToLoginRequestUrl: false  // Prevent automatic navigation after login
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level: any, message: string, containsPii: boolean) => {
        if (containsPii) return;
        // Reduce console noise in production-like UX
        if (level === 0) console.error('MSAL Error:', message);
        else if (level === 1 && environment.features.enableDebugMode) console.warn('MSAL Warning:', message);
        else if (level === 2 && environment.features.enableDebugMode) console.info('MSAL Info:', message);
        else if (level === 3 && environment.features.enableDebugMode) console.debug('MSAL Verbose:', message);
      },
      logLevel: environment.features.enableDebugMode ? 3 : 1,
      piiLoggingEnabled: false
    }
  }
};

export const protectedResources = {
  apiEndpoint: environment.apiUrl,
  scopes: [MSAL_SCOPES.user]
};
