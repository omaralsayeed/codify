import { Injectable } from '@angular/core';
import { Contest, ContestResult, ContestStatus, CreateContestPayload } from '../models/contest.model';

@Injectable({ providedIn: 'root' })
export class ContestService {

  // Mutable copy so createContest() can push into it
  private readonly contests: Contest[] = [...MOCK_CONTESTS];
  private readonly results:  ContestResult[] = [...MOCK_RESULTS];

  // ── Queries ──────────────────────────────────────────────────────────────

  getContests(): Contest[] {
    return this.contests;
  }

  getContestById(id: string): Contest | undefined {
    return this.contests.find(c => c.id === id);
  }

  /** Returns results for a contest sorted by rank (ascending). */
  getContestResults(contestId: string): ContestResult[] {
    return this.results
      .filter(r => r.contestId === contestId)
      .sort((a, b) => a.rank - b.rank);
  }

  /** Returns all results for a student across every contest, oldest first. */
  getStudentContestHistory(studentId: string): ContestResult[] {
    return this.results
      .filter(r => r.studentId === studentId)
      .sort((a, b) => new Date(a.finishedAt).getTime() - new Date(b.finishedAt).getTime());
  }

  // ── Mutations ─────────────────────────────────────────────────────────────

  /**
   * Creates a new contest and pushes it into the mock array.
   * Validates: at least 1 problem, at least 1 student, end after start.
   */
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

    const now   = new Date().toISOString();
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

// ── Mock data ─────────────────────────────────────────────────────────────────
// Student IDs mirror instructor.service.ts mock: s1=Karim, s2=Layla, s3=Omar, s4=Sara
// Problem IDs mirror problem.service.ts mock: 1-9

const MOCK_CONTESTS: Contest[] = [
  {
    id: 'c1',
    title: 'Arrays & Hashing Sprint',
    description: 'Quick contest covering array manipulation and hash map problems.',
    createdByInstructorId: 'instructor-1',
    problemIds: ['5', '6', '8'],
    assignedStudentIds: ['s1', 's2', 's3', 's4'],
    startAt: '2026-07-10T09:00:00Z',
    endAt:   '2026-07-10T11:00:00Z',
    status:  'ended',
  },
  {
    id: 'c2',
    title: 'Graph Theory Challenge',
    description: 'BFS, DFS, and topological sort problems for advanced students.',
    createdByInstructorId: 'instructor-1',
    problemIds: ['2', '7', '9'],
    assignedStudentIds: ['s1', 's2', 's3'],
    startAt: '2026-07-17T14:00:00Z',
    endAt:   '2026-07-17T16:30:00Z',
    status:  'ended',
  },
  {
    id: 'c3',
    title: 'Dynamic Programming Intro',
    description: 'Introductory DP problems — memoization and tabulation.',
    createdByInstructorId: 'instructor-1',
    problemIds: ['1', '3'],
    assignedStudentIds: ['s2', 's4'],
    startAt: '2026-07-24T10:00:00Z',
    endAt:   '2026-07-24T12:00:00Z',
    status:  'upcoming',
  },
  {
    id: 'c4',
    title: 'Mixed Concepts Round',
    description: 'Covers recursion, greedy, and sorting under time pressure.',
    createdByInstructorId: 'instructor-1',
    problemIds: ['3', '4', '8'],
    assignedStudentIds: ['s1', 's3', 's4'],
    startAt: '2026-07-28T09:00:00Z',
    endAt:   '2026-07-28T11:30:00Z',
    status:  'upcoming',
  },
  {
    id: 'c5',
    title: 'Trees & Recursion Deep Dive',
    description: 'Tree traversals and recursive algorithms.',
    createdByInstructorId: 'instructor-1',
    problemIds: ['3', '9'],
    assignedStudentIds: ['s1', 's2'],
    startAt: '2026-07-23T08:00:00Z',
    endAt:   '2026-07-23T18:00:00Z',
    status:  'live',
  },
  {
    id: 'c6',
    title: 'Sorting Algorithms Warmup',
    description: 'Light contest to warm up on sorting and interval problems.',
    createdByInstructorId: 'instructor-1',
    problemIds: ['4', '6'],
    assignedStudentIds: ['s2', 's3', 's4'],
    startAt: '2026-07-30T09:00:00Z',
    endAt:   '2026-07-30T10:30:00Z',
    status:  'draft',
  },
];

// Results only exist for ended contests (c1, c2)
const MOCK_RESULTS: ContestResult[] = [
  // ── c1: Arrays & Hashing Sprint (4 students, 3 problems) ─────────────────
  {
    contestId: 'c1', studentId: 's1', studentName: 'Karim Ahmed',
    rank: 1, score: 95, problemsSolved: 3, totalProblems: 3,
    accuracy: 96, finishedAt: '2026-07-10T10:22:00Z',
  },
  {
    contestId: 'c1', studentId: 's2', studentName: 'Layla Mostafa',
    rank: 2, score: 88, problemsSolved: 3, totalProblems: 3,
    accuracy: 91, finishedAt: '2026-07-10T10:35:00Z',
  },
  {
    contestId: 'c1', studentId: 's4', studentName: 'Sara Mahmoud',
    rank: 3, score: 74, problemsSolved: 2, totalProblems: 3,
    accuracy: 78, finishedAt: '2026-07-10T10:51:00Z',
  },
  {
    contestId: 'c1', studentId: 's3', studentName: 'Omar Sherif',
    rank: 4, score: 60, problemsSolved: 2, totalProblems: 3,
    accuracy: 65, finishedAt: '2026-07-10T10:58:00Z',
  },

  // ── c2: Graph Theory Challenge (3 students, 3 problems) ──────────────────
  {
    contestId: 'c2', studentId: 's2', studentName: 'Layla Mostafa',
    rank: 1, score: 91, problemsSolved: 3, totalProblems: 3,
    accuracy: 94, finishedAt: '2026-07-17T15:40:00Z',
  },
  {
    contestId: 'c2', studentId: 's1', studentName: 'Karim Ahmed',
    rank: 2, score: 85, problemsSolved: 3, totalProblems: 3,
    accuracy: 88, finishedAt: '2026-07-17T15:52:00Z',
  },
  {
    contestId: 'c2', studentId: 's3', studentName: 'Omar Sherif',
    rank: 3, score: 52, problemsSolved: 1, totalProblems: 3,
    accuracy: 58, finishedAt: '2026-07-17T16:18:00Z',
  },
];
