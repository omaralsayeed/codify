export type ContestStatus = 'draft' | 'upcoming' | 'live' | 'ended';

export interface ContestProblemDetail {
  id: string;
  title: string;
  difficulty: string;
  points: number;
  order: number;
}

export interface Contest {
  id: string;
  title: string;
  description: string;
  createdByInstructorId: string;
  instructorName?: string;
  problemIds: string[];
  problems?: ContestProblemDetail[];
  assignedStudentIds: string[];
  startAt: string;   // ISO-8601
  endAt: string;     // ISO-8601
  status: ContestStatus;
  createdAt?: string;
}

export interface ContestResult {
  contestId: string;
  contestTitle?: string;
  studentId: string;
  studentName: string;
  rank: number;
  score: number;
  problemsSolved: number;
  totalProblems: number;
  accuracy: number;
  finishedAt: string; // ISO-8601
}

export interface CreateContestPayload {
  title: string;
  description: string;
  problemIds: string[];
  assignedStudentIds: string[];
  startAt: string;
  endAt: string;
}

export interface StudentPastContest {
  contestId: string;
  title: string;
  description: string;
  instructorName: string;
  startAt: string;
  endAt: string;
  totalProblems: number;
  problemsSolved: number;
  score: number;
  rank: number;
  accuracy: number;
  finishedAt?: string;
  problems: ContestProblemDetail[];
}

export interface StudentContestsOverview {
  hasActiveContestNotification: boolean;
  activeContestsCount: number;
  liveContests: Contest[];
  upcomingContests: Contest[];
  pastContests: StudentPastContest[];
}
