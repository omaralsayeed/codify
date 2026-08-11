import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  signal,
  ElementRef,
  ChangeDetectorRef,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { AnalyticsService } from '../../core/services/analytics.service';
import { AuthService }      from '../../core/services/auth.service';
import {
  PublicProfileData,
  ActivityDay,
  TopicPerformance,
  TopicStrength,
  RecentSubmission,
} from '../../core/models/analytics.model';
import { ActivityHeatmapComponent } from './activity-heatmap.component';
import { SolvedRingComponent, RingDifficultyData } from './solved-ring.component';
import {
  buildHeatmapGrid,
  activityDaysToMap,
  HeatmapGrid,
} from '../../utils/heatmap-calendar.util';

/** Mirrors the slug function in app.routes.ts */
function toSlug(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, '_');
}

/** Eases a 0→1 progress value with cubic ease-out */
function easeOut(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

@Component({
  selector: 'app-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, RouterLink, ActivityHeatmapComponent, SolvedRingComponent],
  templateUrl: './profile.component.html',
  styleUrl:    './profile.component.scss',
  host: {
    '[class.no-anim]': 'reducedMotion',
  },
})
export class ProfileComponent implements OnInit, OnDestroy {
  private readonly analyticsService = inject(AnalyticsService);
  private readonly authService      = inject(AuthService);
  private readonly route            = inject(ActivatedRoute);
  private readonly el               = inject(ElementRef);
  private readonly cdr              = inject(ChangeDetectorRef);

  profile: PublicProfileData | null = null;
  isLoading = false;
  error: string | null = null;

  /** Profile photo saved during registration (base64 from localStorage) */
  savedAvatar: string | null = null;

  // ── Reduced motion ────────────────────────────────────────────────────────
  reducedMotion = false;

  // ── Heatmap state ─────────────────────────────────────────────────────────
  /** 'rolling' = ALL tab; 'year' = a specific year tab */
  activeMode: 'year' | 'rolling' = 'year';
  /** The year currently selected; null when activeMode === 'rolling' */
  activeYear: number | null = null;
  /** Built once from activityGrid; never mutated after that */
  private submissionMap: Map<string, number> = new Map();
  /** The pre-built grid passed down to the heatmap component */
  grid: HeatmapGrid | null = null;
  /** Year tabs shown in the UI — derived from data, current year always present */
  availableYears: number[] = [];

  // ── Animated display values ───────────────────────────────────────────────
  displaySolved     = signal(0);
  displayStreak     = signal(0);
  displayActiveDays = signal(0);

  private countsAnimated = false;
  private intersectionObs: IntersectionObserver | null = null;
  private animTimers: ReturnType<typeof setTimeout>[] = [];

  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.reducedMotion =
      window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    // Load saved profile photo (set during registration)
    this.savedAvatar = localStorage.getItem('codify_avatar');

    const username = this.route.snapshot.paramMap.get('username') ?? '';
    this.load(username);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.intersectionObs?.disconnect();
    this.animTimers.forEach(t => clearTimeout(t));
  }

  load(username: string): void {
    this.isLoading = false;
    this.error     = null;

    this.analyticsService
      .getPublicProfile(username)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: p => {
          this.profile = p;

          // Build the submission map once — all grid rebuilds read from this
          this.submissionMap = activityDaysToMap(p.activityGrid);

          // Derive year tabs from the actual data + always include current year
          this.availableYears = this.computeAvailableYears();

          // Default: most recent year tab (current year if data exists)
          const currentYear = new Date().getFullYear();
          this.activeMode = 'year';
          this.activeYear = this.availableYears.includes(currentYear)
            ? currentYear
            : (this.availableYears[this.availableYears.length - 1] ?? currentYear);

          this.rebuildGrid();
          this.cdr.markForCheck();

          this.animTimers.push(setTimeout(() => this.setupCountup(p), 0));
        },
        error: () => {
          this.error = 'Could not load profile.';
          this.cdr.markForCheck();
        },
      });
  }

  // ── Year / mode selection ─────────────────────────────────────────────────

  selectYear(year: number | null): void {
    if (year === null) {
      // null = ALL tab = rolling mode
      this.activeMode = 'rolling';
      this.activeYear = null;
    } else {
      this.activeMode = 'year';
      this.activeYear = year;
    }
    this.rebuildGrid();
    this.cdr.markForCheck();
  }

  /** Convenience getter so the template can express "is ALL active" */
  get selectedYear(): number | null {
    return this.activeMode === 'rolling' ? null : this.activeYear;
  }

  // ── Grid builder ──────────────────────────────────────────────────────────

  private rebuildGrid(): void {
    this.grid = buildHeatmapGrid(
      this.activeMode,
      this.activeYear,
      this.submissionMap,
      new Date(),
    );
  }

  // ── Stats helpers (read from grid) ───────────────────────────────────────

  get heatmapSubmissions(): number {
    return this.grid?.totalSubmissions ?? 0;
  }

  get heatmapActiveDays(): number {
    return this.grid?.activeDays ?? 0;
  }

  get heatmapMaxStreak(): number {
    return this.grid?.maxStreak ?? 0;
  }

  get statsLabel(): string {
    return this.activeMode === 'rolling' ? 'last 52 weeks' : String(this.activeYear);
  }

  // ── Year tab derivation ───────────────────────────────────────────────────
  /**
   * Collect distinct years from the submission map.
   * Always include the current year even if there are no submissions yet.
   */
  private computeAvailableYears(): number[] {
    const currentYear = new Date().getFullYear();
    const years = new Set<number>([currentYear]);
    for (const iso of this.submissionMap.keys()) {
      years.add(parseInt(iso.slice(0, 4), 10));
    }
    return Array.from(years).sort((a, b) => a - b);
  }

  // ── Countup ───────────────────────────────────────────────────────────────

  private setupCountup(p: PublicProfileData): void {
    if (this.reducedMotion) {
      this.displaySolved.set(p.totalSolved);
      this.displayStreak.set(p.streak.currentStreak);
      this.displayActiveDays.set(p.streak.totalActiveDays);
      return;
    }

    const statsCard = (this.el.nativeElement as HTMLElement)
      .querySelector('.stats-card');
    if (!statsCard) return;

    this.intersectionObs = new IntersectionObserver(
      (entries) => {
        if (!entries[0].isIntersecting || this.countsAnimated) return;
        this.countsAnimated = true;
        this.intersectionObs?.disconnect();

        this.animateCount(0, p.totalSolved,            1000, v => this.displaySolved.set(v));
        this.animateCount(0, p.streak.currentStreak,   1000, v => this.displayStreak.set(v));
        this.animateCount(0, p.streak.totalActiveDays, 1000, v => this.displayActiveDays.set(v));
      },
      { threshold: 0.3 },
    );
    this.intersectionObs.observe(statsCard);
  }

  private animateCount(
    from: number,
    to: number,
    duration: number,
    setter: (v: number) => void,
  ): void {
    if (to === from) { setter(to); return; }

    const start = performance.now();

    const tick = (now: number) => {
      const elapsed  = now - start;
      const progress = Math.min(elapsed / duration, 1);
      setter(Math.round(from + (to - from) * easeOut(progress)));
      this.cdr.markForCheck();
      if (progress < 1) requestAnimationFrame(tick);
    };

    requestAnimationFrame(tick);
  }

  // ── Own-profile detection ─────────────────────────────────────────────────

  get isOwnProfile(): boolean {
    const u = this.authService.currentUser();
    if (!u || !this.profile) return false;
    return toSlug(u.name) === this.profile.user.username;
  }

  // ── Topic groups ──────────────────────────────────────────────────────────

  get strongTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'strong') ?? [];
  }

  get averageTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'average') ?? [];
  }

  // ── Language bar width ────────────────────────────────────────────────────

  private get maxLangSolved(): number {
    if (!this.profile?.languageStats.length) return 1;
    return Math.max(...this.profile.languageStats.map(l => l.solved));
  }

  langBarWidth(solved: number): number {
    const max = this.maxLangSolved;
    return max === 0 ? 0 : Math.round((solved / max) * 100);
  }

  // ── Difficulty bar width ──────────────────────────────────────────────────

  diffBarWidth(solved: number, total: number): number {
    return total === 0 ? 0 : Math.round((solved / total) * 100);
  }

  // ── Streak helpers ────────────────────────────────────────────────────────

  get isPersonalBest(): boolean {
    if (!this.profile) return false;
    return this.profile.streak.currentStreak > 0 &&
           this.profile.streak.currentStreak >= this.profile.streak.longestStreak;
  }

  // ── Solved ring data ──────────────────────────────────────────────────────

  get ringData(): RingDifficultyData | null {
    if (!this.profile) return null;
    const p = this.profile;
    return {
      easySolved:       p.difficultyBreakdown.easy,
      mediumSolved:     p.difficultyBreakdown.medium,
      hardSolved:       p.difficultyBreakdown.hard,
      easyTotal:        p.difficultyTotals.easy,
      mediumTotal:      p.difficultyTotals.medium,
      hardTotal:        p.difficultyTotals.hard,
      totalSolved:      p.totalSolved,
      totalAttempted:   p.totalAttempted,
      acceptanceRate:   p.successRate,
      totalSubmissions: p.streak.totalSubmissionsLastYear,
    };
  }

  // ── Misc helpers ──────────────────────────────────────────────────────────

  avatarColor(initials: string): string {
    const palette = ['#2E86AB', '#1D9E75', '#C8A951', '#7B1FA2', '#E65100'];
    const idx = (initials.charCodeAt(0) + (initials.charCodeAt(1) || 0)) % palette.length;
    return palette[idx];
  }

  topicBadgeClass(strength: TopicStrength): string {
    return `topic-badge topic-badge--${strength}`;
  }

  difficultyClass(d: string): string {
    return `badge badge--${d.toLowerCase()}`;
  }

  relativeTime(iso: string): string {
    const date  = new Date(iso);
    const diff  = Date.now() - date.getTime();
    const mins  = Math.floor(diff / 60_000);
    if (mins < 1)   return 'just now';
    if (mins < 60)  return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)   return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days === 1) return 'Yesterday';
    if (days < 7)   return `${days} days ago`;
    const isCurrentYear = date.getFullYear() === new Date().getFullYear();
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day:   'numeric',
      ...(isCurrentYear ? {} : { year: 'numeric' }),
    });
  }

  joinedYear(iso: string): string {
    return new Date(iso).getFullYear().toString();
  }

  trackBySubmissionId(_i: number, s: RecentSubmission): string {
    return s.submissionId;
  }

  trackByTopic(_i: number, t: TopicPerformance): string {
    return t.topicId;
  }

  subRowDelay(index: number): string {
    return `${index * 35}ms`;
  }

  /** 53 weeks × 7 days skeleton cells for the heatmap loading state */
  readonly skCells = Array(53 * 7);
}
