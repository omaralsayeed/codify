/**
 * AnalyticsService
 *
 * Powers the Student Dashboard with data from the Analytics Agent.
 *
 * All methods are currently mocked with realistic data and a delay(1200)
 * to simulate network latency. When the real Analytics Agent API is ready,
 * only this service changes — nothing in the components.
 *
 * Base URL (future): http://localhost:5237/api
 * Auth: reads JWT from localStorage key 'codify_token'.
 *
 * TODO: replace mock implementations with real API calls once the
 *       Analytics Agent endpoints are available.
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError, delay, catchError, map } from 'rxjs';
import {
  StudentDashboardData,
  DashboardSummary,
  TopicStat,
  WeeklyActivity,
  ScorePoint,
  RecommendedProblem,
} from '../models/analytics.model';
import { ServiceError } from '../models/submission.model';

/** Shape of every response envelope from the backend: { data: T } */
interface ApiEnvelope<T> { data: T; }

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
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
        message: err.error?.message ?? err.message ?? 'Analytics request failed',
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
   * GET /api/analytics/dashboard
   *
   * Returns the full student dashboard payload in one call.
   * TODO: replace with real Analytics Agent API call
   */
  getDashboard(): Observable<StudentDashboardData> {
    // TODO: replace with real Analytics Agent API call
    // return this.http
    //   .get<ApiEnvelope<StudentDashboardData>>(`${this.API}/analytics/dashboard`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.mockDashboard()).pipe(delay(1200));
  }

  /**
   * GET /api/analytics/summary
   *
   * Returns only the headline summary stats.
   * TODO: replace with real Analytics Agent API call
   */
  getSummary(): Observable<DashboardSummary> {
    // TODO: replace with real Analytics Agent API call
    // return this.http
    //   .get<ApiEnvelope<DashboardSummary>>(`${this.API}/analytics/summary`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.mockDashboard().summary).pipe(delay(1200));
  }

  /**
   * GET /api/analytics/topics
   *
   * Returns topic mastery stats with trend indicators.
   * TODO: replace with real Analytics Agent API call
   */
  getTopicStats(): Observable<TopicStat[]> {
    // TODO: replace with real Analytics Agent API call
    // return this.http
    //   .get<ApiEnvelope<TopicStat[]>>(`${this.API}/analytics/topics`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.mockDashboard().topicStats).pipe(delay(1200));
  }

  /**
   * GET /api/analytics/activity
   *
   * Returns weekly activity (problems solved per day) for the last 4 weeks.
   * TODO: replace with real Analytics Agent API call
   */
  getWeeklyActivity(): Observable<WeeklyActivity[]> {
    // TODO: replace with real Analytics Agent API call
    // return this.http
    //   .get<ApiEnvelope<WeeklyActivity[]>>(`${this.API}/analytics/activity`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.mockDashboard().weeklyActivity).pipe(delay(1200));
  }

  /**
   * GET /api/analytics/scores
   *
   * Returns score history for the rolling 30-day trend chart.
   * TODO: replace with real Analytics Agent API call
   */
  getScoreHistory(): Observable<ScorePoint[]> {
    // TODO: replace with real Analytics Agent API call
    // return this.http
    //   .get<ApiEnvelope<ScorePoint[]>>(`${this.API}/analytics/scores`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.mockDashboard().scoreHistory).pipe(delay(1200));
  }

  /**
   * GET /api/analytics/recommendations
   *
   * Returns personalized problem recommendations from the Analytics Agent,
   * ordered by priority (weakest topics first).
   * TODO: replace with real Analytics Agent API call
   */
  getRecommendations(): Observable<RecommendedProblem[]> {
    // TODO: replace with real Analytics Agent API call
    // return this.http
    //   .get<ApiEnvelope<RecommendedProblem[]>>(`${this.API}/analytics/recommendations`, { headers: this.headers() })
    //   .pipe(map(r => r.data), catchError(e => this.handleError(e)));

    return of(this.mockDashboard().recommendations).pipe(delay(1200));
  }

  // ── Mock data ──────────────────────────────────────────────────────────────

  private mockDashboard(): StudentDashboardData {
    return {
      summary:          this.mockSummary(),
      topicStats:       this.mockTopicStats(),
      weeklyActivity:   this.mockWeeklyActivity(),
      scoreHistory:     this.mockScoreHistory(),
      recommendations:  this.mockRecommendations(),
    };
  }

  private mockSummary(): DashboardSummary {
    return {
      problemsSolved:  47,
      avgScore:        68,
      streak:          12,
      totalAttempts:   83,
      acceptanceRate:  57,
      hintsUsedToday:  3,
      hintsLimit:      5,
    };
  }

  private mockTopicStats(): TopicStat[] {
    return [
      { topic: 'Arrays',             percentage: 85, trend: 'up'   },
      { topic: 'Recursion',          percentage: 72, trend: 'up'   },
      { topic: 'Dyn. Programming',   percentage: 54, trend: 'flat' },
      { topic: 'Graphs',             percentage: 38, trend: 'down' },
      { topic: 'Greedy',             percentage: 61, trend: 'up'   },
      { topic: 'Sorting',            percentage: 79, trend: 'up'   },
      { topic: 'Binary Search',      percentage: 66, trend: 'flat' },
      { topic: 'Trees',              percentage: 43, trend: 'down' },
    ];
  }

  private mockWeeklyActivity(): WeeklyActivity[] {
    // Last 28 days
    const today = new Date();
    return Array.from({ length: 28 }, (_, i) => {
      const d = new Date(today);
      d.setDate(today.getDate() - (27 - i));
      const solved   = Math.round(Math.random() * 4);
      const extra    = Math.round(Math.random() * 2);
      return {
        date:      d.toISOString().slice(0, 10),
        solved,
        attempted: solved + extra,
      };
    });
  }

  private mockScoreHistory(): ScorePoint[] {
    // Last 30 days — generally trending upward with noise
    const today = new Date();
    let score = 45;
    return Array.from({ length: 30 }, (_, i) => {
      const d = new Date(today);
      d.setDate(today.getDate() - (29 - i));
      score = Math.min(100, Math.max(20, score + (Math.random() * 10 - 3)));
      return {
        date:  d.toISOString().slice(0, 10),
        score: Math.round(score),
      };
    });
  }

  private mockRecommendations(): RecommendedProblem[] {
    return [
      {
        id:                 '2',
        title:              'Number of Islands',
        difficulty:         'hard',
        topic:              'graphs',
        topicLabel:         'Graphs · BFS',
        reason:             'Weak area: Graphs (38%)',
        estimatedMinutes:   35,
      },
      {
        id:                 '9',
        title:              'Lowest Common Ancestor',
        difficulty:         'hard',
        topic:              'trees',
        topicLabel:         'Trees · DFS',
        reason:             'Weak area: Trees (43%)',
        estimatedMinutes:   30,
      },
      {
        id:                 '1',
        title:              'Coin Change II',
        difficulty:         'medium',
        topic:              'dynamic-programming',
        topicLabel:         'Dynamic Programming',
        reason:             'Needs improvement: Dyn. Programming (54%)',
        estimatedMinutes:   25,
      },
      {
        id:                 '7',
        title:              'Course Schedule',
        difficulty:         'medium',
        topic:              'graphs',
        topicLabel:         'Graphs · Topological Sort',
        reason:             'Weak area: Graphs (38%)',
        estimatedMinutes:   20,
      },
      {
        id:                 '8',
        title:              'Maximum Subarray',
        difficulty:         'medium',
        topic:              'greedy',
        topicLabel:         'Greedy · Kadane',
        reason:             'Steady progress — push further: Greedy (61%)',
        estimatedMinutes:   15,
      },
    ];
  }
}
