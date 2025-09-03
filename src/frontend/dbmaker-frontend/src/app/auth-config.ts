export const authConfig = {
  auth: {
    clientId: 'your-client-id-here', // Replace with your Azure AD app registration client ID
    authority: 'https://login.microsoftonline.com/your-tenant-id-here', // Replace with your tenant ID
    redirectUri: 'http://localhost:4200', // For development
    postLogoutRedirectUri: 'http://localhost:4200'
  },
  cache: {
    cacheLocation: 'localStorage',
    storeAuthStateInCookie: false,
  }
};

export const protectedResources = {
  apiEndpoint: 'http://localhost:5000/api', // Your API endpoint
  scopes: ['api://your-api-client-id/access_as_user'] // Replace with your API scope
};
