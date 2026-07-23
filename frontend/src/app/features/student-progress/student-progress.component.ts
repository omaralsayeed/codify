import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';

import { AnalyticsService } from '../../core/services/analytics.service';
import { AuthService }      from '../../core/services/auth.service';
import { StudentAnalytics } from '../../core/models/analytics.model';

@Component({
  selector: 'app-student-progress',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './student-progress.component.html',
  styleUrl: './student-progress.component.scss',
})
export class StudentProgressComponent implements OnInit, OnDestroy {
  // ── DI ─────────────────────────────────────────────────────────────────────
  private readonly analyticsService = inject(AnalyticsService);
  readonly authService              = inject(AuthService);
  private readonly cdr              = inject(ChangeDetectorRef);

  // ── Page state ──────────────────────────────────────────────────────────────
  analytics: StudentAnalytics | null = null;
  isLoading = true;
  error: string | null = null;

  // Time-range toggle — drives chart section (built in later chunks)
  activeTimeRange: '7d' | '30d' | '3m' = '7d';

  // Teardown
  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── Data loading ────────────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.error     = null;

    this.analyticsService
      .getStudentAnalytics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.analytics = data;
          this.isLoading = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.error     = 'Could not load your progress. Please try again.';
          this.isLoading = false;
        },
      });
  }

  // ── Time-range toggle ───────────────────────────────────────────────────────

  setTimeRange(range: '7d' | '30d' | '3m'): void {
    this.activeTimeRange = range;
  }
}
