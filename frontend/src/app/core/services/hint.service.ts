/**
 * HintService
 *
 * Covers the backend endpoint:
 *   POST /api/ai/hints
 *
 * Base URL: http://localhost:5237  (matches launchSettings.json http profile)
 * Auth: reads JWT from localStorage key 'codify_token'.
 *
 * The backend returns one hint per call. The caller is responsible for
 * tracking hintLevel and previousHints across successive calls.
 * Max level: 3 (HintRequest.MaxHintLevel).
 *
 * To switch from mock to real: uncomment the http block in getHint()
 * and remove the mockHint() return line below it.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError, delay, catchError, map } from 'rxjs';
import { HintRequest, HintResponse } from '../models/hint.model';
import { ServiceError } from '../models/submission.model';

/** Shape of every response envelope from the backend: { data: T } */
interface ApiEnvelope<T> { data: T; }

@Injectable({ providedIn: 'root' })
export class HintService {
  private readonly http = inject(HttpClient);
  private readonly API  = 'http://localhost:5237/api';

  // ── Auth helper ────────────────────────────────────────────────────────────

  private headers(): HttpHeaders {
    // TODO: replace with HttpInterceptor once real JWT auth is wired
    const token = localStorage.getItem('codify_token') ?? '';
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  // ── Error handler ──────────────────────────────────────────────────────────

  private handleError(err: unknown): Observable<never> {
    if (err instanceof HttpErrorResponse) {
      const serviceErr: ServiceError = {
        code:    'http',
        status:  err.status,
        message: err.error?.message ?? err.message ?? 'Hint request failed',
      };
      return throwError(() => serviceErr);
    }
    const serviceErr: ServiceError = {
      code:    'unknown',
      message: err instanceof Error ? err.message : 'Unknown error',
    };
    return throwError(() => serviceErr);
  }

  // ── Public API ─────────────────────────────────────────────────────────────

  /**
   * POST /api/ai/hints
   *
   * Returns the next progressive hint for the student's current code state.
   * Call with hintLevel 1 first; on each subsequent call pass the previous
   * hintTexts in previousHints[] and increment hintLevel.
   *
   * The response's hasMoreHints flag tells you whether a level 2 or 3 call
   * will yield anything new.
   */
  getHint(req: HintRequest): Observable<HintResponse> {
    // TODO: replace mock with real call when backend is running
    // return this.http
    //   .post<ApiEnvelope<HintResponse>>(`${this.API}/ai/hints`, req, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return this.mockHint(req.hintLevel); // ← remove when uncommenting above
  }

  // ── Mock implementation ────────────────────────────────────────────────────

  private mockHint(level: number): Observable<HintResponse> {
    const hints: HintResponse[] = [
      {
        hintText:        'Think about what data structure lets you look up a value in O(1) time. For each number you visit, you want to quickly check whether its complement (target − number) has already been seen.',
        hintLevel:       1,
        followUpQuestion: 'What would you store as the key and value in that structure as you iterate?',
        hasMoreHints:    true,
      },
      {
        hintText:        'Use a hash map (dictionary) where the key is a number you\'ve seen and the value is its index. For each element nums[i], check if (target − nums[i]) is already in the map before inserting nums[i].',
        hintLevel:       2,
        followUpQuestion: 'Why do we check before inserting rather than after?',
        hasMoreHints:    true,
      },
      {
        hintText:        'Here\'s the pattern:\n\n  seen = {}\n  for i, n in enumerate(nums):\n      complement = target - n\n      if complement in seen:\n          return [seen[complement], i]\n      seen[n] = i\n\nThis runs in O(n) time and O(n) space — single pass.',
        hintLevel:       3,
        followUpQuestion: undefined,
        hasMoreHints:    false,
      },
    ];
    const idx = Math.min(level - 1, hints.length - 1);
    return of(hints[idx]).pipe(delay(1200));
  }
}
