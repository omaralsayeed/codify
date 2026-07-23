import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, takeUntil } from 'rxjs';

import { AnalyticsService }  from '../../core/services/analytics.service';
import { AuthService }       from '../../core/services/auth.service';
import {
  StudentAnalytics,
  DailyActivity,
  TopicPerformance,
  TopicStrength,
} from '../../core/models/analytics.model';
import { TopicRadarChartComponent } from './topic-radar-chart.component';

// ── countUp config ────────────────────────────────────────────────────────────
const COUNT_UP_DURATION_MS = 800;
const COUNT_UP_TICK_MS     = 16;   // ~60 fps

// Strength sort order: weak → average → strong
const STRENGTH_ORDER: Record<TopicStrength, number> = {
  weak: 0, average: 1, strong: 2,
};

@Component({
  selector: 'app-student-progress',
  standalone: true,
  imports: [CommonModule, TopicRadarChartComponent],
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

  // ── Activity dot stagger visibility ────────────────────────────────────────
  dotVisible: boolean[] = Array(7).fill(false);

  // ── Topic bar animation: width currently displayed per topic (by topicId) ──
  barWidths: Record<string, number> = {};

  // ── Timer handles ───────────────────────────────────────────────────────────
  private counterIds: (ReturnType<typeof setInterval> | null)[] = [];
  private dotTimerIds: (ReturnType<typeof setTimeout> | null)[] = [];
  private barTimerId: ReturnType<typeof setTimeout> | null = null;

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
    if (this.barTimerId !== null) clearTimeout(this.barTimerId);
  }

  // ── Data loading ────────────────────────────────────────────────────────────

  load(): void {
    this.isLoading = true;
    this.error     = null;

    this.displayedAttempted   = 0;
    this.displayedSolved      = 0;
    this.displayedSuccessRate = 0;
    this.displayedStreak      = 0;
    this.dotVisible           = Array(7).fill(false);
    this.barWidths            = {};
    this.clearAllCounters();
    this.clearAllDotTimers();

    this.analyticsService
      .getStudentAnalytics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.analytics = data;
          this.isLoading = false;
          // Initialise all bars at 0 so the CSS transition fires from 0
          data.topics.forEach(t => { this.barWidths[t.topicId] = 0; });
          this.cdr.detectChanges();
          this.startHeroAnimations(data);
          // Small delay so the DOM paints the 0-width bars first,
          // then we set real widths and the CSS transition animates them
          this.barTimerId = setTimeout(() => {
            data.topics.forEach(t => {
              this.barWidths[t.topicId] = t.strengthScore;
            });
            this.cdr.detectChanges();
          }, 120);
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

  // ── Sorted topics ───────────────────────────────────────────────────────────

  /** Returns topics sorted weak → average → strong, then by strengthScore asc. */
  get sortedTopics(): TopicPerformance[] {
    return [...(this.analytics?.topics ?? [])].sort((a, b) => {
      const orderDiff = STRENGTH_ORDER[a.strength] - STRENGTH_ORDER[b.strength];
      return orderDiff !== 0 ? orderDiff : a.strengthScore - b.strengthScore;
    });
  }

  // ── Bar helpers ─────────────────────────────────────────────────────────────

  barWidth(topic: TopicPerformance): number {
    return this.barWidths[topic.topicId] ?? 0;
  }

  strengthBarClass(strength: TopicStrength): string {
    return `topic-bar__fill topic-bar__fill--${strength}`;
  }

  strengthBadgeClass(strength: TopicStrength): string {
    return `strength-badge strength-badge--${strength}`;
  }

  trackByTopicId(_i: number, t: TopicPerformance): string {
    return t.topicId;
  }

  // ── Hero animation orchestration ────────────────────────────────────────────

  private startHeroAnimations(data: StudentAnalytics): void {
    const s = data.summary;

    this.counterIds[0] = this.countUp(s.totalAttempted,       v => { this.displayedAttempted   = v; this.cdr.detectChanges(); });
    this.counterIds[1] = this.countUp(s.totalSolved,          v => { this.displayedSolved      = v; this.cdr.detectChanges(); });
    this.counterIds[2] = this.countUp(s.successRate,          v => { this.displayedSuccessRate = v; this.cdr.detectChanges(); });
    this.counterIds[3] = this.countUp(s.streak.currentStreak, v => { this.displayedStreak      = v; this.cdr.detectChanges(); });

    s.streak.lastSevenDays.forEach((_, i) => {
      const id = setTimeout(() => {
        this.dotVisible[i] = true;
        this.cdr.detectChanges();
      }, i * 60);
      this.dotTimerIds[i] = id;
    });
  }

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
      const eased = 1 - Math.pow(1 - tick / totalTicks, 2);
      setter(Math.min(target, Math.round(eased * target)));
      if (tick >= totalTicks) setter(target);
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
    const [y, m, d] = day.date.split('-').map(Number);
    return new Date(y, m - 1, d).toLocaleDateString('en-US', { weekday: 'short' });
  }

  isToday(day: DailyActivity): boolean {
    return day.date === new Date().toISOString().slice(0, 10);
  }
}
