import { Routes } from '@angular/router';
import { inject } from '@angular/core';
import { authGuard } from './core/guards/auth.guard';
import { AuthService } from './core/services/auth.service';

/** Converts a display name to a URL-safe username slug. */
function toSlug(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, '_');
}

export const routes: Routes = [
  { path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent) },

  { path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },

  { path: 'problems',
    loadComponent: () => import('./features/problem-list/problem-list.component').then(m => m.ProblemListComponent),
    canActivate: [authGuard] },

  { path: 'problems/:id',
    loadComponent: () => import('./features/problem-page/problem-page.component').then(m => m.ProblemPageComponent),
    canActivate: [authGuard],
    data: { hideLayout: true } },

  // ── Public profile ─────────────────────────────────────────────────────────
  // No auth guard — anyone can view a profile
  { path: 'profile/:username',
    loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },

  // ── /dashboard now redirects to the logged-in user's own profile ──────────
  { path: 'dashboard',
    redirectTo: () => {
      const user = inject(AuthService).currentUser();
      return user ? `/profile/${toSlug(user.name)}` : '/';
    },
  },

  // ── Private progress page (unchanged) ─────────────────────────────────────
  { path: 'progress',
    loadComponent: () => import('./features/student-progress/student-progress.component').then(m => m.StudentProgressComponent),
    canActivate: [authGuard] },

  { path: '**', redirectTo: '' },
];
