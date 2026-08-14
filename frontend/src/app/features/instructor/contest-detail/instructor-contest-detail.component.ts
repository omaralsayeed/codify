import {
  Component, inject, OnInit, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { ProblemService } from '../../../core/services/problem.service';
import { Contest, ContestResult } from '../../../core/models/contest.model';
import { Problem } from '../../../core/models/problem.model';

/** One bar bucket in the score-distribution chart */
interface ScoreBucket {
  label: string;   // e.g. "60–69"
  count: number;
  pct: number;     // height % of max bucket
}

/** Per-problem accuracy row */
interface ProblemAccuracy {
  title: string;
  solved: number;
  total: number;
  accuracy: number;
}

@Component({
  selector: 'app-instructor-contest-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-contest-detail.component.html',
  styleUrl: './instructor-contest-detail.component.scss',
})
export class InstructorContestDetailComponent implements OnInit {
  private readonly route      = inject(ActivatedRoute);
  private readonly contestSvc = inject(ContestService);
  private readonly problemSvc = inject(ProblemService);

  readonly contest = signal<Contest | 'not-found' | null>(null);
  readonly results = signal<ContestResult[]>([]);

  // ── Derived analytics (populated in ngOnInit) ─────────────────────────────
  avgScore        = 0;
  avgAccuracy     = 0;
  completionRate  = 0;
  scoreBuckets: ScoreBucket[]     = [];
  problemAccuracy: ProblemAccuracy[] = [];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    const found = this.contestSvc.getContestById(id);
    if (!found) { this.contest.set('not-found'); return; }

    this.contest.set(found);

    const res = this.contestSvc.getContestResults(id);
    this.results.set(res);

    if (res.length > 0) {
      this.avgScore       = Math.round(res.reduce((s, r) => s + r.score, 0) / res.length);
      this.avgAccuracy    = Math.round(res.reduce((s, r) => s + r.accuracy, 0) / res.length);
      this.completionRate = Math.round((res.length / found.assignedStudentIds.length) * 100);
      this.scoreBuckets   = this.buildScoreBuckets(res);
      this.problemAccuracy = this.buildProblemAccuracy(found, res);
    }
  }

  // ── Chart builders ────────────────────────────────────────────────────────

  private buildScoreBuckets(res: ContestResult[]): ScoreBucket[] {
    const buckets: Record<string, number> = {
      '0–49': 0, '50–59': 0, '60–69': 0,
      '70–79': 0, '80–89': 0, '90–100': 0,
    };
    for (const r of res) {
      if      (r.score <= 49)  buckets['0–49']++;
      else if (r.score <= 59)  buckets['50–59']++;
      else if (r.score <= 69)  buckets['60–69']++;
      else if (r.score <= 79)  buckets['70–79']++;
      else if (r.score <= 89)  buckets['80–89']++;
      else                     buckets['90–100']++;
    }
    const max = Math.max(...Object.values(buckets), 1);
    return Object.entries(buckets).map(([label, count]) => ({
      label,
      count,
      pct: Math.round((count / max) * 100),
    }));
  }

  private buildProblemAccuracy(contest: Contest, res: ContestResult[]): ProblemAccuracy[] {
    const problems = this.problemSvc.getAllSync();
    const total    = res.length;

    return contest.problemIds.map(pid => {
      const problem = problems.find(p => p.id === pid);
      // Count results where student solved at least this many problems
      // Approximate: use problemsSolved / totalProblems ratio per student per problem slot
      const solved  = res.filter(r => (r.problemsSolved / r.totalProblems) >= (1 / contest.problemIds.length)).length;
      const accuracy = total > 0 ? Math.round((solved / total) * 100) : 0;
      return {
        title:    problem?.title ?? `Problem ${pid}`,
        solved,
        total,
        accuracy,
      };
    });
  }

  // ── Leaderboard badge helpers (reuse integrity badge pattern) ─────────────

  rankBadgeClass(rank: number): string {
    if (rank === 1) return 'rank--gold';
    if (rank === 2) return 'rank--silver';
    if (rank === 3) return 'rank--bronze';
    return 'rank--default';
  }

  scoreBadgeClass(score: number): string {
    if (score >= 80) return 'badge--high';
    if (score >= 60) return 'badge--medium';
    return 'badge--low';
  }

  barColor(accuracy: number): string {
    if (accuracy >= 70) return 'bar--teal';
    if (accuracy >= 50) return 'bar--blue';
    if (accuracy >= 35) return 'bar--gold';
    return 'bar--red';
  }

  // ── Date helpers ──────────────────────────────────────────────────────────

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString('en-GB', {
      hour: '2-digit', minute: '2-digit',
    });
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      live: 'badge--live', upcoming: 'badge--upcoming',
      ended: 'badge--ended', draft: 'badge--draft',
    };
    return map[status] ?? '';
  }
}
