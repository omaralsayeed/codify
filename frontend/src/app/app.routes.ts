import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '',          loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },
  { path: 'auth',      loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },
  { path: 'problems',  loadComponent: () => import('./features/problem-list/problem-list.component').then(m => m.ProblemListComponent), canActivate: [authGuard] },
  { path: 'dashboard', loadComponent: () => import('./features/student-dashboard/student-dashboard.component').then(m => m.StudentDashboardComponent), canActivate: [authGuard] },
  { path: '**',        redirectTo: '' }
];
