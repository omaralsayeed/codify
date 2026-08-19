import { Component, inject, ChangeDetectionStrategy, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { InstructorService } from '../../../core/services/instructor.service';
import { ContestService } from '../../../core/services/contest.service';
import { DailyActivity } from '../../../core/models/progress.model';

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
export class InstructorOverviewComponent implements OnInit {
  private readonly instructorSvc = inject(InstructorService);
  private readonly contestSvc    = inject(ContestService);

  // ── Metric cards ──────────────────────────────────────────────────────────

  private readonly _metrics = signal([
    {
      label: 'Active Students',
      value: 0,
      sub: 'in platform',
      colorClass: 'card--teal',
      icon: '👥',
    },
    {
      label: 'Class Avg Score',
      value: 0,
      sub: 'out of 100',
      colorClass: 'card--blue',
      icon: '📊',
    },
    {
      label: 'Integrity Flags',
      value: 0,
      sub: 'need review',
      colorClass: 'card--teal',
      icon: '⚑',
    },
    {
      label: 'Assigned Problems',
      value: 0,
      sub: 'total problems',
      colorClass: 'card--gold',
      icon: '🗂️',
    },
  ]);

  get metrics() {
    return this._metrics();
  }

  // ── Activity trend chart (reactive) ───────────────────────────────────────

  readonly trendW = 520;
  readonly trendH = 80;
  readonly padY   = 6;

  readonly activityDays = signal<DailyActivity[]>([]);

  private readonly _trendMax = computed(() => Math.max(...this.activityDays().map(d => d.submissions), 1));
  private readonly _trendTotal = computed(() => this.activityDays().reduce((s, d) => s + d.submissions, 0));

  private readonly _trendPoints = computed<TrendPoint[]>(() => {
    const days = this.activityDays();
    const max  = this._trendMax();
    const W    = this.trendW;
    const H    = this.trendH;
    const pY   = this.padY;

    if (days.length === 0) return [];

    return days.map((d, i) => ({
      x:           days.length > 1 ? (i / (days.length - 1)) * W : 0,
      y:           H - pY - (d.submissions / max) * (H - pY * 2),
      submissions: d.submissions,
      dayLabel:    d.dayLabel,
    }));
  });

  private readonly _trendPath = computed<string>(() => {
    const pts = this._trendPoints();
    if (pts.length < 2) return '';
    let d = `M ${pts[0].x} ${pts[0].y}`;
    for (let i = 1; i < pts.length; i++) {
      const p = pts[i - 1];
      const c = pts[i];
      const cpX = (p.x + c.x) / 2;
      d += ` C ${cpX} ${p.y}, ${cpX} ${c.y}, ${c.x} ${c.y}`;
    }
    return d;
  });

  private readonly _trendArea = computed<string>(() => {
    const pts = this._trendPoints();
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
  });

  get trendMax(): number { return this._trendMax(); }
  get trendTotal(): number { return this._trendTotal(); }
  get trendPoints(): TrendPoint[] { return this._trendPoints(); }
  get trendPath(): string { return this._trendPath(); }
  get trendArea(): string { return this._trendArea(); }

  // ── Topic mastery (reactive) ───────────────────────────────────────────────

  private readonly _sortedTopics = signal<{ topic: string; percentage: number }[]>([]);

  get sortedTopics() {
    return this._sortedTopics();
  }

  readonly masteryGridLines = [25, 50, 75, 100];

  barColor(pct: number): string {
    if (pct >= 70) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 40) return 'bar--gold';
    return 'bar--red';
  }

  // ── Contest summary (ended only, most recent 6) ───────────────────────────
  readonly MAX_CONTESTS = 6;
  private readonly _contestSummaries = signal<ContestSummaryRow[]>([]);
  get contestSummaries(): ContestSummaryRow[] { return this._contestSummaries(); }
  readonly totalEndedContests = computed(() => this._contestSummaries().length);
  get hasMoreContests(): boolean { return this.totalEndedContests() > this.MAX_CONTESTS; }

  // ── OnInit data loader ────────────────────────────────────────────────────

  ngOnInit(): void {
    this.instructorSvc.getOverview$().subscribe(data => {
      if (data) {
        // 1. Update Metrics Cards
        this._metrics.set([
          {
            label: 'Active Students',
            value: data.totalStudentsReached,
            sub: `${data.totalStudentsReached} active students`,
            colorClass: 'card--teal',
            icon: '👥',
          },
          {
            label: 'Class Avg Score',
            value: Math.round(data.overallAcceptRatePercent),
            sub: 'out of 100',
            colorClass: 'card--blue',
            icon: '📊',
          },
          {
            label: 'Integrity Flags',
            value: data.integrityFlagsCount ?? 0,
            sub: 'need review',
            colorClass: (data.integrityFlagsCount ?? 0) > 0 ? 'card--red' : 'card--teal',
            icon: '⚑',
          },
          {
            label: 'Assigned Problems',
            value: data.totalAssignedProblems || data.totalProblemsAuthored || 0,
            sub: 'total problems',
            colorClass: 'card--gold',
            icon: '🗂️',
          },
        ]);

        // 2. Update Submission Activity Trend
        if (data.dailyActivity && data.dailyActivity.length > 0) {
          this.activityDays.set(
            data.dailyActivity.map(d => ({
              date: d.date,
              dayLabel: d.dayLabel,
              submissions: d.submissions,
            }))
          );
        }

        // 3. Update Topic Mastery Bars
        if (data.topicPerformance && data.topicPerformance.length > 0) {
          this._sortedTopics.set(
            [...data.topicPerformance].sort((a, b) => b.percentage - a.percentage)
          );
        }
      }
    });

    this.instructorSvc.getIntegrityFlags$().subscribe(flags => {
      if (flags) {
        this._metrics.update(m => [
          m[0],
          m[1],
          {
            ...m[2],
            value: flags.length,
            colorClass: flags.length > 0 ? 'card--red' : 'card--teal',
          },
          m[3],
        ]);
      }
    });

    this.contestSvc.getContests$().subscribe(contests => {
      if (contests) {
        const ended = contests
          .filter(c => c.status === 'ended')
          .sort((a, b) => new Date(b.endAt).getTime() - new Date(a.endAt).getTime())
          .slice(0, this.MAX_CONTESTS);

        const rows: ContestSummaryRow[] = ended.map(c => {
          const results = this.contestSvc.getContestResults(c.id);
          const participantCount = results.length;
          const assignedCount = c.assignedStudentIds.length;
          const participationRate = assignedCount > 0
            ? Math.round((participantCount / assignedCount) * 100)
            : 0;
          const avgScore = participantCount > 0
            ? Math.round(results.reduce((s, r) => s + r.score, 0) / participantCount)
            : 0;

          return { id: c.id, title: c.title, participationRate, participantCount, assignedCount, avgScore };
        });

        this._contestSummaries.set(rows);
      }
    });
  }

  showLabel(i: number): boolean { return i % 2 === 0; }
}
