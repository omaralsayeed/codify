import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { mapRole, mapDifficulty } from '../utils/enum-mappers';
import { environment } from '../../../environments/environment';

// ── Shared types (exported for use in admin components) ───────────────────────

export interface AdminStats {
  totalUsers: number;
  totalStudents: number;
  totalInstructors: number;
  activeInstructors: number;
  pendingInstructors: number;
  totalProblems: number;
  totalSubmissions: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  submissionsToday: number;
}

export interface AdminUserRow {
  id: string;
  name: string;
  initials: string;
  email: string;
  role: 'student' | 'instructor' | 'admin';
  status: 'active' | 'pending';
  registeredAt: string;
  lastActiveAt: string | null;
  problemsSolved: number | null;
  organization: string | null;
}

export interface AdminUserDetail extends AdminUserRow {
  avgScore: number | null;
  streak: number | null;
  totalSubmissions: number;
  recentSubmissions: {
    problemTitle: string;
    status: string;
    submittedAt: string;
  }[];
}

export interface AdminProblemRow {
  id: string;
  title: string;
  difficulty: 'easy' | 'medium' | 'hard';
  tags: string[];
  solvedCount: number;
  totalSubmissions: number;
  isActive: boolean;
  createdAt: string;
}

export interface AdminUsersFilters {
  search?: string;
  role?: 'student' | 'instructor';
  status?: 'active' | 'pending';
  sortBy?: 'name' | 'registeredAt' | 'lastActiveAt';
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface AdminProblemsFilters {
  search?: string;
  difficulty?: number;
  tag?: string;
  isActive?: boolean;
  sortBy?: 'title' | 'difficulty' | 'solvedCount' | 'createdAt';
  sortDir?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}

export interface CreateProblemBody {
  title: string;
  difficulty: number;
  tags: string[];
  statement: string;
  constraints: string;
  sampleTestCases: { input: string; expectedOutput: string; }[];
  isActive: boolean;
  timeLimitMs: number;
  memoryLimitMb: number;
}

// Partial update — all fields optional
export type UpdateProblemBody = Partial<CreateProblemBody>;

// ── Backend raw response shapes (before mapping) ──────────────────────────────

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
}

interface RawUserRow {
  id: string;
  name: string;
  initials: string;
  email: string;
  role: number | string;
  status: string;
  registeredAt: string;
  lastActiveAt: string | null;
  problemsSolved: number | null;
  organization: string | null;
}

interface RawUserDetail extends RawUserRow {
  avgScore: number | null;
  streak: number | null;
  totalSubmissions: number;
  recentSubmissions: {
    problemTitle: string;
    status: string;
    submittedAt: string;
  }[];
}

interface RawProblemRow {
  id: string;
  title: string;
  difficulty: number | string;
  tags: string[];
  solvedCount: number;
  totalSubmissions: number;
  isActive: boolean;
  createdAt: string;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http    = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  // ── Auth header ─────────────────────────────────────────────────────────────
  private headers(): HttpHeaders {
    const token = localStorage.getItem('codify_token') ?? '';
    return new HttpHeaders({ Authorization: `Bearer ${token}` });
  }

