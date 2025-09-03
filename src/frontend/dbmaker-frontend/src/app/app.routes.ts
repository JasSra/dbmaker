import { Routes } from '@angular/router';
import { MsalGuard } from '@azure/msal-angular';
import { DashboardComponent } from './dashboard/dashboard';
import { ContainerListComponent } from './container-list/container-list';
import { CreateContainerComponent } from './create-container/create-container';

export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [MsalGuard]
  },
  {
    path: 'containers',
    component: ContainerListComponent,
    canActivate: [MsalGuard]
  },
  {
    path: 'create',
    component: CreateContainerComponent,
    canActivate: [MsalGuard]
  }
];
