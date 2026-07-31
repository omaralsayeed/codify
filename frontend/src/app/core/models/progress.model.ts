export interface TopicMastery {
  topic: string;
  percentage: number;
}

export interface DailyActivity {
  date: string;        // ISO date string, e.g. "2026-07-10"
  dayLabel: string;    // short label, e.g. "Thu 10"
  submissions: number;
}

export interface StudentProgress {
  problemsSolved: number;
  avgScore: number;
  streak: number;
  hintsUsedToday: number;
  hintsLimit: number;
  topicMastery: TopicMastery[];
}

export interface ClassProgress {
  activeStudents: number;
  enrolledStudents: number;
  classAvgScore: number;
  integrityFlags: number;
  assignedProblems: number;
  topicMastery: TopicMastery[];
}
