import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { InstructorService } from '../../../core/services/instructor.service';
import { ProgressService } from '../../../core/services/progress.service';
import { ContestService } from '../../../core/services/contest.service';
import { ClassProgress, DailyActivity } from '../../../core/models/progress.model';

interface TrendPoint {
  x: number;
  y: number;
  submissions: number;
  dayLabel: string;
}

interface ContestSummaryRow {
  id: string;
  title: string;
  participationRate: number;  // %
  participantCount: number;
  assignedCount: number;
  avgScore: number;
}

@Component({
  selector: 'app-instructor-overview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-overview.component.html',
  styleUrl: './instructor-overview.component.scss',
})
export class InstructorOverviewComponent {
  private readonly instructorSvc = inject(InstructorService);
  private readonly progressSvc   = inject(ProgressService);
  private readonly contestSvc    = inject(ContestService);

  readonly progress: ClassProgress = this.instructorSvc.getClassProgress();

  // ── Metric cards ──────────────────────────────────────────────────────────

  readonly metrics = [
    {
      label: 'Active Students',
      value: this.progress.activeStudents,
      sub: `of ${this.progress.enrolledStudents} enrolled`,
      colorClass: 'card--teal',
      icon: '👥',
    },
    {
      label: 'Class Avg Score',
      value: this.progress.classAvgScore,
      sub: 'out of 100',
      colorClass: 'card--blue',
      icon: '📊',
    },
    {
      label: 'Integrity Flags',
      value: this.progress.integrityFlags,
      sub: 'need review',
      colorClass: this.progress.integrityFlags > 0 ? 'card--red' : 'card--teal',
      icon: '⚑',
    },
    {
      label: 'Assigned Problems',
      value: this.progress.assignedProblems,
      sub: 'total problems',
      colorClass: 'card--gold',
      icon: '🗂️',
    },
  ];

  // ── Activity trend chart ──────────────────────────────────────────────────

  readonly trendW    = 520;
  readonly trendH    = 80;
  readonly padY      = 6;
  private  readonly padX = 0;

  readonly activityDays: DailyActivity[] = this.progressSvc.getClassActivityTrend();

  readonly trendPoints: TrendPoint[] = (() => {
    const days = this.activityDays;
    const max  = Math.max(...days.map(d => d.submissions), 1);
    const W    = this.trendW;
    const H    = this.trendH;
    const pY   = this.padY;

    return days.map((d, i) => ({
      x:           (i / (days.length - 1)) * W,
      y:           H - pY - (d.submissions / max) * (H - pY * 2),
      submissions: d.submissions,
      dayLabel:    d.dayLabel,
    }));
  })();

  /** Smooth bezier path through trend points */
  readonly trendPath: string = (() => {
    const pts = this.trendPoints;
    if (pts.length < 2) return '';
    let d = `M ${pts[0].x} ${pts[0].y}`;
    for (let i = 1; i < pts.length; i++) {
      const p = pts[i - 1];
      const c = pts[i];
      const cpX = (p.x + c.x) / 2;
      d += ` C ${cpX} ${p.y}, ${cpX} ${c.y}, ${c.x} ${c.y}`;
    }
    return d;
  })();

  /** Closed fill path (area under curve) */
  readonly trendArea: string = (() => {
    const pts = this.trendPoints;
    const H   = this.trendH;
    if (pts.length < 2) return '';
    let d = `M ${pts[0].x} ${H} L ${pts[0].x} ${pts[0].y}`;
    for (let i = 1; i < pts.length; i++) {
      const p = pts[i - 1];
      const c = pts[i];
      const cpX = (p.x + c.x) / 2;
      d += ` C ${cpX} ${p.y}, ${cpX} ${c.y}, ${c.x} ${c.y}`;
    }
    d += ` L ${pts[pts.length - 1].x} ${H} Z`;
    return d;
  })();

  readonly trendMax = Math.max(...this.activityDays.map(d => d.submissions), 1);
  readonly trendTotal = this.activityDays.reduce((s, d) => s + d.submissions, 0);

  // Show every other label on x-axis to avoid crowding
  showLabel(i: number): boolean { return i % 2 === 0; }

  // ── Topic mastery ─────────────────────────────────────────────────────────

  /** Topics sorted descending by mastery percentage */
  readonly sortedTopics = [...this.progress.topicMastery]
    .sort((a, b) => b.percentage - a.percentage);

  readonly masteryGridLines = [25, 50, 75, 100];

  barColor(pct: number): string {
    if (pct >= 70) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 40) return 'bar--gold';
    return 'bar--red';
  }

  // ── Contest summary (ended only, most recent 6) ───────────────────────────

  /** Max 6 most recent ended contests, newest first */
  readonly MAX_CONTESTS = 6;

  readonly contestSummaries: ContestSummaryRow[] = (() => {
    const ended = this.contestSvc.getContests()
      .filter(c => c.status === 'ended')
      .sort((a, b) => new Date(b.endAt).getTime() - new Date(a.endAt).getTime())
      .slice(0, this.MAX_CONTESTS);

    return ended.map(c => {
      const results         = this.contestSvc.getContestResults(c.id);
      const participantCount = results.length;
      const assignedCount    = c.assignedStudentIds.length;
      const participationRate = assignedCount > 0
        ? Math.round((participantCount / assignedCount) * 100)
        : 0;
      const avgScore = participantCount > 0
        ? Math.round(results.reduce((s, r) => s + r.score, 0) / participantCount)
        : 0;

      return { id: c.id, title: c.title, participationRate, participantCount, assignedCount, avgScore };
    });
  })();

  readonly totalEndedContests = this.contestSvc.getContests()
    .filter(c => c.status === 'ended').length;

  readonly hasMoreContests = this.totalEndedContests > this.MAX_CONTESTS;
}
