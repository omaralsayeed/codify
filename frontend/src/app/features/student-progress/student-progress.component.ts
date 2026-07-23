import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';

import { AnalyticsService }             from '../../core/services/analytics.service';
import { AuthService }                  from '../../core/services/auth.service';
import { StudentAnalytics, DailyActivity } from '../../core/models/analytics.model';

// ── countUp config ────────────────────────────────────────────────────────────
const COUNT_UP_DURATION_MS = 800;
const COUNT_UP_TICK_MS     = 16;   // ~60 fps

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

  // ── Hero countUp display values ─────────────────────────────────────────────
  displayedAttempted   = 0;
  displayedSolved      = 0;
  displayedSuccessRate = 0;
  displayedStreak      = 0;

  // ── Activity dot stagger visibility (index → visible) ──────────────────────
  dotVisible: boolean[] = Array(7).fill(false);

  // ── Timer handles ───────────────────────────────────────────────────────────
  private counterIds: (ReturnType<typeof setInterval> | null)[] = [];
  private dotTimerIds: (ReturnType<typeof setTimeout> | null)[] = [];

  // Teardown
  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearAllCounters();
    this.clearAllDotTimers();
  }

  // ── Data loading ────────────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.error     = null;

    // Reset display values for retry
    this.displayedAttempted   = 0;
    this.displayedSolved      = 0;
    this.displayedSuccessRate = 0;
    this.displayedStreak      = 0;
    this.dotVisible           = Array(7).fill(false);
    this.clearAllCounters();
    this.clearAllDotTimers();

    this.analyticsService
      .getStudentAnalytics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.analytics = data;
          this.isLoading = false;
          this.cdr.detectChanges();
          this.startHeroAnimations(data);
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

  // ── Hero animation orchestration ────────────────────────────────────────────

  private startHeroAnimations(data: StudentAnalytics): void {
    const s = data.summary;

    // Four stat counters — all start simultaneously, 800 ms each
    this.counterIds[0] = this.countUp(s.totalAttempted,   v => this.displayedAttempted   = v);
    this.counterIds[1] = this.countUp(s.totalSolved,      v => this.displayedSolved      = v);
    this.counterIds[2] = this.countUp(s.successRate,      v => this.displayedSuccessRate = v);
    this.counterIds[3] = this.countUp(s.streak.currentStreak, v => this.displayedStreak = v);

    // Activity dots stagger in at 60 ms intervals
    s.streak.lastSevenDays.forEach((_, i) => {
      const id = setTimeout(() => {
        this.dotVisible[i] = true;
        this.cdr.detectChanges();
      }, i * 60);
      this.dotTimerIds[i] = id;
    });
  }

  /**
   * Counts a display value from 0 → target over COUNT_UP_DURATION_MS.
   * Uses ease-out quadratic — fast start, gentle landing.
   * Respects prefers-reduced-motion: jumps straight to target if set.
   * Returns the interval ID so the caller can clear it on destroy.
   */
  private countUp(
    target: number,
    setter: (v: number) => void,
  ): ReturnType<typeof setInterval> | null {
    if (this.prefersReducedMotion() || target === 0) {
      setter(target);
      return null;
    }

    const totalTicks = Math.ceil(COUNT_UP_DURATION_MS / COUNT_UP_TICK_MS);
    let tick = 0;

    return setInterval(() => {
      tick++;
      const progress = tick / totalTicks;
      const eased    = 1 - Math.pow(1 - progress, 2);  // ease-out quad
      setter(Math.min(target, Math.round(eased * target)));

      if (tick >= totalTicks) {
        setter(target); // guarantee exact final value
        // Can't self-clear inside setInterval without the id — caller clears
      }
    }, COUNT_UP_TICK_MS);
  }

  private clearAllCounters(): void {
    this.counterIds.forEach(id => { if (id !== null) clearInterval(id); });
    this.counterIds = [];
  }

  private clearAllDotTimers(): void {
    this.dotTimerIds.forEach(id => { if (id !== null) clearTimeout(id); });
    this.dotTimerIds = [];
  }

  private prefersReducedMotion(): boolean {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  // ── Greeting ────────────────────────────────────────────────────────────────

  get greeting(): string {
    const h = new Date().getHours();
    if (h >= 5  && h < 12) return 'Good morning';
    if (h >= 12 && h < 17) return 'Good afternoon';
    if (h >= 17 && h < 22) return 'Good evening';
    return 'Good night';
  }

  // ── Streak helpers ──────────────────────────────────────────────────────────

  get isPersonalBest(): boolean {
    const s = this.analytics?.summary.streak;
    return !!s && s.currentStreak > 0 && s.currentStreak === s.longestStreak;
  }

  // ── Activity dot helpers ────────────────────────────────────────────────────

  dayLabel(day: DailyActivity): string {
    // Parse the ISO date string as local date (split to avoid timezone shift)
    const [y, m, d] = day.date.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString('en-US', { weekday: 'short' });
  }

  isToday(day: DailyActivity): boolean {
    return day.date === new Date().toISOString().slice(0, 10);
  }
}
