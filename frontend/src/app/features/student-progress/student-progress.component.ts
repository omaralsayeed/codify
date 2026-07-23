import {
  Component,
  OnInit,
  OnDestroy,
  AfterViewInit,
  inject,
  ChangeDetectorRef,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { AnalyticsService }  from '../../core/services/analytics.service';
import { AuthService }       from '../../core/services/auth.service';
import {
  StudentAnalytics,
  DailyActivity,
  TopicPerformance,
  TopicStrength,
} from '../../core/models/analytics.model';
import { TopicRadarChartComponent }     from './topic-radar-chart.component';
import { SuccessRateChartComponent }    from './success-rate-chart.component';
import { DifficultyDonutChartComponent } from './difficulty-donut-chart.component';

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
  imports: [CommonModule, RouterLink, TopicRadarChartComponent, SuccessRateChartComponent, DifficultyDonutChartComponent],
  templateUrl: './student-progress.component.html',
  styleUrl: './student-progress.component.scss',
})
export class StudentProgressComponent implements OnInit, AfterViewInit, OnDestroy {
  // ── DI ─────────────────────────────────────────────────────────────────────
  private readonly analyticsService = inject(AnalyticsService);
  readonly authService              = inject(AuthService);
  private readonly cdr              = inject(ChangeDetectorRef);
  private readonly router           = inject(Router);
  private readonly host             = inject(ElementRef);

  // ── Chart refs ──────────────────────────────────────────────────────────────
  @ViewChild(SuccessRateChartComponent)
  private srChart?: SuccessRateChartComponent;

  // ── Page state ──────────────────────────────────────────────────────────────
  analytics: StudentAnalytics | null = null;
  isLoading = true;
  error: string | null = null;

  // Time-range toggle options
  readonly timeRanges: { label: string; value: '7d' | '30d' | '3m' }[] = [
    { label: '7 Days', value: '7d'  },
    { label: '30 Days', value: '30d' },
    { label: '3 Months', value: '3m' },
  ];
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

  // ── Focus-card stagger visibility (index → visible) ────────────────────────
  focusCardVisible: boolean[] = [];

  // ── Rec-card stagger visibility (index → visible) ──────────────────────────
  recCardVisible: boolean[] = [];

  // ── Hint countUp display values ─────────────────────────────────────────────
  displayedTotalHints      = 0;
  displayedAvgHints        = 0;   // stored ×10 so we can integer-animate, divide in template
  displayedSolvedClean     = 0;
  displayedSolvedAllHints  = 0;

  // ── Timer handles ───────────────────────────────────────────────────────────
  private counterIds:      (ReturnType<typeof setInterval> | null)[] = [];
  private dotTimerIds:     (ReturnType<typeof setTimeout>  | null)[] = [];
  private barTimerId:       ReturnType<typeof setTimeout>  | null    = null;
  private focusTimerIds:   (ReturnType<typeof setTimeout>  | null)[] = [];
  private recTimerIds:     (ReturnType<typeof setTimeout>  | null)[] = [];

  // ── IntersectionObserver for scroll-fade ────────────────────────────────────
  private sectionObserver?: IntersectionObserver;

