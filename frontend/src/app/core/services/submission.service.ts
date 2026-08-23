/**
 * SubmissionService
 *
 * Covers two backend endpoints:
 *   POST /api/execution/run          → run code against sample cases (no persistence)
 *   POST /api/submissions            → submit code for judging (202 + poll)
 *   GET  /api/submissions/:id        → poll for final verdict
 *   GET  /api/submissions/:id/feedback → AI code-quality feedback (mock until backend ships)
 *
 * Base URL: configured via environment.apiUrl (see src/environments/)
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
  SubmissionSummaryResponse,
  SubmissionLanguage,
  ServiceError,
  SubmissionFeedback,
  FeedbackItem,
  FeedbackItemDisplay,
} from '../models/submission.model';
import { environment } from '../../../environments/environment';

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
  private readonly API  = environment.apiUrl;

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

    return this.http
      .post<ApiEnvelope<RunCodeResponse>>(`${this.API}/execution/run`, body, { headers: this.headers() })
      .pipe(map(r => r.data), catchError(e => this.handleError(e)));
  }

  // ── Submit ─────────────────────────────────────────────────────────────────

  /**
   * POST /api/submissions  → 202 Accepted  { data: SubmissionDetailResponse }
   *
   * The backend returns a 'Pending' submission immediately.
   * This method now returns the initial response immediately WITHOUT polling.
   * The component should call pollSubmission() separately to get the final result.
   */
  submit(problemId: string, code: string, editorLang: string): Observable<SubmissionDetailResponse> {
    const lang = this.backendLang(editorLang);

    if (!lang) {
      // Language not supported — simulate a full submission response
      return this.mockSubmit(code, editorLang);
    }

    const body: CreateSubmissionRequest = { problemId, code, language: lang };

    return this.http
      .post<ApiEnvelope<SubmissionDetailResponse>>(`${this.API}/submissions`, body, { headers: this.headers() })
      .pipe(
        map(r => r.data),
        // No longer polling here - return immediately
        catchError(e => this.handleError(e)),
      );
  }

  /**
   * Polls for the final submission result.
   * Call this after submit() returns to get the final verdict.
   */
  pollSubmission(submissionId: string): Observable<SubmissionDetailResponse> {
    return this.pollUntilDone(submissionId);
  }

  /**
   * GET /api/submissions/:id
   * Used for polling. Returns the current snapshot of a submission.
   */
  getSubmission(id: string): Observable<SubmissionDetailResponse> {
    return this.http
      .get<ApiEnvelope<SubmissionDetailResponse>>(`${this.API}/submissions/${id}`, { headers: this.headers() })
      .pipe(map(r => r.data), catchError(e => this.handleError(e)));
  }

  /**
   * Poll every 1.5 s until status leaves Pending / Running.
   * takeWhile with inclusive: true ensures the final (done) value is emitted.
   */
  private pollUntilDone(id: string): Observable<SubmissionDetailResponse> {
    console.log('[Polling] Starting to poll submission:', id);
    let pollCount = 0;
    return timer(0, 1500).pipe(
      switchMap(() => {
        pollCount++;
        console.log(`[Polling] Attempt ${pollCount} - fetching submission status...`);
        return this.getSubmission(id);
      }),
      takeWhile(r => {
        const isPending = PENDING_STATUSES.has(r.status);
        console.log(`[Polling] Attempt ${pollCount} - Status: ${r.status}, Is Pending: ${isPending}`);
        return isPending;
      }, /* inclusive */ true),
      last(),
    );
  }

  /**
   * GET /api/problems/:id/submissions
   * Returns the current user's submission history for a specific problem,
   * ordered by submittedAt DESC. Called when the Submissions tab is activated.
   */
  getSubmissionsByProblem(problemId: string): Observable<SubmissionSummaryResponse[]> {
    return this.http
      .get<ApiEnvelope<SubmissionSummaryResponse[]>>(
        `${this.API}/problems/${problemId}/submissions`,
        { headers: this.headers() },
      )
      .pipe(map(r => r.data), catchError(e => this.handleError(e)));
  }

  // ── AI Feedback ───────────────────────────────────────────────────────────

  /**
   * GET /api/submissions/:id/feedback
   *
   * Fetches AI-generated code-quality feedback for a completed submission.
   * Called automatically by the component once a submission ID is available.
   *
   * Since feedback is generated asynchronously by a background job that takes
   * ~7-8 seconds, this method implements intelligent polling:
   * - Retries up to 6 times (max 15 seconds total)
   * - Uses exponential backoff: 1s, 2s, 3s, 4s, 5s
   * - Returns immediately if feedback is available
   * - Returns empty array if still not ready after max attempts
   */
  getSubmissionFeedback(submissionId: string): Observable<SubmissionFeedback> {
    return this.pollForFeedback(submissionId, 0);
  }

  /**
   * Recursive polling helper for feedback.
   * @param submissionId - The submission ID to fetch feedback for
   * @param attempt - Current attempt number (0-indexed)
   * @param maxAttempts - Maximum number of polling attempts
   */
  private pollForFeedback(
    submissionId: string,
    attempt: number,
    maxAttempts: number = 6
  ): Observable<SubmissionFeedback> {
    return this.http
      .get<ApiEnvelope<FeedbackItem[]>>(
        `${this.API}/submissions/${submissionId}/feedback`,
        { headers: this.headers() },
      )
      .pipe(
        map(r => r.data),
        switchMap(feedbackArray => {
          // If we got feedback items, transform and return immediately
          if (feedbackArray && feedbackArray.length > 0) {
            return of(this.transformFeedbackResponse(feedbackArray));
          }

          // If we've exhausted our attempts, return empty feedback
          if (attempt >= maxAttempts) {
            console.warn(`[Feedback] No feedback available after ${maxAttempts} attempts for submission ${submissionId}`);
            return of(this.emptyFeedback());
          }

          // Calculate delay: 1s, 2s, 3s, 4s, 5s
          const delayMs = (attempt + 1) * 1000;
          console.log(`[Feedback] Attempt ${attempt + 1}/${maxAttempts}: No feedback yet, retrying in ${delayMs}ms...`);

          // Wait and retry
          return timer(delayMs).pipe(
            switchMap(() => this.pollForFeedback(submissionId, attempt + 1, maxAttempts))
          );
        }),
        catchError(e => {
          // On error, if we haven't exhausted attempts, retry after delay
          if (attempt < maxAttempts) {
            const delayMs = (attempt + 1) * 1000;
            console.warn(`[Feedback] Error on attempt ${attempt + 1}, retrying in ${delayMs}ms...`, e);
            return timer(delayMs).pipe(
              switchMap(() => this.pollForFeedback(submissionId, attempt + 1, maxAttempts))
            );
          }
          // Exhausted attempts, return error
          return this.handleError(e);
        })
      );
  }

  /**
   * Transforms the backend feedback array into the frontend SubmissionFeedback shape.
   * Calculates an overall score based on feedback types:
   * - CodeQuality: +10 points each
   * - Optimization: +15 points each
   * - AiGenerated: -20 points
   * Base score starts at 50, clamped between 0-100.
   */
  private transformFeedbackResponse(feedbackArray: FeedbackItem[]): SubmissionFeedback {
    let score = 50; // Base score

    const items: FeedbackItemDisplay[] = feedbackArray.map((item, index) => {
      // Adjust score based on feedback type
      if (item.feedbackType === 'CodeQuality') {
        score += 10;
      } else if (item.feedbackType === 'Optimization') {
        score += 15;
      } else if (item.feedbackType === 'AiGenerated') {
        score -= 20;
      }

      // Map backend type to frontend display type
      const displayType = this.mapFeedbackType(item.feedbackType);
      const severity = item.feedbackType === 'AiGenerated' ? 'high' as const : 'low' as const;

      return {
        id: item.id || `feedback-${index}`,
        type: displayType,
        title: this.generateTitle(item.feedbackType),
        description: item.message,
        message: item.message,
        severity,
        lineStart: null,
        lineEnd: null,
      };
    });

    // Clamp score between 0 and 100
    score = Math.max(0, Math.min(100, score));

    return {
      overallScore: score,
      feedbackItems: items,
      summary: this.generateSummary(feedbackArray),
    };
  }

  /**
   * Generates a title for the feedback item based on its type.
   */
  private generateTitle(type: 'CodeQuality' | 'Optimization' | 'AiGenerated'): string {
    switch (type) {
      case 'CodeQuality':
        return 'Code Quality';
      case 'Optimization':
        return 'Performance Optimization';
      case 'AiGenerated':
        return 'AI Detection';
      default:
        return 'Feedback';
    }
  }

  /**
   * Generates a summary for the feedback response.
   */
  private generateSummary(feedbackArray: FeedbackItem[]): string {
    const qualityCount = feedbackArray.filter(f => f.feedbackType === 'CodeQuality').length;
    const optimizationCount = feedbackArray.filter(f => f.feedbackType === 'Optimization').length;
    const aiGeneratedCount = feedbackArray.filter(f => f.feedbackType === 'AiGenerated').length;

    if (feedbackArray.length === 0) {
      return 'No feedback available yet. The analysis may still be processing.';
    }

    const parts: string[] = [];
    if (qualityCount > 0) parts.push(`${qualityCount} code quality suggestion${qualityCount > 1 ? 's' : ''}`);
    if (optimizationCount > 0) parts.push(`${optimizationCount} optimization tip${optimizationCount > 1 ? 's' : ''}`);
    if (aiGeneratedCount > 0) parts.push(`AI generation detected`);

    return `Your submission received ${parts.join(', ')}.`;
  }

  /**
   * Maps backend feedback types to frontend display types.
   * Backend: CodeQuality, Optimization, AiGenerated
   * Frontend: quality, optimization, anomaly
   */
  private mapFeedbackType(backendType: 'CodeQuality' | 'Optimization' | 'AiGenerated'): 'quality' | 'optimization' | 'anomaly' {
    switch (backendType) {
      case 'CodeQuality':
        return 'quality';
      case 'Optimization':
        return 'optimization';
      case 'AiGenerated':
        return 'anomaly';
      default:
        return 'quality'; // Fallback
    }
  }

  /**
   * Returns an empty feedback response when no feedback is available.
   */
  private emptyFeedback(): SubmissionFeedback {
    return {
      overallScore: 50, // Neutral score
      feedbackItems: [],
      summary: 'Feedback is being generated in the background. Please check back in a moment.',
    };
  }

  // ── Mock implementations ───────────────────────────────────────────────────
  // Mock verdict is determined by what the student typed:
  //   • Contains a real data structure (dict, map, {})  → Accepted
  //   • Contains only pass/return/{}  (starter code)    → WrongAnswer
  //   • Contains "error" or "throw"                     → RuntimeError
  //   • Contains "tle" or "while True"                  → TimeLimitExceeded
  //   • Otherwise (something typed, but not a solution) → WrongAnswer

  private mockVerdict(code: string): 'Accepted' | 'WrongAnswer' | 'RuntimeError' | 'TimeLimitExceeded' {
    const c = code.toLowerCase();
    if (c.includes('while true') || c.includes('tle')) return 'TimeLimitExceeded';
    if (c.includes('raise ') || c.includes('throw ') || c.includes('error(')) return 'RuntimeError';
    // A real Two Sum solution will mention a hash map / dict
    if (c.includes('seen') || c.includes('dict') || c.includes('hashmap') ||
        c.includes('{}') || c.includes('map(') || c.includes('lookup')) return 'Accepted';
    return 'WrongAnswer';
  }

  private mockRun(code: string): Observable<RunCodeResponse> {
    const verdict = this.mockVerdict(code);
    const passed  = verdict === 'Accepted';
    return of<RunCodeResponse>({
      stdout:          passed ? '[0, 1]\n[1, 2]\n[0, 1]' : '',
      stderr:          passed ? '' : verdict === 'RuntimeError'
                         ? 'KeyError: complement not found'
                         : 'No output produced — is your function returning a value?',
      executionTimeMs: 42,
      status:          verdict,
      testResults: [
        { input: 'nums=[2,7,11,15], target=9', expectedOutput: '[0,1]', actualOutput: passed ? '[0,1]' : '[]', passed },
        { input: 'nums=[3,2,4],     target=6', expectedOutput: '[1,2]', actualOutput: passed ? '[1,2]' : '[]', passed },
        { input: 'nums=[3,3],       target=6', expectedOutput: '[0,1]', actualOutput: passed ? '[0,1]' : '[]', passed },
      ],
    }).pipe(delay(900));
  }

  private mockSubmit(code: string, lang: string): Observable<SubmissionDetailResponse> {
    const verdict = this.mockVerdict(code);
    return of<SubmissionDetailResponse>(
      this.buildMockResult(verdict, lang)
    ).pipe(delay(1500));
  }

  private buildMockResult(
    status: 'Accepted' | 'WrongAnswer' | 'RuntimeError' | 'TimeLimitExceeded',
    lang = 'python',
  ): SubmissionDetailResponse {
    const accepted = status === 'Accepted';
    return {
      submissionId:    crypto.randomUUID(),
      problemId:       '00000000-0000-0000-0000-000000000005',
      userId:          'mock-user-id',
      code:            '',
      language:        lang,
      status,
      submittedAt:     new Date().toISOString(),
      executionTimeMs: accepted ? 38  : 22,
      memoryUsedKb:    accepted ? 14200 : 12800,
      passedTestCases: accepted ? 32 : status === 'RuntimeError' ? 0 : 14,
      totalTestCases:  32,
      score:           accepted ? 100 : 0,
      result: {
        passedTestCount: accepted ? 32 : 0,
        failedTestCount: accepted ? 0  : 32,
        totalTestCount:  32,
        errorMessage: accepted ? undefined
          : status === 'RuntimeError'    ? 'KeyError: complement not found at line 4'
          : status === 'TimeLimitExceeded' ? 'Time limit exceeded after 2000ms'
          : 'Expected [0,1] but got [].',
        outputSummary: accepted ? 'All test cases passed.' : undefined,
      },
      aiFeedback: [],
      testCaseResults: [],
    };
  }

  private buildMockAccepted(lang = 'python'): SubmissionDetailResponse {
    return this.buildMockResult('Accepted', lang);
  }

  private buildMockWrong(lang = 'python'): SubmissionDetailResponse {
    return this.buildMockResult('WrongAnswer', lang);
  }
}
