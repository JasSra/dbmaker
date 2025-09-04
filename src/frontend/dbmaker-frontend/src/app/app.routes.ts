import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard';
import { ContainerListComponent } from './container-list/container-list';
import { LandingComponent } from './landing/landing.component';

export const routes: Routes = [
  { path: '', component: LandingComponent },
  {
    path: 'setup',
    loadComponent: () => import('./setup/setup.component').then(m => m.SetupComponent)
  },
  {
    path: 'dashboard',
    component: DashboardComponent
  },
  {
    path: 'containers',
    component: ContainerListComponent
  },
  {
    path: 'create',
    loadComponent: () => import('./create-container/create-container.component').then(m => m.CreateContainerComponent)
  }
];
