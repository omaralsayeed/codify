import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import {
  StudentDashboardData,
  DashboardSummary,
  TopicStat,
  WeeklyActivity,
  ScorePoint,
  RecommendedProblem,
  StudentAnalytics,
  PublicProfileData,
} from '../models/analytics.model';
import { ServiceError } from '../models/submission.model';
import { environment } from '../../../environments/environment';

/** Shape of every response envelope from the backend: { data: T } */
interface ApiEnvelope<T> { data: T; }

/** Backend StudentAnalyticsResponse shape */
interface BackendStudentAnalytics {
  userId: string;
  fullName: string;
  email: string;
  totalSolvedProblems: number;
  easySolved: number;
  mediumSolved: number;
  hardSolved: number;
  totalSubmissions: number;
  acceptedSubmissions: number;
  wrongAnswers: number;
  runtimeErrors: number;
  compileErrors: number;
  timeLimitExceeded: number;
  successRatePercent: number;
  averageExecutionTimeMs: number | null;
  averageAttemptsPerProblem: number;
  languageBreakdown: { language: string; submissions: number }[];
  strongTopics: string[];
  weakTopics: string[];
  lastSubmissionAt: string | null;
  memberSince: string;
  totalHintsUsed?: number;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly API  = environment.apiUrl;

  private headers(): HttpHeaders {
    const token = localStorage.getItem('codify_token') ?? '';
    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    });
  }

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
   * GET /api/analytics/me (or /api/analytics/profile)
   * Returns live student dashboard payload with real DB calculations.
   */
  getDashboard(): Observable<StudentDashboardData> {
    return this.http
      .get<ApiEnvelope<PublicProfileData>>(`${this.API}/analytics/profile`, { headers: this.headers() })
      .pipe(
        map(r => {
          const p = r.data;
          const topicStats: TopicStat[] = (p.topicStats || []).map(t => ({
            topic: t.topicName,
            percentage: t.strengthScore,
            trend: t.strengthScore >= 70 ? 'up' : t.strengthScore >= 40 ? 'flat' : 'down',
          }));

          const grid = p.activityGrid || [];
          const recentDays = grid.slice(-28);
          const weeklyActivity: WeeklyActivity[] = recentDays.map(d => ({
            date: d.date,
            solved: d.count,
            attempted: d.count,
          }));

          const scoreHistory: ScorePoint[] = (p.recentAccepted || []).map(s => ({
            date: s.submittedAt.slice(0, 10),
            score: p.successRate,
          }));

          return {
            summary: {
              problemsSolved: p.totalSolved,
              avgScore: Math.round(p.successRate),
              streak: p.streak?.currentStreak || 0,
              totalAttempts: p.totalAttempted,
              acceptanceRate: Math.round(p.successRate),
              hintsUsedToday: 0,
              hintsLimit: 5,
            },
            topicStats,
            weeklyActivity,
            scoreHistory,
            recommendations: [],
          };
        }),
        catchError(() =>
          of({
            summary: {
              problemsSolved: 0,
              avgScore: 0,
              streak: 0,
              totalAttempts: 0,
              acceptanceRate: 0,
              hintsUsedToday: 0,
              hintsLimit: 5,
            },
            topicStats: [],
            weeklyActivity: [],
            scoreHistory: [],
            recommendations: [],
          })
        )
      );
  }

  getSummary(): Observable<DashboardSummary> {
    return this.getDashboard().pipe(map(d => d.summary));
  }

  getTopicStats(): Observable<TopicStat[]> {
    return this.getDashboard().pipe(map(d => d.topicStats));
  }

  getWeeklyActivity(): Observable<WeeklyActivity[]> {
    return this.getDashboard().pipe(map(d => d.weeklyActivity));
  }

  getScoreHistory(): Observable<ScorePoint[]> {
    return this.getDashboard().pipe(map(d => d.scoreHistory));
  }

  getRecommendations(): Observable<RecommendedProblem[]> {
    return this.getDashboard().pipe(map(d => d.recommendations));
  }

  /**
   * GET /api/analytics/profile
   * Returns live StudentAnalytics payload for the progress page from database.
   */
  getStudentAnalytics(): Observable<StudentAnalytics> {
    return this.http
      .get<ApiEnvelope<PublicProfileData>>(`${this.API}/analytics/profile`, { headers: this.headers() })
      .pipe(
        map(r => {
          const p = r.data;
          const grid = p.activityGrid || [];
          const lastSeven = grid.slice(-7).map(d => ({
            date: d.date,
            submitted: d.count > 0,
          }));

          return {
            summary: {
              studentName: p.user?.name || 'Student',
              totalAttempted: p.totalAttempted,
              totalSolved: p.totalSolved,
              successRate: Math.round(p.successRate),
              streak: {
                currentStreak: p.streak?.currentStreak || 0,
                longestStreak: p.streak?.longestStreak || 0,
                lastSevenDays: lastSeven,
              },
            },
            topics: p.topicStats || [],
            difficultyBreakdown: p.difficultyBreakdown || { easy: 0, medium: 0, hard: 0 },
            successRateHistory: [],
            recentSubmissions: (p.recentAccepted || []).map(s => ({
              submissionId: s.submissionId,
              problemId: s.problemId,
              problemTitle: s.problemTitle,
              difficulty: s.difficulty as any,
              status: s.status as any,
              language: s.language,
              submittedAt: s.submittedAt,
            })),
            recommendations: [],
            hintUsage: {
              totalHintsUsed: 0,
              averageHintsPerProblem: 0,
              solvedWithZeroHints: p.totalSolved,
              solvedUsingAllHints: 0,
            },
          };
        }),
        catchError(err => this.handleError(err))
      );
  }

  /**
   * GET /api/analytics/profile/:username
   * Returns public profile data directly from database.
   */
  getPublicProfile(username: string): Observable<PublicProfileData> {
    const slug = encodeURIComponent(username.trim());
    return this.http
      .get<ApiEnvelope<PublicProfileData>>(`${this.API}/analytics/profile/${slug}`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }
}
