import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard';
import { ContainerListComponent } from './container-list/container-list';
import { LandingComponent } from './landing/landing.component';
import { AuthGuard } from './guards/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingComponent, data: { title: 'Home', breadcrumb: 'Home' } },
  {
    path: 'setup',
    loadComponent: () => import('./setup/setup.component').then(m => m.SetupComponent),
    data: { title: 'Setup', breadcrumb: 'Setup' }
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [AuthGuard],
    data: { title: 'Dashboard', breadcrumb: 'Dashboard' }
  },
  {
    path: 'containers',
    component: ContainerListComponent,
    canActivate: [AuthGuard],
    data: { title: 'Containers', breadcrumb: 'Containers' }
  },
  {
    path: 'create',
    loadComponent: () => import('./create-container/create-container.component').then(m => m.CreateContainerComponent),
    canActivate: [AuthGuard],
    data: { title: 'Create', breadcrumb: 'Create' }
  },
  {
    path: 'settings',
    loadComponent: () => import('./settings/settings').then(m => m.SettingsComponent),
    canActivate: [AuthGuard],
    data: { title: 'Settings', breadcrumb: 'Settings' }
  },
  {
    path: 'analytics',
    loadComponent: () => import('./analytics/analytics.component').then(m => m.AnalyticsComponent),
    canActivate: [AuthGuard],
    data: { title: 'Analytics', breadcrumb: 'Analytics' }
  },
  // Redirect any unknown route to landing
  { path: '**', redirectTo: '' }
];
