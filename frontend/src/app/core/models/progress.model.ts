export interface TopicMastery {
  topic: string;
  percentage: number;
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
