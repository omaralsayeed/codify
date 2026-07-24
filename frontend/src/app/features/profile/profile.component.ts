import {
  Component,
  OnInit,
  OnDestroy,
  inject,
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

/** Mirrors the slug function in app.routes.ts */
function toSlug(name: string): string {
  return name.trim().toLowerCase().replace(/\s+/g, '_');
}

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, ActivityHeatmapComponent, SolvedRingComponent],
  templateUrl: './profile.component.html',
  styleUrl:    './profile.component.scss',
})
export class ProfileComponent implements OnInit, OnDestroy {
  private readonly analyticsService = inject(AnalyticsService);
  private readonly authService      = inject(AuthService);
  private readonly route            = inject(ActivatedRoute);

  profile: PublicProfileData | null = null;
  isLoading = false;
  error: string | null = null;

  // ── Year filter ───────────────────────────────────────────────────────────────
  /** null = "All years" */
  selectedYear: number | null = null;
  availableYears: number[] = [];

  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ────────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const username = this.route.snapshot.paramMap.get('username') ?? '';
    this.load(username);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(username: string): void {
    this.isLoading = false;
    this.error     = null;

    this.analyticsService
      .getPublicProfile(username)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: p => {
          this.profile        = p;
          this.availableYears = this.computeAvailableYears(p.activityGrid);
          // Default to current calendar year
          const currentYear = new Date().getFullYear();
          this.selectedYear = this.availableYears.includes(currentYear)
            ? currentYear
            : (this.availableYears[this.availableYears.length - 1] ?? null);
        },
        error: () => { this.error = 'Could not load profile.'; },
      });
  }

  selectYear(year: number | null): void {
    this.selectedYear = year;
  }

  // ── Year filter helpers ───────────────────────────────────────────────────────

  private computeAvailableYears(grid: ActivityDay[]): number[] {
    const years = new Set<number>();
    for (const d of grid) years.add(parseInt(d.date.slice(0, 4), 10));
    return Array.from(years).sort((a, b) => a - b);
  }

  get filteredDays(): ActivityDay[] {
    if (!this.profile) return [];
    if (this.selectedYear === null) return this.profile.activityGrid;
    return this.profile.activityGrid.filter(d => d.date.startsWith(String(this.selectedYear)));
  }

  get filteredSubmissions(): number {
    return this.filteredDays.reduce((s, d) => s + d.count, 0);
  }

  get filteredActiveDays(): number {
    return this.filteredDays.filter(d => d.count > 0).length;
  }

  get filteredMaxStreak(): number {
    let max = 0, run = 0;
    for (const d of this.filteredDays) {
      if (d.count > 0) { run++; max = Math.max(max, run); }
      else run = 0;
    }
    return max;
  }

  get statsLabel(): string {
    return this.selectedYear === null ? 'all time' : String(this.selectedYear);
  }

  // ── Own-profile detection ────────────────────────────────────────────────────

  get isOwnProfile(): boolean {
    const u = this.authService.currentUser();
    if (!u || !this.profile) return false;
    return toSlug(u.name) === this.profile.user.username;
  }

  // ── Topic groups ─────────────────────────────────────────────────────────────

  get strongTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'strong') ?? [];
  }

  get averageTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'average') ?? [];
  }

  // ── Language bar width ────────────────────────────────────────────────────────

  private get maxLangSolved(): number {
    if (!this.profile?.languageStats.length) return 1;
    return Math.max(...this.profile.languageStats.map(l => l.solved));
  }

  langBarWidth(solved: number): number {
    const max = this.maxLangSolved;
    return max === 0 ? 0 : Math.round((solved / max) * 100);
  }

  // ── Difficulty bar width ──────────────────────────────────────────────────────

  diffBarWidth(solved: number, total: number): number {
    return total === 0 ? 0 : Math.round((solved / total) * 100);
  }

  // ── Streak helpers ────────────────────────────────────────────────────────────

  get isPersonalBest(): boolean {
    if (!this.profile) return false;
    return this.profile.streak.currentStreak > 0 &&
           this.profile.streak.currentStreak >= this.profile.streak.longestStreak;
  }

  // ── Solved ring data ──────────────────────────────────────────────────────────

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

  // ── Misc helpers ──────────────────────────────────────────────────────────────

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
}
