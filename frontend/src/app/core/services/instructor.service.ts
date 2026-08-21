import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import {
  InstructorStudentDetail,
  InstructorStudentSummary,
  IntegrityFlag,
} from '../models/instructor.model';
import { environment } from '../../../environments/environment';

interface ApiEnvelope<T> {
  data: T;
  success: boolean;
}

export interface BackendInstructorOverview {
  instructorId: string;
  fullName: string;
  email: string;
  totalProblemsAuthored: number;
  totalStudentsReached: number;
  totalSubmissionsReceived: number;
  overallAcceptRatePercent: number;
  totalAssignedProblems: number;
  integrityFlagsCount: number;
  dailyActivity: {
    date: string;
    dayLabel: string;
    submissions: number;
  }[];
  topicPerformance: {
    topic: string;
    percentage: number;
  }[];
  students: {
    studentId: string;
    fullName: string;
    email: string;
    totalSubmissions: number;
    acceptedSubmissions: number;
    successRatePercent: number;
    problemsSolved: number;
    lastActivityAt?: string;
  }[];
}

export interface BackendIntegrityFlag {
  feedbackId: string;
  submissionId: string;
  studentName: string;
  studentEmail: string;
  problemTitle: string;
  problemId: string;
  confidence: number;
  indicators: string;
  flaggedAt: string;
}

@Injectable({ providedIn: 'root' })
export class InstructorService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/analytics`;

  private cachedStudents: InstructorStudentSummary[] = [];
  private cachedFlags: IntegrityFlag[] = [];

  private headers(): HttpHeaders {
    const token = localStorage.getItem('codify_token') ?? '';
    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    });
  }

  getStudents(): InstructorStudentSummary[] {
    return this.cachedStudents;
  }

  getOverview$(): Observable<BackendInstructorOverview | null> {
    return this.http
      .get<ApiEnvelope<BackendInstructorOverview>>(`${this.baseUrl}/overview`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => r.data),
        tap(data => {
          if (data && data.students) {
            this.cachedStudents = data.students.map(s => ({
              id: s.studentId,
              name: s.fullName,
              initials: (s.fullName || 'ST').split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase(),
              avgScore: Math.round(s.successRatePercent),
              problemsSolved: s.problemsSolved,
              integrityStatus: 'clean',
            }));
          }
        }),
        catchError(() => of(null))
      );
  }

  getIntegrityFlags$(): Observable<IntegrityFlag[]> {
    return this.http
      .get<ApiEnvelope<BackendIntegrityFlag[]>>(`${this.baseUrl}/integrity-flags`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => (r.data || []).map(f => ({
          id: f.feedbackId,
          studentId: f.studentEmail,
          studentName: f.studentName,
          severity: (f.confidence >= 0.8 ? 'high' : f.confidence >= 0.5 ? 'medium' : 'low') as 'high' | 'medium' | 'low',
          reason: f.indicators || `AI-generated pattern (confidence: ${Math.round(f.confidence * 100)}%)`,
          detectedAt: f.flaggedAt,
        }))),
        tap(flags => {
          this.cachedFlags = flags;
        }),
        catchError(() => of([]))
      );
  }

  searchStudents(query: string): InstructorStudentSummary[] {
    const q = query.toLowerCase().trim();
    if (!q) return [];
    return this.cachedStudents.filter(s => s.name.toLowerCase().includes(q));
  }

  getStudentById$(id: string): Observable<InstructorStudentDetail | null> {
    return this.http
      .get<ApiEnvelope<any>>(`${this.baseUrl}/profile/${id}`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => {
          const p = r.data;
          if (!p) return null;
          return {
            id: id,
            name: p.user?.name || 'Student',
            initials: p.user?.avatarInitials || 'ST',
            avgScore: Math.round(p.successRate || 0),
            problemsSolved: p.totalSolved || 0,
            integrityStatus: 'clean' as const,
            streak: p.streak?.currentStreak || 0,
            hintsUsed: 0,
            lastActiveAt: p.recentAccepted?.[0]?.submittedAt || new Date().toISOString(),
            topicMastery: (p.topicStats || []).map((t: any) => ({
              topic: t.topicName,
              percentage: t.strengthScore,
            })),
            recentSubmissions: (p.recentAccepted || []).map((s: any) => ({
              problemTitle: s.problemTitle,
              status: s.status,
              submittedAt: s.submittedAt,
            })),
          };
        }),
        catchError(() => of(null))
      );
  }

  getStudentById(id: string): InstructorStudentDetail | undefined {
    const summary = this.cachedStudents.find(s => s.id === id);
    if (summary) {
      return {
        ...summary,
        streak: 0,
        hintsUsed: 0,
        lastActiveAt: new Date().toISOString(),
        topicMastery: [],
        recentSubmissions: [],
      };
    }
    return undefined;
  }

  getIntegrityFlags(): IntegrityFlag[] {
    return this.cachedFlags;
  }
}
