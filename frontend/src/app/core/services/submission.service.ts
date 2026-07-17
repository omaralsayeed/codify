/**
 * SubmissionService
 *
 * Covers two backend endpoints:
 *   POST /api/execution/run   → run code against sample cases (no persistence)
 *   POST /api/submissions     → submit code for judging (202 + poll)
 *   GET  /api/submissions/:id → poll for final verdict
 *
 * Base URL: http://localhost:5237  (matches launchSettings.json http profile)
 *
 * Auth: reads the JWT from localStorage key 'codify_token'.
 * TODO: replace the manual header with a proper HttpInterceptor once
 *       the Angular auth flow is wired to the real backend.
 *
 * Mocks are active while the backend is not running.
 * To swap any mock for the real call: uncomment the http block and
 * delete the mock line directly below it.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import {
  Observable, of, throwError,
  timer, switchMap, takeWhile, last,
  delay, catchError, map,
} from 'rxjs';
import {
  RunCodeRequest,
  RunCodeResponse,
  CreateSubmissionRequest,
  SubmissionDetailResponse,
  SubmissionLanguage,
  ServiceError,
} from '../models/submission.model';

/** Shape of every response envelope from the backend: { data: T } */
interface ApiEnvelope<T> { data: T; }

/**
 * Editor language keys → backend enum values.
 * JavaScript / Java / C++ have no backend judge yet → mock returns simulated results.
 */
const LANG_MAP: Record<string, SubmissionLanguage | null> = {
  python:     'Python',
  csharp:     'CSharp',
  javascript: null,   // mock-only; no backend judge support
  java:       null,   // mock-only
  cpp:        null,   // mock-only
};

/** Statuses that mean the judge hasn't finished yet */
const PENDING_STATUSES = new Set(['Pending', 'Running']);

@Injectable({ providedIn: 'root' })
export class SubmissionService {
  private readonly http = inject(HttpClient);
  private readonly API  = 'http://localhost:5237/api';

  // ── Auth helper ────────────────────────────────────────────────────────────

