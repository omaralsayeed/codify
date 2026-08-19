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
    return this.http
      .post<ApiEnvelope<HintResponse>>(`${this.API}/ai/hints`, req, { headers: this.headers() })
      .pipe(map(r => r.data), catchError(e => this.handleError(e)));
  }
}
