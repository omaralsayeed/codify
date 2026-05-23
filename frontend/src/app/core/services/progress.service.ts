import { Injectable } from '@angular/core';
import { StudentProgress, ClassProgress } from '../models/progress.model';

@Injectable({ providedIn: 'root' })
export class ProgressService {
  getStudentProgress(): StudentProgress {
    return {
      problemsSolved: 47,
      avgScore: 68,
      streak: 12,
      hintsUsedToday: 3,
      hintsLimit: 5,
      topicMastery: [
        { topic: 'Arrays',             percentage: 85 },
        { topic: 'Recursion',          percentage: 72 },
        { topic: 'Dyn. Programming',   percentage: 54 },
        { topic: 'Graphs',             percentage: 38 },
        { topic: 'Greedy',             percentage: 61 },
      ]
    };
  }

  getClassProgress(): ClassProgress {
    return {
      activeStudents: 28,
      enrolledStudents: 32,
      classAvgScore: 63,
      integrityFlags: 3,
      assignedProblems: 14,
      topicMastery: [
        { topic: 'Arrays',           percentage: 78 },
        { topic: 'Recursion',        percentage: 65 },
        { topic: 'Dyn. Programming', percentage: 48 },
        { topic: 'Graphs',           percentage: 41 },
        { topic: 'Sorting',          percentage: 72 },
      ]
    };
  }
}