  private headers(): HttpHeaders {
    // TODO: replace with HttpInterceptor once real JWT auth is wired
    const token = localStorage.getItem('codify_token') ?? '';
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  private backendLang(editorLang: string): SubmissionLanguage | null {
    return LANG_MAP[editorLang.toLowerCase()] ?? null;
  }

  // ── Error handler ──────────────────────────────────────────────────────────

  private handleError(err: unknown): Observable<never> {
    if (err instanceof HttpErrorResponse) {
      const serviceErr: ServiceError = {
        code:    'http',
        status:  err.status,
        message: err.error?.message ?? err.message ?? 'Request failed',
      };
      return throwError(() => serviceErr);
    }
    const serviceErr: ServiceError = {
      code:    'unknown',
      message: err instanceof Error ? err.message : 'Unknown error',
    };
    return throwError(() => serviceErr);
  }

  // ── Run ────────────────────────────────────────────────────────────────────

  /**
   * POST /api/execution/run
   * Executes code against the problem's sample test cases.
   * Does NOT create a submission record — safe to call on every "Run" click.
   *
   * Falls back to a mock when the selected language has no backend judge,
   * or when the backend is unreachable.
   */
  run(problemId: string, code: string, editorLang: string): Observable<RunCodeResponse> {
    const lang = this.backendLang(editorLang);

    if (!lang) {
      // Language not supported by the judge — return a simulated response
      return this.mockRun(code);
    }

    const body: RunCodeRequest = { problemId, code, language: lang };

    // TODO: replace mock with real call when backend is running
    // return this.http
    //   .post<ApiEnvelope<RunCodeResponse>>(`${this.API}/execution/run`, body, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return this.mockRun(code); // ← remove this line when uncommenting above
  }

  // ── Submit ─────────────────────────────────────────────────────────────────

  /**
   * POST /api/submissions  → 202 Accepted  { data: SubmissionDetailResponse }
   *
   * The backend returns a 'Pending' submission immediately.
   * We then poll GET /api/submissions/:id every 1.5 s until the status
   * leaves 'Pending' / 'Running', then emit the final result and complete.
   */
  submit(problemId: string, code: string, editorLang: string): Observable<SubmissionDetailResponse> {
    const lang = this.backendLang(editorLang);

    if (!lang) {
      // Language not supported — simulate a full submission response
      return this.mockSubmit(code, editorLang);
    }

    const body: CreateSubmissionRequest = { problemId, code, language: lang };

    // TODO: replace mock with real call when backend is running
    // return this.http
    //   .post<ApiEnvelope<SubmissionDetailResponse>>(`${this.API}/submissions`, body, { headers: this.headers() })
    //   .pipe(
    //     map(r => r.data),
    //     switchMap(pending => this.pollUntilDone(pending.submissionId.toString())),
    //     catchError(e => this.handleError(e)),
    //   );

    return this.mockSubmit(code, editorLang); // ← remove when uncommenting above
  }

  /**
   * GET /api/submissions/:id
   * Used for polling. Returns the current snapshot of a submission.
   */
  getSubmission(id: string): Observable<SubmissionDetailResponse> {
    // TODO: replace mock with real call when backend is running
    // return this.http
    //   .get<ApiEnvelope<SubmissionDetailResponse>>(`${this.API}/submissions/${id}`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.buildMockAccepted()); // ← remove when uncommenting above
  }

  /**
   * Poll every 1.5 s until status leaves Pending / Running.
   * takeWhile with inclusive: true ensures the final (done) value is emitted.
   */
  private pollUntilDone(id: string): Observable<SubmissionDetailResponse> {
    return timer(0, 1500).pipe(
      switchMap(() => this.getSubmission(id)),
      takeWhile(r => PENDING_STATUSES.has(r.status), /* inclusive */ true),
      last(),
    );
  }

  // ── Mock implementations ───────────────────────────────────────────────────
  // Each mock simulates realistic latency so the UI loading states are visible.
  // Replace the return statement in the public method above to switch to real API.

  private mockRun(code: string): Observable<RunCodeResponse> {
    const passed = code.trim().length > 30;
    return of<RunCodeResponse>({
      stdout:         passed ? '[0, 1]\n[1, 2]\n[0, 1]' : '',
      stderr:         passed ? '' : 'No output produced — is your function returning a value?',
      executionTimeMs: 42,
      status:         passed ? 'Accepted' : 'WrongAnswer',
      testResults: [
        { input: 'nums=[2,7,11,15], target=9', expectedOutput: '[0,1]', actualOutput: passed ? '[0,1]' : '[]', passed },
        { input: 'nums=[3,2,4],     target=6', expectedOutput: '[1,2]', actualOutput: passed ? '[1,2]' : '[]', passed },
        { input: 'nums=[3,3],       target=6', expectedOutput: '[0,1]', actualOutput: passed ? '[0,1]' : '[]', passed },
      ],
    }).pipe(delay(900));
  }

  private mockSubmit(code: string, lang: string): Observable<SubmissionDetailResponse> {
    const accepted = code.trim().length > 30;
    return of<SubmissionDetailResponse>(
      accepted ? this.buildMockAccepted(lang) : this.buildMockWrong(lang)
    ).pipe(delay(1500));
  }

  private buildMockAccepted(lang = 'python'): SubmissionDetailResponse {
    return {
      submissionId:    crypto.randomUUID(),
      problemId:       '00000000-0000-0000-0000-000000000005',
      userId:          'mock-user-id',
      code:            '',
      language:        lang,
      status:          'Accepted',
      submittedAt:     new Date().toISOString(),
      executionTimeMs: 38,
      memoryUsedKb:    14200,
      passedTestCases: 32,
      totalTestCases:  32,
      score:           100,
      result: {
        passedTestCount: 32,
        failedTestCount: 0,
        totalTestCount:  32,
        outputSummary:   'All test cases passed.',
      },
      aiFeedback: [],
    };
  }

  private buildMockWrong(lang = 'python'): SubmissionDetailResponse {
    return {
      submissionId:    crypto.randomUUID(),
      problemId:       '00000000-0000-0000-0000-000000000005',
      userId:          'mock-user-id',
      code:            '',
      language:        lang,
      status:          'WrongAnswer',
      submittedAt:     new Date().toISOString(),
      executionTimeMs: 22,
      passedTestCases: 14,
      totalTestCases:  32,
      score:           0,
      result: {
        passedTestCount: 14,
        failedTestCount: 18,
        totalTestCount:  32,
        errorMessage:    'Expected [0,1] but got [].',
      },
      aiFeedback: [],
    };
  }
}