  // Teardown
  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.load();
  }

  ngAfterViewInit(): void {
    this.initSectionObserver();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearAllCounters();
    this.clearAllDotTimers();
    this.clearFocusTimers();
    this.clearRecTimers();
    if (this.barTimerId !== null) clearTimeout(this.barTimerId);
    this.sectionObserver?.disconnect();
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
    this.focusCardVisible     = [];
    this.recCardVisible       = [];
    this.displayedTotalHints     = 0;
    this.displayedAvgHints       = 0;
    this.displayedSolvedClean    = 0;
    this.displayedSolvedAllHints = 0;
    this.clearAllCounters();
    this.clearAllDotTimers();
    this.clearFocusTimers();
    this.clearRecTimers();

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
          // Re-attach the scroll-fade observer now that content is in the DOM
          this.sectionObserver?.disconnect();
          this.initSectionObserver();
          // Small delay so the DOM paints the 0-width bars first,
          // then we set real widths and the CSS transition animates them
          this.barTimerId = setTimeout(() => {
            data.topics.forEach(t => {
              this.barWidths[t.topicId] = t.strengthScore;
            });
            this.cdr.detectChanges();
          }, 120);

          // Stagger focus cards in after bars animate (offset 300ms)
          const weak = this.weakSpotlightTopics;
          this.focusCardVisible = new Array(weak.length).fill(false);
          weak.forEach((_, i) => {
            const id = setTimeout(() => {
              this.focusCardVisible[i] = true;
              this.cdr.detectChanges();
            }, 300 + i * 120);
            this.focusTimerIds[i] = id;
          });

          // Stagger recommendation cards in (offset 400ms, 100ms between)
          this.recCardVisible = new Array(data.recommendations.length).fill(false);
          data.recommendations.forEach((_, i) => {
            const id = setTimeout(() => {
              this.recCardVisible[i] = true;
              this.cdr.detectChanges();
            }, 400 + i * 100);
            this.recTimerIds[i] = id;
          });
        },
        error: () => {
          this.error     = 'Could not load your progress. Please try again.';
          this.isLoading = false;
        },
      });
  }

  // ── Time-range toggle ───────────────────────────────────────────────────────

  setTimeRange(range: '7d' | '30d' | '3m'): void {
    if (this.activeTimeRange === range) return;
    this.activeTimeRange = range;
    // Re-render chart with smooth transition.
    // Currently all ranges share the same mock data; the backend will
    // return different slices per range once the endpoint is live.
    this.cdr.detectChanges();
    this.srChart?.refresh();
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
    const h = data.hintUsage;

    // Hero stats
    this.counterIds[0] = this.countUp(s.totalAttempted,       v => { this.displayedAttempted   = v; this.cdr.detectChanges(); });
    this.counterIds[1] = this.countUp(s.totalSolved,          v => { this.displayedSolved      = v; this.cdr.detectChanges(); });
    this.counterIds[2] = this.countUp(s.successRate,          v => { this.displayedSuccessRate = v; this.cdr.detectChanges(); });
    this.counterIds[3] = this.countUp(s.streak.currentStreak, v => { this.displayedStreak      = v; this.cdr.detectChanges(); });

    // Hint stats — avg stored ×10 so integer animation stays smooth
    this.counterIds[4] = this.countUp(h.totalHintsUsed,                  v => { this.displayedTotalHints     = v; this.cdr.detectChanges(); });
    this.counterIds[5] = this.countUp(Math.round(h.averageHintsPerProblem * 10), v => { this.displayedAvgHints = v; this.cdr.detectChanges(); });
    this.counterIds[6] = this.countUp(h.solvedWithZeroHints,             v => { this.displayedSolvedClean    = v; this.cdr.detectChanges(); });
    this.counterIds[7] = this.countUp(h.solvedUsingAllHints,             v => { this.displayedSolvedAllHints = v; this.cdr.detectChanges(); });

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

  // ── Weak spotlight ──────────────────────────────────────────────────────────

  /** Up to 3 weak topics, sorted weakest-first — drives the Focus Areas section. */
  get weakSpotlightTopics(): TopicPerformance[] {
    return [...(this.analytics?.topics ?? [])]
      .filter(t => t.strength === 'weak')
      .sort((a, b) => a.strengthScore - b.strengthScore)
      .slice(0, 3);
  }

  /**
   * Maps a topicName to a query-param slug for /problems?topic=...
   * Uses lowercase + hyphen, matching the Topic enum values already in the project.
   */
  practiceRoute(topic: TopicPerformance): void {
    const slug = topic.topicName.toLowerCase().replace(/\s+/g, '-');
    this.router.navigate(['/problems'], { queryParams: { topic: slug } });
  }

  // ── Cleanup helpers ──────────────────────────────────────────────────────────

  private clearFocusTimers(): void {
    this.focusTimerIds.forEach(id => { if (id !== null) clearTimeout(id); });
    this.focusTimerIds = [];
  }

  private clearRecTimers(): void {
    this.recTimerIds.forEach(id => { if (id !== null) clearTimeout(id); });
    this.recTimerIds = [];
  }

  private clearAllCounters(): void {
    this.counterIds.forEach(id => { if (id !== null) clearInterval(id); });
    this.counterIds = [];
  }

  private clearAllDotTimers(): void {
    this.dotTimerIds.forEach(id => { if (id !== null) clearTimeout(id); });
    this.dotTimerIds = [];
  }

  // ── Scroll-fade via IntersectionObserver ────────────────────────────────────

  /**
   * Observes every .section-fade element inside the host.
   * - Elements already visible on first paint get the class immediately (no flash).
   * - Elements that scroll into view get the class with a 100 ms stagger based
   *   on their order among the currently-intersecting batch.
   * - prefers-reduced-motion: all sections become visible instantly.
   */
  private initSectionObserver(): void {
    if (this.prefersReducedMotion()) {
      const all = this.host.nativeElement.querySelectorAll('.section-fade') as NodeListOf<HTMLElement>;
      all.forEach((el: HTMLElement) => el.classList.add('section-fade--visible'));
      return;
    }

    let batchDelay = 0;

    this.sectionObserver = new IntersectionObserver(
      (entries) => {
        let inView = 0;
        entries.forEach(entry => {
          if (!entry.isIntersecting) return;
          const el = entry.target as HTMLElement;
          const delay = batchDelay + inView * 100;
          inView++;
          setTimeout(() => el.classList.add('section-fade--visible'), delay);
          this.sectionObserver!.unobserve(el);
        });
        if (inView > 0) batchDelay = 0;
      },
      { threshold: 0.08 },
    );

    const sections = this.host.nativeElement.querySelectorAll('.section-fade') as NodeListOf<HTMLElement>;
    sections.forEach((el: HTMLElement) => this.sectionObserver!.observe(el));
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

  // ── Submissions helpers ──────────────────────────────────────────────────────

  submissionStatusClass(status: string): string {
    const map: Record<string, string> = {
      'Accepted':           'sub-status sub-status--accepted',
      'Wrong Answer':       'sub-status sub-status--wrong',
      'Runtime Error':      'sub-status sub-status--error',
      'Time Limit Exceeded':'sub-status sub-status--tle',
    };
    return map[status] ?? 'sub-status';
  }

  submissionStatusIcon(status: string): string {
    const map: Record<string, string> = {
      'Accepted':            '✓',
      'Wrong Answer':        '✗',
      'Runtime Error':       '⚡',
      'Time Limit Exceeded': '⏱',
    };
    return map[status] ?? '•';
  }

  difficultyBadgeClass(d: string): string {
    const cls = d.toLowerCase();
    return `badge badge--${cls}`;
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins  = Math.floor(diff / 60_000);
    if (mins < 1)    return 'just now';
    if (mins < 60)   return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs  < 24)   return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days === 1)  return 'Yesterday';
    if (days < 7)    return `${days} days ago`;
    const weeks = Math.floor(days / 7);
    if (weeks < 5)   return `${weeks}w ago`;
    const months = Math.floor(days / 30);
    if (months < 12) return `${months}mo ago`;
    return `${Math.floor(months / 12)}y ago`;
  }

  trackBySubmissionId(_i: number, s: { submissionId: string }): string {
    return s.submissionId;
  }

  // ── Recommendations helpers ──────────────────────────────────────────────────

  trackByProblemId(_i: number, r: { problemId: string }): string {
    return r.problemId;
  }
}
