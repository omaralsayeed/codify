import {
  Component, inject, OnInit, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { Contest, ContestResult } from '../../../core/models/contest.model';

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
    
    this.contestSvc.getContestById$(id).subscribe(found => {
      if (!found) {
        this.contest.set('not-found');
        return;
      }
      this.contest.set(found);

      this.contestSvc.getContestResults$(id).subscribe(res => {
        this.results.set(res);
        if (res.length > 0) {
          this.avgScore       = Math.round(res.reduce((s, r) => s + r.score, 0) / res.length);
          this.avgAccuracy    = Math.round(res.reduce((s, r) => s + r.accuracy, 0) / res.length);
          const acceptedCount = (found.participants || []).filter(p => p.invitationStatus === 'accepted').length;
          this.completionRate = Math.round((res.length / Math.max(acceptedCount || found.assignedStudentIds.length, 1)) * 100);
          this.scoreBuckets   = this.buildScoreBuckets(res);
          this.problemAccuracy = this.buildProblemAccuracy(found, res);
        }
      });
    });
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
    const total = res.length;
    const problems = contest.problems || [];

    if (problems.length > 0) {
      return problems.map(p => {
        const solved = res.filter(r => (r.problemsSolved / Math.max(r.totalProblems, 1)) >= (1 / problems.length)).length;
        const accuracy = total > 0 ? Math.round((solved / total) * 100) : 0;
        return {
          title: p.title,
          solved,
          total,
          accuracy,
        };
      });
    }

    return contest.problemIds.map(pid => {
      const solved = res.filter(r => (r.problemsSolved / Math.max(r.totalProblems, 1)) >= (1 / contest.problemIds.length)).length;
      const accuracy = total > 0 ? Math.round((solved / total) * 100) : 0;
      return {
        title: `Problem ${pid.slice(0, 8)}`,
        solved,
        total,
        accuracy,
      };
    });
  }

  // ── Status badge helpers ──────────────────────────────────────────────────

  invitationStatusClass(status: string): string {
    if (status === 'accepted') return 'inv-badge--accepted';
    if (status === 'declined') return 'inv-badge--declined';
    return 'inv-badge--pending';
  }

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
    if (!iso) return '';
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  formatTime(iso: string): string {
    if (!iso) return '';
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

  getInvitationCount(contest: Contest, status: string): number {
    return (contest.participants || []).filter(p => p.invitationStatus === status).length;
  }
}
