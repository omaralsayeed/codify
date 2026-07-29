import {
  Component, inject, OnInit, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { InstructorService } from '../../../core/services/instructor.service';
import { ContestService } from '../../../core/services/contest.service';
import { InstructorStudentDetail } from '../../../core/models/instructor.model';
import { ContestResult } from '../../../core/models/contest.model';

/** A contest history row enriched with class-average delta */
interface ContestHistoryRow extends ContestResult {
  contestTitle: string;
  classAvgScore: number;
  scoreDelta: number;  // student score − class avg (positive = above avg)
}

@Component({
  selector: 'app-instructor-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-student-detail.component.html',
  styleUrl: './instructor-student-detail.component.scss',
})
export class InstructorStudentDetailComponent implements OnInit {
  private readonly route          = inject(ActivatedRoute);
  private readonly instructorSvc  = inject(InstructorService);
  private readonly contestSvc     = inject(ContestService);

  readonly student = signal<InstructorStudentDetail | null | 'not-found'>('not-found');

  /** Contest history rows, chronological (oldest first) */
  contestHistory: ContestHistoryRow[] = [];

  /** SVG sparkline path string — score trend */
  sparklinePath  = '';
  sparklineW     = 260;
  sparklineH     = 56;
  minScore       = 0;
  scoreRange     = 1;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    const found = this.instructorSvc.getStudentById(id);
    this.student.set(found ?? 'not-found');

    if (found) {
      this.buildContestHistory(id);
    }
  }

  // ── Contest history builder ───────────────────────────────────────────────

  private buildContestHistory(studentId: string): void {
    const raw = this.contestSvc.getStudentContestHistory(studentId); // chronological
    const allContests = this.contestSvc.getContests();

    this.contestHistory = raw.map(r => {
      const contest     = allContests.find(c => c.id === r.contestId);
      const allResults  = this.contestSvc.getContestResults(r.contestId);
      const classAvg    = allResults.length
        ? Math.round(allResults.reduce((s, x) => s + x.score, 0) / allResults.length)
        : 0;

      return {
        ...r,
        contestTitle: contest?.title ?? `Contest ${r.contestId}`,
        classAvgScore: classAvg,
        scoreDelta: r.score - classAvg,
      };
    });

    if (this.contestHistory.length >= 2) {
      this.sparklinePath = this.buildSparkline(this.contestHistory.map(r => r.score));
    }
  }

  // ── SVG sparkline ─────────────────────────────────────────────────────────

  private buildSparkline(scores: number[]): string {
    const W = this.sparklineW;
    const H = this.sparklineH;
    const pad = 4;
    const min = Math.min(...scores);
    const max = Math.max(...scores);
    const range = max - min || 1;

    this.minScore   = min;
    this.scoreRange = range;

    const points = scores.map((s, i) => {
      const x = pad + (i / (scores.length - 1)) * (W - pad * 2);
      const y = H - pad - ((s - min) / range) * (H - pad * 2);
      return [x, y] as [number, number];
    });

    // Smooth line using cubic bezier through control points
    let d = `M ${points[0][0]} ${points[0][1]}`;
    for (let i = 1; i < points.length; i++) {
      const prev = points[i - 1];
      const curr = points[i];
      const cpX = (prev[0] + curr[0]) / 2;
      d += ` C ${cpX} ${prev[1]}, ${cpX} ${curr[1]}, ${curr[0]} ${curr[1]}`;
    }
    return d;
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  deltaClass(delta: number): string {
    if (delta > 0)  return 'delta--above';
    if (delta < 0)  return 'delta--below';
    return 'delta--neutral';
  }

  deltaLabel(delta: number): string {
    if (delta > 0) return `+${delta}`;
    return `${delta}`;
  }

  rankBadgeClass(rank: number): string {
    if (rank === 1) return 'rank--gold';
    if (rank === 2) return 'rank--silver';
    if (rank === 3) return 'rank--bronze';
    return 'rank--default';
  }

  barColor(pct: number): string {
    if (pct >= 75) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 40) return 'bar--gold';
    return 'bar--red';
  }

  integrityClass(status: string): string {
    if (status === 'flagged') return 'badge--flagged';
    if (status === 'review')  return 'badge--review';
    return 'badge--clean';
  }

  statusClass(status: string): string {
    if (status === 'Accepted')    return 'status--accepted';
    if (status === 'WrongAnswer') return 'status--wrong';
    return 'status--other';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }
}
