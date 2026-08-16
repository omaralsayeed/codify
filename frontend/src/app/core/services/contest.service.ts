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
  };
}

@Injectable({ providedIn: 'root' })
export class ContestService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5237/api/contests';

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
          const live = (data?.liveContests || []).map(normalizeContest);
          const upcoming = (data?.upcomingContests || []).map(normalizeContest);
          const past = (data?.pastContests || []).map(p => ({
            ...p,
            problems: p.problems || [],
          }));
          return {
            hasActiveContestNotification: data?.hasActiveContestNotification || live.length > 0,
            activeContestsCount: data?.activeContestsCount || live.length,
            liveContests: live,
            upcomingContests: upcoming,
            pastContests: past,
          };
        }),
        catchError(() =>
          of({
            hasActiveContestNotification: false,
            activeContestsCount: 0,
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

  createContest(payload: CreateContestPayload): Contest {
    if (payload.problemIds.length === 0) {
      throw new Error('A contest must include at least one problem.');
    }
    if (payload.assignedStudentIds.length === 0) {
      throw new Error('A contest must be assigned to at least one student.');
    }
    if (new Date(payload.endAt) <= new Date(payload.startAt)) {
      throw new Error('End date must be after start date.');
    }

    const start = new Date(payload.startAt);
    const end   = new Date(payload.endAt);
    const nowDate = new Date();

    let status: ContestStatus;
    if (nowDate < start)      status = 'upcoming';
    else if (nowDate > end)   status = 'ended';
    else                      status = 'live';

    const created: Contest = {
      id: `c${Date.now()}`,
      title: payload.title.trim(),
      description: payload.description.trim(),
      createdByInstructorId: 'instructor-1',
      problemIds: payload.problemIds,
      assignedStudentIds: payload.assignedStudentIds,
      startAt: payload.startAt,
      endAt: payload.endAt,
      status,
    };

    this.contests.unshift(created);
    return created;
  }
}
