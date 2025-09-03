// Azure B2C Configuration
const AUTH_CLIENT_ID = (globalThis as any).process?.env?.NEXT_PUBLIC_MSAL_CLIENT_ID || 'c83c5908-2b64-4304-8c53-b964ace5a1ea';
const AUTH_AUTHORITY = (globalThis as any).process?.env?.NEXT_PUBLIC_MSAL_AUTHORITY || 'https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0';
const AUTH_KNOWN_AUTHORITIES = ((globalThis as any).process?.env?.NEXT_PUBLIC_MSAL_KNOWN_AUTHORITIES || 'jsraauth.b2clogin.com').split(',').map((s: string) => s.trim()).filter(Boolean);

// Scope helpers (replace TENANT_DOMAIN and APP_ID with your values)
const TENANT_DOMAIN = 'jsraauth.onmicrosoft.com';
const APP_ID = 'api-dbmaker'; // API Application ID URI name (as configured in Expose an API)
export const MSAL_SCOPES = {
  admin: `https://${TENANT_DOMAIN}/${APP_ID}/Consolidated.Administrator`,
  client: `https://${TENANT_DOMAIN}/${APP_ID}/Consolidated.Client`,
  user: `https://${TENANT_DOMAIN}/${APP_ID}/Consolidated.User`,
} as const;

export const authConfig = {
  auth: {
    clientId: AUTH_CLIENT_ID,
    authority: AUTH_AUTHORITY,
    knownAuthorities: AUTH_KNOWN_AUTHORITIES,
    redirectUri: 'http://localhost:4201', // For development
    postLogoutRedirectUri: 'http://localhost:4201'
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  }
};

export const protectedResources = {
  apiEndpoint: 'http://localhost:5021/api',
  scopes: [MSAL_SCOPES.user]
};
