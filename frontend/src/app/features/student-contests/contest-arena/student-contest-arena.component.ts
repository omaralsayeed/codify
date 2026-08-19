import {
  Component, inject, OnInit, OnDestroy, ChangeDetectionStrategy, signal, computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { Contest, ContestProblemDetail, ContestResult } from '../../../core/models/contest.model';

@Component({
  selector: 'app-student-contest-arena',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './student-contest-arena.component.html',
  styleUrl: './student-contest-arena.component.scss',
})
export class StudentContestArenaComponent implements OnInit, OnDestroy {
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly contestSvc = inject(ContestService);

  readonly isLoading = signal(true);
  readonly notFound = signal(false);
  readonly contest = signal<Contest | null>(null);
  readonly results = signal<ContestResult[]>([]);
  readonly activeTab = signal<'problems' | 'standings'>('problems');
  readonly timeRemaining = signal<string>('');

  private timerInterval?: any;

  readonly totalPoints = computed(() => {
    const c = this.contest();
    if (!c || !c.problems) return 0;
    return c.problems.reduce((sum, p) => sum + (p.points || 100), 0);
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    if (!id) {
      this.isLoading.set(false);
      this.notFound.set(true);
      return;
    }

    this.contestSvc.getContestById$(id).subscribe({
      next: (found) => {
        this.isLoading.set(false);
        if (!found) {
          this.notFound.set(true);
          return;
        }
        this.contest.set(found);
        this.updateCountdown(found.endAt, found.startAt);

        // Fetch standings
        this.contestSvc.getContestResults$(id).subscribe((res) => {
          this.results.set(res || []);
        });

        // Live countdown timer
        this.timerInterval = setInterval(() => {
          this.updateCountdown(found.endAt, found.startAt);
        }, 1000);
      },
      error: () => {
        this.isLoading.set(false);
        this.notFound.set(true);
      }
    });
  }

  ngOnDestroy(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
    }
  }

  setTab(tab: 'problems' | 'standings'): void {
    this.activeTab.set(tab);
  }

  startProblem(problem: ContestProblemDetail): void {
    const c = this.contest();
    const contestId = c ? c.id : '';
    this.router.navigate(['/problems', problem.id], {
      queryParams: contestId ? { contestId } : {}
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

  private updateCountdown(endAtIso: string, startAtIso: string): void {
    const now = Date.now();
    const startMs = new Date(startAtIso).getTime();
    const endMs = new Date(endAtIso).getTime();

    if (now < startMs) {
      const diff = startMs - now;
      const hours = Math.floor(diff / (1000 * 60 * 60));
      const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
      const secs = Math.floor((diff % (1000 * 60)) / 1000);
      this.timeRemaining.set(`Starts in ${hours}h ${mins}m ${secs}s`);
      return;
    }

    if (now >= endMs) {
      this.timeRemaining.set('Contest Ended');
      return;
    }

    const diff = endMs - now;
    const hours = Math.floor(diff / (1000 * 60 * 60));
    const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
    const secs = Math.floor((diff % (1000 * 60)) / 1000);
    this.timeRemaining.set(`${this.pad(hours)}:${this.pad(mins)}:${this.pad(secs)} remaining`);
  }

  private pad(n: number): string {
    return n < 10 ? `0${n}` : `${n}`;
  }
}
