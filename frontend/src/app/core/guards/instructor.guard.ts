import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Instructor Guard — allows only authenticated users with the instructor role.
 * Redirects students to their dashboard and guests to login.
 */
export const instructorGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/auth/login']);
    return false;
  }

  if (authService.user()?.role === 'instructor') {
    return true;
  }

  router.navigate(['/dashboard']);
  return false;
};
