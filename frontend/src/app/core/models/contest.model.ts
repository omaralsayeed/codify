/**
 * contest.model.ts
 *
 * Data model for the instructor-only Contest feature.
 * Approved in PROJECT_CONTEXT.md §5 boundary change (2026-07-23).
 *
 * Ranking is by score only. No time-pressure mechanics.
 * No student-facing UI — instructor dashboard only.
 */

export type ContestStatus = 'draft' | 'upcoming' | 'live' | 'ended';

export interface Contest {
  id: string;
  title: string;
  description: string;
  createdByInstructorId: string;
  /** IDs of problems included in this contest (from ProblemService) */
  problemIds: string[];
  /** Explicit subset of student IDs assigned — never implicit "all" */
  assignedStudentIds: string[];
  startAt: string;   // ISO-8601
  endAt: string;     // ISO-8601
  status: ContestStatus;
}

export interface ContestResult {
  contestId: string;
  studentId: string;
  studentName: string;
  /** Rank within this contest — 1-based, by score desc, ties broken by problemsSolved then finishedAt */
  rank: number;
  score: number;
  problemsSolved: number;
  totalProblems: number;
  /** Percentage of attempted problems answered correctly */
  accuracy: number;
  finishedAt: string; // ISO-8601
}

/** Lightweight shape used when creating a new contest */
export interface CreateContestPayload {
  title: string;
  description: string;
  problemIds: string[];
  assignedStudentIds: string[];
  startAt: string;
  endAt: string;
}
