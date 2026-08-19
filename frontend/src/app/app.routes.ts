import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { authGuard } from './core/guards/auth.guard';
import { AuthService } from './core/services/auth.service';

/** Converts a display name to a URL-safe username slug. */
function toSlug(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, '_');
}

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then((m) => m.HomeComponent),
  },

  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },

  {
    path: 'problems',
    loadComponent: () =>
      import('./features/problem-list/problem-list.component').then(
        (m) => m.ProblemListComponent
      ),
    canActivate: [authGuard],
  },

  {
    path: 'problems/:id',
    loadComponent: () =>
      import('./features/problem-page/problem-page.component').then(
        (m) => m.ProblemPageComponent
      ),
    canActivate: [authGuard],
    data: { hideLayout: true },
  },

  // Public profile (accessible without authentication)
  {
    path: 'profile/:username',
    loadComponent: () =>
      import('./features/profile/profile.component').then(
        (m) => m.ProfileComponent
      ),
  },

  // Redirect logged-in users to their own profile
  {
    path: 'dashboard',
    redirectTo: () => {
      const user = inject(AuthService).currentUser();
      return user ? `/profile/${toSlug(user.name)}` : '/';
    },
  },

  // Student progress
  {
    path: 'progress',
    loadComponent: () =>
      import('./features/student-progress/student-progress.component').then(
        (m) => m.StudentProgressComponent
      ),
    canActivate: [authGuard],
  },

  // Student contests & challenges arena
  {
    path: 'contests',
    loadComponent: () =>
      import('./features/student-contests/student-contests.component').then(
        (m) => m.StudentContestsComponent
      ),
    canActivate: [authGuard],
  },

  // Student contest arena lobby — view all problems & choose which to solve
  {
    path: 'contests/:id',
    loadComponent: () =>
      import('./features/student-contests/contest-arena/student-contest-arena.component').then(
        (m) => m.StudentContestArenaComponent
      ),
    canActivate: [authGuard],
  },

  // Instructor module
  {
    path: 'instructor',
    loadChildren: () =>
      import('./features/instructor/instructor.routes').then(
        (m) => m.INSTRUCTOR_ROUTES
      ),
  },

  // Admin module — full-screen control panel, no global navbar
  {
    path: 'admin',
    loadChildren: () =>
      import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES),
  },

  {
    path: '**',
    redirectTo: '',
  },
];