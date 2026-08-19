import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ContestService } from '../../core/services/contest.service';
import { StudentContestsOverview } from '../../core/models/contest.model';

@Component({
  selector: 'app-student-contests',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './student-contests.component.html',
  styleUrl: './student-contests.component.scss',
})
export class StudentContestsComponent implements OnInit {
  private readonly contestSvc = inject(ContestService);

  readonly isLoading = signal(true);
  readonly overview = signal<StudentContestsOverview | null>(null);
  readonly respondingContestId = signal<string | null>(null);
  readonly actionMessage = signal<{ type: 'success' | 'error'; text: string } | null>(null);

  ngOnInit(): void {
    this.fetchOverview();
  }

  fetchOverview(): void {
    this.isLoading.set(true);
    this.contestSvc.getMyContests$().subscribe({
      next: (data) => {
        this.overview.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  respondToInvitation(contestId: string, accept: boolean): void {
    this.respondingContestId.set(contestId);
    this.actionMessage.set(null);

    this.contestSvc.respondToInvitation$(contestId, accept).subscribe({
      next: (res) => {
        this.respondingContestId.set(null);
        this.actionMessage.set({
          type: 'success',
          text: accept ? 'Contest invitation accepted! You can now participate in this contest.' : 'Contest invitation declined.',
        });
        this.fetchOverview();
      },
      error: (err) => {
        this.respondingContestId.set(null);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.message ?? 'Failed to update invitation status.',
        });
      }
    });
  }

  formatDate(iso: string): string {
    if (!iso) return '';
    return new Date(iso).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  getTimeRemaining(endAtIso: string): string {
    const totalMs = new Date(endAtIso).getTime() - Date.now();
    if (totalMs <= 0) return 'Ended';
    const hours = Math.floor(totalMs / (1000 * 60 * 60));
    const mins = Math.floor((totalMs % (1000 * 60 * 60)) / (1000 * 60));
    return `${hours}h ${mins}m left`;
  }
}
