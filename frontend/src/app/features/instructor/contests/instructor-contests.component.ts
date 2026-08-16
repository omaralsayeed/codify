import { Component, inject, ChangeDetectionStrategy, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { Contest, ContestStatus } from '../../../core/models/contest.model';

type FilterStatus = ContestStatus | 'all';

@Component({
  selector: 'app-instructor-contests',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-contests.component.html',
  styleUrl: './instructor-contests.component.scss',
})
export class InstructorContestsComponent implements OnInit {
  private readonly contestSvc = inject(ContestService);

  protected readonly allContests = signal<Contest[]>(this.contestSvc.getContests());

  readonly activeFilter = signal<FilterStatus>('all');

  readonly filters: { label: string; value: FilterStatus }[] = [
    { label: 'All',      value: 'all'      },
    { label: 'Live',     value: 'live'     },
    { label: 'Upcoming', value: 'upcoming' },
    { label: 'Ended',    value: 'ended'    },
    { label: 'Draft',    value: 'draft'    },
  ];

  ngOnInit(): void {
    this.contestSvc.getContests$().subscribe(contests => {
      this.allContests.set(contests || []);
    });
  }

  readonly contests = computed<Contest[]>(() => {
    const f = this.activeFilter();
    const list = this.allContests();
    return f === 'all'
      ? list
      : list.filter(c => c.status === f);
  });

  // ── Per-contest computed stats ────────────────────────────────────────────

  participantCount(contest: Contest): number {
    return contest.assignedStudentIds.length;
  }

  avgScore(contest: Contest): number | null {
    if (contest.status !== 'ended') return null;
    const results = this.contestSvc.getContestResults(contest.id);
    if (results.length === 0) return null;
    const sum = results.reduce((acc, r) => acc + r.score, 0);
    return Math.round(sum / results.length);
  }

  // ── Badge helpers ─────────────────────────────────────────────────────────

  statusClass(status: ContestStatus): string {
    const map: Record<ContestStatus, string> = {
      live:     'badge--live',
      upcoming: 'badge--upcoming',
      ended:    'badge--ended',
      draft:    'badge--draft',
    };
    return map[status];
  }

  statusLabel(status: ContestStatus): string {
    return status.charAt(0).toUpperCase() + status.slice(1);
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
}
