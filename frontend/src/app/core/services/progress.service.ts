import { Injectable } from '@angular/core';
import { StudentProgress, ClassProgress, DailyActivity } from '../models/progress.model';

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

  /**
   * Returns mock daily submission counts for the last 14 days.
   * Pattern is weekday-heavier (Mon–Thu peak, Fri lighter, Sat–Sun low).
   * Anchored to 2026-07-23 (today in project context) so data looks current.
   */
  getClassActivityTrend(): DailyActivity[] {
    // Weekday multiplier: 0=Sun,1=Mon,...,6=Sat
    const dayWeight = [4, 18, 22, 20, 21, 10, 5];
    const base = 8;
    const jitter = [3, -2, 5, -1, 4, 2, -3, 6, -4, 3, 1, -2, 5, 2];

    const today = new Date('2026-07-23T00:00:00Z');
    const days: DailyActivity[] = [];

    for (let i = 13; i >= 0; i--) {
      const d = new Date(today);
      d.setUTCDate(d.getUTCDate() - i);

      const dow   = d.getUTCDay();
      const count = Math.max(0, base + dayWeight[dow] + jitter[13 - i]);

      days.push({
        date:     d.toISOString().slice(0, 10),
        dayLabel: d.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', timeZone: 'UTC' }),
        submissions: count,
      });
    }
    return days;
  }
}
