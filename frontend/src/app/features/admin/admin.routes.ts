import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { adminGuard } from '../../core/guards/admin.guard';
import { AdminShellComponent } from './shell/admin-shell.component';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    component: AdminShellComponent,
    canActivate: [authGuard, adminGuard],
    data: { hideLayout: true },
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./overview/admin-overview.component').then(m => m.AdminOverviewComponent),
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./users/admin-users.component').then(m => m.AdminUsersComponent),
      },
      {
        path: 'users/:id',
        loadComponent: () =>
          import('./user-detail/admin-user-detail.component').then(m => m.AdminUserDetailComponent),
      },
      {
        path: 'problems',
        loadComponent: () =>
          import('./problems/admin-problems.component').then(m => m.AdminProblemsComponent),
      },
      {
        path: 'problems/new',
        loadComponent: () =>
          import('./problem-form/admin-problem-form.component').then(m => m.AdminProblemFormComponent),
      },
      {
        path: 'problems/:id/edit',
        loadComponent: () =>
          import('./problem-form/admin-problem-form.component').then(m => m.AdminProblemFormComponent),
      },
    ],
  },
];
