import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Problem } from '../models/problem.model';
import { mapDifficulty, difficultyToNumber, Difficulty } from '../utils/enum-mappers';
import { environment } from '../../../environments/environment';

// Backend API response interfaces
interface ProblemListItem {
  id: string;
  title: string;
  difficulty: number;
  tags: string[];
  isActive: boolean;
}

interface ProblemDetailResponse {
  id: string;
  title: string;
  slug: string;
  statement: string;
  difficulty: number;
  constraints: string;
  languageSupport: string[];
  tags: string[];
  sampleTestCases: {
    input: string;
    expectedOutput: string;
  }[];
  isActive: boolean;
  isPublic: boolean;
  timeLimitMs: number;
  memoryLimitMb: number;
  acceptedSubmissionsCount: number;
  totalSubmissionsCount: number;
}

interface ProblemListResponse {
  items: ProblemListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface ApiEnvelope<T> {
  success: boolean;
  data: T;
  message: string | null;
  errorCode: string | null;
  details: string | null;
}

@Injectable({ providedIn: 'root' })
export class ProblemService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl;

  /**
   * Get authorization headers with JWT token
   */
  private headers(): HttpHeaders {
    const token = localStorage.getItem('codify_token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  /**
   * Get all problems with optional filters
   */
  getAll(filters?: {
    difficulty?: Difficulty;
    tag?: string;
    search?: string;
    page?: number;
    pageSize?: number;
  }): Observable<Problem[]> {
    let params = new HttpParams();
    
    if (filters?.difficulty) {
      params = params.set('difficulty', difficultyToNumber(filters.difficulty).toString());
    }
    if (filters?.tag) {
      params = params.set('tag', filters.tag);
    }
    if (filters?.search) {
      params = params.set('search', filters.search);
    }
    if (filters?.page) {
      params = params.set('page', filters.page.toString());
    }
    if (filters?.pageSize) {
      params = params.set('pageSize', filters.pageSize.toString());
    }

    return this.http
      .get<ApiEnvelope<ProblemListResponse>>(`${this.baseUrl}/problems`, {
        headers: this.headers(),
        params
      })
      .pipe(
        map(response => response.data.items),
        map(items => items.map(item => this.mapProblemSummary(item)))
      );
  }

  /**
   * Get a single problem by ID with full details
   */
  getById(id: string): Observable<any> {
    return this.http
      .get<ApiEnvelope<ProblemDetailResponse>>(`${this.baseUrl}/problems/${id}`, {
        headers: this.headers()
      })
      .pipe(
        map(response => response.data),
        map(detail => this.mapProblemDetail(detail))
      );
  }

  /**
   * Get recommended problems (still using mock until backend ready)
   */
  getRecommended(): Observable<Problem[]> {
    // Backend endpoint not ready yet - return first 3 from getAll
    return this.getAll().pipe(
      map(problems => problems.slice(0, 3))
    );
  }

  /**
   * Synchronous stub for home page preview (mocked component)
   */
  getRecommendedSync(): Problem[] {
    return this.mockProblems.slice(0, 3);
  }

  /**
   * Search problems by title or topic (now uses backend filtering)
   */
  search(query: string): Observable<Problem[]> {
    return this.getAll({ search: query });
  }

  create(payload: any): Observable<any> {
    return this.http
      .post<ApiEnvelope<any>>(`${this.baseUrl}/problems`, payload, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  update(id: string, payload: any): Observable<any> {
    return this.http
      .put<ApiEnvelope<any>>(`${this.baseUrl}/problems/${id}`, payload, { headers: this.headers() })
      .pipe(map(r => r.data));
  }

  /**
   * Map backend problem list item to frontend Problem model
   */
  private mapProblemSummary(raw: ProblemListItem): Problem {
    const topicSlug = raw.tags?.[0]?.toLowerCase().replace(/\s+/g, '-') ?? 'arrays';
    return {
      id: raw.id,
      title: raw.title,
      difficulty: mapDifficulty(raw.difficulty),
      topic: topicSlug as any, // Backend tags may not match our Topic type exactly
      topicLabel: raw.tags?.join(' · ') ?? '',
      solvedCount: 0 // Not available in list response
    };
  }

  /**
   * Map backend problem detail to frontend format
   */
  private mapProblemDetail(raw: ProblemDetailResponse): any {
    const topicSlug = raw.tags?.[0]?.toLowerCase().replace(/\s+/g, '-') ?? 'arrays';
    return {
      id: raw.id,
      title: raw.title,
      difficulty: mapDifficulty(raw.difficulty),
      topic: topicSlug as any, // Backend tags may not match our Topic type exactly
      topicLabel: raw.tags?.join(' · ') ?? '',
      solvedCount: raw.acceptedSubmissionsCount,
      description: raw.statement,
      constraints: raw.constraints?.split('\n') ?? [],
      examples: raw.sampleTestCases?.map(tc => ({
        input: tc.input,
        output: tc.expectedOutput,
        explanation: '' // Not available yet
      })) ?? [],
      // Starter code still hardcoded - backend doesn't provide it yet
      starterCode: this.getHardcodedStarterCode(raw.id)
    };
  }

  /**
   * Get hardcoded starter code (temporary until backend provides it)
   */
  private getHardcodedStarterCode(problemId: string): any {
    // Default starter code for Two Sum pattern
    return {
      python: 'def twoSum(nums, target):\n    pass',
      csharp: 'public int[] TwoSum(int[] nums, int target) {\n    \n}',
      javascript: 'var twoSum = function(nums, target) {\n    \n};',
      java: 'public int[] twoSum(int[] nums, int target) {\n    \n}',
      cpp: 'vector<int> twoSum(vector<int>& nums, int target) {\n    \n}'
    };
  }

  // ── Synchronous mock methods (for instructor features — not wired to backend yet) ──

  /**
   * Synchronous stub used by instructor components (contests, etc.) that are
   * still fully mocked. Returns a static list so those components compile
   * without async changes. Remove once instructor endpoints are ready.
   */
  getAllSync(): Problem[] {
    return this.mockProblems;
  }

  /**
   * Synchronous search stub for mocked instructor components.
   */
  searchSync(query: string): Problem[] {
    const q = query.toLowerCase().trim();
    if (!q) return [];
    return this.mockProblems.filter(p =>
      p.title.toLowerCase().includes(q) ||
      p.topicLabel.toLowerCase().includes(q) ||
      p.topic.toLowerCase().includes(q)
    );
  }

  private readonly mockProblems: Problem[] = [
    { id: '1', title: 'Coin Change II', difficulty: 'medium', topic: 'dynamic-programming', topicLabel: 'Dynamic Programming', solvedCount: 13210 },
    { id: '2', title: 'Number of Islands', difficulty: 'hard', topic: 'graphs', topicLabel: 'Graphs · BFS', solvedCount: 9210 },
    { id: '3', title: 'Climbing Stairs', difficulty: 'easy', topic: 'recursion', topicLabel: 'Recursion · Memo', solvedCount: 22104 },
    { id: '4', title: 'Merge Intervals', difficulty: 'medium', topic: 'sorting', topicLabel: 'Sorting · Intervals', solvedCount: 16884 },
    { id: '5', title: 'Two Sum', difficulty: 'easy', topic: 'arrays', topicLabel: 'Arrays · Hash Map', solvedCount: 36045 },
    { id: '6', title: 'Binary Search', difficulty: 'easy', topic: 'binary-search', topicLabel: 'Binary Search', solvedCount: 28791 },
    { id: '7', title: 'Course Schedule', difficulty: 'medium', topic: 'graphs', topicLabel: 'Graphs · Topological Sort', solvedCount: 11762 },
    { id: '8', title: 'Maximum Subarray', difficulty: 'medium', topic: 'greedy', topicLabel: 'Greedy · Kadane', solvedCount: 19503 },
    { id: '9', title: 'Lowest Common Ancestor', difficulty: 'hard', topic: 'trees', topicLabel: 'Trees · DFS', solvedCount: 8540 }
  ];
}
