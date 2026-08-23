import { Injectable, inject, effect } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';

/**
 * PlanRedirectService — Chunk 6 (Stripe success redirect handler)
 *
 * How it works:
 * 1. In your Stripe Dashboard, set each Payment Link's "success URL" to:
 *      https://[your-domain]/?plan=learner   (Learner monthly + yearly)
 *      https://[your-domain]/?plan=proplus   (Pro Plus monthly + yearly)
 *
 * 2. After a successful test payment, Stripe redirects the user back to
 *    that URL. This service reads the `?plan=` param on every app boot.
 *
 * 3. If the user is already logged in → sets the plan immediately.
 *    If not logged in yet → stores the pending plan in sessionStorage
 *    and picks it up the moment the user signal becomes non-null.
 *
 * 4. Cleans the query param from the URL after reading so it doesn't
 *    persist across refreshes or show in the browser history.
 */

const SESSION_KEY = 'codify_pending_plan';
const VALID_PLANS = new Set(['free', 'learner', 'proplus']);

@Injectable({ providedIn: 'root' })
export class PlanRedirectService {
  private readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

  constructor() {
    this.handleRedirect();
  }

  private handleRedirect(): void {
    const params  = new URLSearchParams(window.location.search);
    const rawPlan = params.get('plan')?.toLowerCase().trim() ?? '';

    if (rawPlan && VALID_PLANS.has(rawPlan)) {
      const plan = rawPlan as 'free' | 'learner' | 'proplus';

      // Clean the query param from the URL immediately — no history entry
      const cleanUrl = window.location.pathname + window.location.hash;
      window.history.replaceState({}, '', cleanUrl);

      if (this.auth.isLoggedIn()) {
        // User is already logged in — apply right away
        this.auth.setPlan(plan);
      } else {
        // User landed here before logging in (rare but possible)
        // Store in sessionStorage and apply after login
        sessionStorage.setItem(SESSION_KEY, plan);
      }
    }

    // Watch for the user signal becoming non-null (login completes)
    // to pick up any plan stored during a pre-login redirect
    effect(() => {
      const user = this.auth.user();
      if (!user) return;

      const pending = sessionStorage.getItem(SESSION_KEY) as 'free' | 'learner' | 'proplus' | null;
      if (pending && VALID_PLANS.has(pending)) {
        sessionStorage.removeItem(SESSION_KEY);
        this.auth.setPlan(pending);
      }
    });
  }
}