  // ── 1. Overview stats ──────────────────────────────────────────────────────
  getStats(): Observable<AdminStats> {
    return this.http
      .get<ApiEnvelope<AdminStats>>(`${this.baseUrl}/admin/stats`, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  // ── 2. Users list ──────────────────────────────────────────────────────────
  getUsers(filters: AdminUsersFilters = {}): Observable<{ users: AdminUserRow[]; total: number }> {
    let params = new HttpParams();
    if (filters.search)   params = params.set('search',   filters.search);
    if (filters.role)     params = params.set('role',     filters.role);
    if (filters.status)   params = params.set('status',   filters.status);
    if (filters.sortBy)   params = params.set('sortBy',   filters.sortBy);
    if (filters.sortDir)  params = params.set('sortDir',  filters.sortDir);
    if (filters.page)     params = params.set('page',     filters.page.toString());
    if (filters.pageSize) params = params.set('pageSize', filters.pageSize.toString());

    return this.http
      .get<ApiEnvelope<{ users: RawUserRow[]; total: number; page: number; pageSize: number }>>
        (`${this.baseUrl}/admin/users`, { headers: this.headers(), params })
      .pipe(
        map(r => ({
          users: r.data.users.map(u => this.mapUserRow(u)),
          total: r.data.total,
        }))
      );
  }

  // ── 3. User detail ─────────────────────────────────────────────────────────
  getUserById(id: string): Observable<AdminUserDetail> {
    return this.http
      .get<ApiEnvelope<RawUserDetail>>(`${this.baseUrl}/admin/users/${id}`, { headers: this.headers() })
      .pipe(map(r => this.mapUserDetail(r.data)));
  }

  // ── 4. User status toggle ──────────────────────────────────────────────────
  updateUserStatus(id: string, status: 'active' | 'pending'): Observable<AdminUserDetail> {
    return this.http
      .patch<ApiEnvelope<RawUserDetail>>
        (`${this.baseUrl}/admin/users/${id}/status`, { status }, { headers: this.headers() })
      .pipe(map(r => this.mapUserDetail(r.data)));
  }

  // ── 5. Problems list ───────────────────────────────────────────────────────
  getProblems(filters: AdminProblemsFilters = {}): Observable<{ problems: AdminProblemRow[]; total: number }> {
    let params = new HttpParams();
    if (filters.search     !== undefined) params = params.set('search',     filters.search);
    if (filters.difficulty !== undefined) params = params.set('difficulty', filters.difficulty.toString());
    if (filters.tag        !== undefined) params = params.set('tag',        filters.tag);
    if (filters.isActive   !== undefined) params = params.set('isActive',   filters.isActive.toString());
    if (filters.sortBy)    params = params.set('sortBy',   filters.sortBy);
    if (filters.sortDir)   params = params.set('sortDir',  filters.sortDir);
    if (filters.page)      params = params.set('page',     filters.page.toString());
    if (filters.pageSize)  params = params.set('pageSize', filters.pageSize.toString());

    return this.http
      .get<ApiEnvelope<{ problems: RawProblemRow[]; total: number }>>
        (`${this.baseUrl}/admin/problems`, { headers: this.headers(), params })
      .pipe(
        map(r => ({
          problems: r.data.problems.map(p => this.mapProblemRow(p)),
          total: r.data.total,
        }))
      );
  }

  // ── 6. Create problem ──────────────────────────────────────────────────────
  createProblem(body: CreateProblemBody): Observable<any> {
    return this.http
      .post<ApiEnvelope<any>>(`${this.baseUrl}/problems`, body, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  // ── 7. Update problem ──────────────────────────────────────────────────────
  updateProblem(id: string, body: UpdateProblemBody): Observable<any> {
    return this.http
      .patch<ApiEnvelope<any>>(`${this.baseUrl}/problems/${id}`, body, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  // ── 8. Problem status toggle ───────────────────────────────────────────────
  updateProblemStatus(id: string, isActive: boolean): Observable<{ id: string; isActive: boolean }> {
    return this.http
      .patch<ApiEnvelope<{ id: string; isActive: boolean }>>
        (`${this.baseUrl}/problems/${id}/status`, { isActive }, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  // ── 9. Delete problem (soft delete) ───────────────────────────────────────
  deleteProblem(id: string): Observable<{ id: string; deleted: boolean }> {
    return this.http
      .delete<ApiEnvelope<{ id: string; deleted: boolean }>>
        (`${this.baseUrl}/problems/${id}`, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  // ── Get single problem (for edit form pre-fill) ────────────────────────────
  // Uses the existing public endpoint — not admin-only
  getProblemById(id: string): Observable<any> {
    return this.http
      .get<ApiEnvelope<any>>(`${this.baseUrl}/problems/${id}`, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  // ── Private mappers ────────────────────────────────────────────────────────

  private mapUserRow(raw: RawUserRow): AdminUserRow {
    return {
      id:             raw.id,
      name:           raw.name,
      initials:       raw.initials,
      email:          raw.email,
      role:           mapRole(raw.role),
      status:         raw.status as 'active' | 'pending',
      registeredAt:   raw.registeredAt,
      lastActiveAt:   raw.lastActiveAt,
      problemsSolved: raw.problemsSolved,
      organization:   raw.organization,
    };
  }

  private mapUserDetail(raw: RawUserDetail): AdminUserDetail {
    return {
      ...this.mapUserRow(raw),
      avgScore:          raw.avgScore,
      streak:            raw.streak,
      totalSubmissions:  raw.totalSubmissions,
      recentSubmissions: raw.recentSubmissions ?? [],
    };
  }

  private mapProblemRow(raw: RawProblemRow): AdminProblemRow {
    return {
      id:               raw.id,
      title:            raw.title,
      difficulty:       mapDifficulty(raw.difficulty),
      tags:             raw.tags ?? [],
      solvedCount:      raw.solvedCount,
      totalSubmissions: raw.totalSubmissions,
      isActive:         raw.isActive,
      createdAt:        raw.createdAt,
    };
  }
}
