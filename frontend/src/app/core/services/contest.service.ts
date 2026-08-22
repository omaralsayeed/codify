import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError, tap } from 'rxjs/operators';
import {
  Contest,
  ContestResult,
  ContestStatus,
  CreateContestPayload,
  StudentContestsOverview,
} from '../models/contest.model';
import { environment } from '../../../environments/environment';

interface ApiEnvelope<T> {
  data: T;
  success: boolean;
}

function normalizeStatus(status: any): ContestStatus {
  if (status === 0 || status === '0' || status === 'Draft' || status === 'draft') return 'draft';
  if (status === 1 || status === '1' || status === 'Upcoming' || status === 'upcoming') return 'upcoming';
  if (status === 2 || status === '2' || status === 'Live' || status === 'live') return 'live';
  if (status === 3 || status === '3' || status === 'Ended' || status === 'ended') return 'ended';
  return 'draft';
}

function normalizeContest(c: any): Contest {
  return {
    ...c,
    status: normalizeStatus(c.status),
    problemIds: c.problemIds || (c.problems ? c.problems.map((p: any) => p.id) : []),
    assignedStudentIds: c.assignedStudentIds || [],
    studentEmails: c.studentEmails || [],
    participants: (c.participants || []).map((p: any) => ({
      studentId: p.studentId,
      studentName: p.studentName,
      studentEmail: p.studentEmail,
      invitationStatus: (typeof p.invitationStatus === 'number'
        ? (p.invitationStatus === 1 ? 'accepted' : p.invitationStatus === 2 ? 'declined' : 'pending')
        : (p.invitationStatus || 'pending').toLowerCase()) as any,
      respondedAt: p.respondedAt,
      score: p.score ?? 0,
      problemsSolved: p.problemsSolved ?? 0,
      accuracy: p.accuracy ?? 0,
      rank: p.rank ?? 0,
    })),
    myInvitationStatus: c.myInvitationStatus
      ? (typeof c.myInvitationStatus === 'number'
        ? (c.myInvitationStatus === 1 ? 'accepted' : c.myInvitationStatus === 2 ? 'declined' : 'pending')
        : (c.myInvitationStatus || 'pending').toLowerCase()) as any
      : undefined,
  };
}

@Injectable({ providedIn: 'root' })
export class ContestService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/contests`;

  private contests: Contest[] = [];
  private results: ContestResult[] = [];

  private headers(): HttpHeaders {
    const token = localStorage.getItem('codify_token') ?? '';
    return new HttpHeaders({
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    });
  }

  // ── Queries ──────────────────────────────────────────────────────────────

  getContests(): Contest[] {
    return this.contests;
  }

  getContests$(): Observable<Contest[]> {
    return this.http
      .get<ApiEnvelope<Contest[]>>(this.baseUrl, { headers: this.headers() })
      .pipe(
        map(r => (r.data || []).map(normalizeContest)),
        tap(contests => {
          this.contests = contests;
        }),
        catchError(() => of(this.contests))
      );
  }

  getMyContests$(): Observable<StudentContestsOverview> {
    return this.http
      .get<ApiEnvelope<StudentContestsOverview>>(`${this.baseUrl}/my-contests`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => {
          const data = r.data;
          const pending = (data?.pendingInvitations || []).map(normalizeContest);
          const live = (data?.liveContests || []).map(normalizeContest);
          const upcoming = (data?.upcomingContests || []).map(normalizeContest);
          const past = (data?.pastContests || []).map(p => ({
            ...p,
            problems: p.problems || [],
          }));
          return {
            hasActiveContestNotification: data?.hasActiveContestNotification || live.length > 0 || pending.length > 0,
            activeContestsCount: data?.activeContestsCount || live.length,
            pendingInvitations: pending,
            liveContests: live,
            upcomingContests: upcoming,
            pastContests: past,
          };
        }),
        catchError(() =>
          of({
            hasActiveContestNotification: false,
            activeContestsCount: 0,
            pendingInvitations: [],
            liveContests: [],
            upcomingContests: [],
            pastContests: [],
          })
        )
      );
  }

  getContestById(id: string): Contest | undefined {
    return this.contests.find(c => c.id === id);
  }

  getContestById$(id: string): Observable<Contest | undefined> {
    return this.http
      .get<ApiEnvelope<Contest>>(`${this.baseUrl}/${id}`, { headers: this.headers() })
      .pipe(
        map(r => (r.data ? normalizeContest(r.data) : undefined)),
        catchError(() => of(this.getContestById(id)))
      );
  }

  /** Returns results for a contest sorted by rank (ascending). */
  getContestResults(contestId: string): ContestResult[] {
    return this.results
      .filter(r => r.contestId === contestId)
      .sort((a, b) => a.rank - b.rank);
  }

  getContestResults$(contestId: string): Observable<ContestResult[]> {
    return this.http
      .get<ApiEnvelope<ContestResult[]>>(`${this.baseUrl}/${contestId}/results`, {
        headers: this.headers(),
      })
      .pipe(
        map(r => (r.data || []).sort((a, b) => a.rank - b.rank)),
        tap(res => {
          if (res && res.length > 0) {
            const others = this.results.filter(r => r.contestId !== contestId);
            this.results = [...res, ...others];
          }
        }),
        catchError(() => of(this.getContestResults(contestId)))
      );
  }

  /** Returns all results for a student across every contest, oldest first. */
  getStudentContestHistory(studentId: string): ContestResult[] {
    return this.results
      .filter(r => r.studentId === studentId)
      .sort((a, b) => new Date(a.finishedAt).getTime() - new Date(b.finishedAt).getTime());
  }

  getStudentContestHistory$(studentId: string): Observable<ContestResult[]> {
    return this.http
      .get<ApiEnvelope<ContestResult[]>>(`${this.baseUrl}/students/${studentId}/history`, {
        headers: this.headers(),
      })
      .pipe(
        map(r =>
          (r.data || []).sort(
            (a, b) => new Date(a.finishedAt).getTime() - new Date(b.finishedAt).getTime()
          )
        ),
        catchError(() => of(this.getStudentContestHistory(studentId)))
      );
  }

  // ── Mutations ─────────────────────────────────────────────────────────────

  createContest$(payload: CreateContestPayload): Observable<Contest> {
    return this.http
      .post<ApiEnvelope<Contest>>(this.baseUrl, payload, { headers: this.headers() })
      .pipe(
        map(r => normalizeContest(r.data)),
        tap(created => {
          this.contests.unshift(created);
        })
      );
  }

  respondToInvitation$(contestId: string, accept: boolean): Observable<{ success: boolean; message: string }> {
    return this.http
      .post<ApiEnvelope<{ contestId: string; accepted: boolean; message: string }>>(
        `${this.baseUrl}/${contestId}/invitations/respond`,
        { accept },
        { headers: this.headers() }
      )
      .pipe(
        map(r => ({
          success: r.success,
          message: r.data?.message || (accept ? 'Invitation accepted' : 'Invitation declined')
        }))
      );
  }

  searchStudents$(query: string = ''): Observable<{ id: string; name: string; email: string }[]> {
    const url = query
      ? `${this.baseUrl}/students/search?query=${encodeURIComponent(query)}`
      : `${this.baseUrl}/students/search`;

    return this.http
      .get<ApiEnvelope<{ id: string; name: string; email: string }[]>>(url, {
        headers: this.headers()
      })
      .pipe(
        map(r => r.data || []),
        catchError(() => of([]))
      );
  }
}

