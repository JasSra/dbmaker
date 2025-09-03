import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi, HTTP_INTERCEPTORS } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

import { routes } from './app.routes';
import { authConfig, protectedResources } from './auth-config';
import { MsalInterceptor, MsalModule, MsalService, MSAL_INSTANCE, MSAL_INTERCEPTOR_CONFIG } from '@azure/msal-angular';
import { IPublicClientApplication, InteractionType, PublicClientApplication } from '@azure/msal-browser';

export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication(authConfig);
}

export function MSALInterceptorConfigFactory() {
  const protectedResourceMap = new Map<string, Array<string>>([
    [`${protectedResources.apiEndpoint}/*`, protectedResources.scopes],
    [protectedResources.apiEndpoint, protectedResources.scopes]
  ]);
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
  provideHttpClient(withInterceptorsFromDi()),
  importProvidersFrom(BrowserModule),
  importProvidersFrom(MsalModule),
  { provide: MSAL_INSTANCE, useFactory: MSALInstanceFactory },
  {
    provide: APP_INITIALIZER,
    multi: true,
    deps: [MSAL_INSTANCE],
    useFactory: (msalInstance: IPublicClientApplication) => () => msalInstance.initialize()
  },
  { provide: MSAL_INTERCEPTOR_CONFIG, useFactory: MSALInterceptorConfigFactory },
  { provide: HTTP_INTERCEPTORS, useClass: MsalInterceptor, multi: true },
  MsalService
  ]
};
