import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Admin Guard — allows only authenticated users with the admin role.
 * Redirects non-admins to home and guests to login.
 *
 * Backend: role = 2 maps to 'admin' on the frontend.
 * All /admin/** routes use [authGuard, adminGuard].
 */
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router      = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/auth/login']);
    return false;
  }

  if (authService.user()?.role === 'admin') {
    return true;
  }

  // Authenticated but not an admin — send them home
  router.navigate(['/']);
  return false;
};
