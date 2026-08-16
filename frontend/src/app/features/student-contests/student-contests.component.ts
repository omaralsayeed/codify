import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ContestService } from '../../core/services/contest.service';
import { StudentContestsOverview, Contest, StudentPastContest } from '../../core/models/contest.model';

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
