import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { ServiceError } from '../models/submission.model';

export interface AdminStats {
  totalUsers: number;
  activeStudents: number;
  pendingInstructors: number;
  activeInstructors: number;
  totalProblems: number;
  totalSubmissions: number;
  passRatePercent: number;
  totalContests: number;
  aiFlagsCount: number;
}

export interface AdminUserRow {
  id: string;
  name: string;
  initials: string;
  email: string;
  role: 'student' | 'instructor' | 'admin';
  status: 'active' | 'pending' | 'suspended' | 'rejected';
  registeredAt: string;
  lastActiveAt: string | null;
  problemsSolved?: number;
  totalSubmissions?: number;
  organization?: string;
}

export interface AdminUserSubmission {
  id: string;
  problemId: string;
  problemTitle: string;
  difficulty: string;
  language: string;
  status: string;
  executionTimeMs?: number;
  submittedAt: string;
}

export interface AdminUserDetail {
  id: string;
  name: string;
  email: string;
  role: 'student' | 'instructor' | 'admin';
  status: 'active' | 'pending' | 'suspended' | 'rejected';
  organization?: string;
  bio?: string;
  rating: number;
  solvedProblems: number;
  totalSubmissions: number;
  successRate: number;
  createdAt: string;
  lastLoginAt: string | null;
  recentSubmissions: AdminUserSubmission[];
}

export interface PendingInstructor {
  userId: string;
  fullName: string;
  email: string;
  organization?: string;
  registeredAt: string;
}

export interface ApproveInstructorResult {
  userId: string;
  fullName: string;
  email: string;
  approvedAt: string;
}

export interface RagReindexResult {
  conceptsIngested: number;
  problemsIngested: number;
  totalIngested: number;
}

interface ApiEnvelope<T> {
  data: T;
  success: boolean;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5237/api/admin';

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
        code: 'http',
        status: err.status,
        message: err.error?.message ?? err.message ?? 'Admin request failed',
      };
      return throwError(() => serviceErr);
    }
    const serviceErr: ServiceError = {
      code: 'unknown',
      message: err instanceof Error ? err.message : 'Unknown error',
    };
    return throwError(() => serviceErr);
  }

  getStats(): Observable<AdminStats> {
    return this.http
      .get<ApiEnvelope<AdminStats>>(`${this.baseUrl}/stats`, { headers: this.headers() })
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }

  getUsers(): Observable<AdminUserRow[]> {
    return this.http
      .get<ApiEnvelope<any[]>>(`${this.baseUrl}/users`, { headers: this.headers() })
      .pipe(
        map(r =>
          (r.data || []).map(u => {
            const name = u.name || 'User';
            const initials = name
              .split(' ')
              .map((w: string) => w[0])
              .join('')
              .slice(0, 2)
              .toUpperCase();
            return {
              id: u.id,
              name: u.name,
              initials: initials || 'US',
              email: u.email,
              role: u.role as 'student' | 'instructor' | 'admin',
              status: u.status as any,
              registeredAt: u.createdAt,
              lastActiveAt: u.lastLoginAt,
              problemsSolved: u.solvedProblems,
              totalSubmissions: u.totalSubmissions,
              organization: u.organization,
            };
          })
        ),
        catchError(err => this.handleError(err))
      );
  }

  getUserById(id: string): Observable<AdminUserDetail> {
    return this.http
      .get<ApiEnvelope<AdminUserDetail>>(`${this.baseUrl}/users/${id}`, { headers: this.headers() })
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }

  updateUserStatus(id: string, status: string): Observable<any> {
    const statusNum = status === 'active' ? 1 : status === 'pending' ? 0 : 2;
    return this.http
      .patch<ApiEnvelope<any>>(
        `${this.baseUrl}/users/${id}/status`,
        { status: statusNum },
        { headers: this.headers()}
      )
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }

  getPendingInstructors(): Observable<PendingInstructor[]> {
    return this.http
      .get<ApiEnvelope<PendingInstructor[]>>(`${this.baseUrl}/instructors/pending`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }

  approveInstructor(instructorId: string): Observable<ApproveInstructorResult> {
    return this.http
      .patch<ApiEnvelope<ApproveInstructorResult>>(
        `${this.baseUrl}/instructors/${instructorId}/approve`,
        {},
        { headers: this.headers() }
      )
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }

  reindexRag(): Observable<RagReindexResult> {
    return this.http
      .post<ApiEnvelope<RagReindexResult>>(
        `${this.baseUrl}/rag/reindex`,
        {},
        { headers: this.headers() }
      )
      .pipe(
        map(r => r.data),
        catchError(err => this.handleError(err))
      );
  }
}
