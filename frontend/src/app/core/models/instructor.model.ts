export interface InstructorStudentSummary {
  id: string;
  name: string;
  initials: string;
  avgScore: number;
  problemsSolved: number;
  integrityStatus: 'clean' | 'flagged' | 'review';
}

export interface TopicMastery {
  topic: string;
  percentage: number;
}

export interface RecentSubmissionSummary {
  problemTitle: string;
  status: string;
  submittedAt: string;
}

export interface InstructorStudentDetail extends InstructorStudentSummary {
  streak: number;
  hintsUsed: number;
  lastActiveAt: string;
  topicMastery: TopicMastery[];
  recentSubmissions: RecentSubmissionSummary[];
}

export interface IntegrityFlag {
  id: string;
  studentId: string;
  studentName: string;
  severity: 'low' | 'medium' | 'high';
  reason: string;
  detectedAt: string;
}