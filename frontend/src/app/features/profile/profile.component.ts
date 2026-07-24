import {
  Component,
  OnInit,
  OnDestroy,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';

import { AnalyticsService }  from '../../core/services/analytics.service';
import {
  PublicProfileData,
  TopicPerformance,
  TopicStrength,
  RecentSubmission,
  LanguageStat,
} from '../../core/models/analytics.model';
import { ActivityHeatmapComponent } from './activity-heatmap.component';
import { SolvedRingComponent, RingDifficultyData } from './solved-ring.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, ActivityHeatmapComponent, SolvedRingComponent],
  templateUrl: './profile.component.html',
  styleUrl:    './profile.component.scss',
})
export class ProfileComponent implements OnInit, OnDestroy {
  private readonly analyticsService = inject(AnalyticsService);
  private readonly route            = inject(ActivatedRoute);

  profile: PublicProfileData | null = null;
  isLoading = false;   // sync mock — no loading state needed
  error: string | null = null;

  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    const username = this.route.snapshot.paramMap.get('username') ?? '';
    this.load(username);
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  load(username: string): void {
    this.isLoading = false;  // sync — skip skeleton entirely for mock
    this.error     = null;

    this.analyticsService
      .getPublicProfile(username)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next:  p => { this.profile = p; },
        error: () => { this.error = 'Could not load profile.'; },
      });
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  avatarColor(initials: string): string {
    // Deterministic bg color from initials — picks from a small palette
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

  statusClass(status: RecentSubmission['status']): string {
    const map: Record<string, string> = {
      'Accepted':            'sub-status sub-status--accepted',
      'Wrong Answer':        'sub-status sub-status--wrong',
      'Runtime Error':       'sub-status sub-status--error',
      'Time Limit Exceeded': 'sub-status sub-status--tle',
    };
    return map[status] ?? 'sub-status';
  }

  relativeTime(iso: string): string {
    const date = new Date(iso);
    const diff  = Date.now() - date.getTime();
    const mins  = Math.floor(diff / 60_000);
    if (mins < 1)   return 'just now';
    if (mins < 60)  return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)   return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days === 1) return 'Yesterday';
    if (days < 7)   return `${days} days ago`;
    // Older than a week — show "Jul 12" or "Jul 12, 2024" if different year
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

  difficultyPercent(solved: number, total: number): number {
    return total === 0 ? 0 : Math.round((solved / total) * 100);
  }

  trackByDate(_i: number, item: { date: string }): string {
    return item.date;
  }

  trackBySubmissionId(_i: number, s: RecentSubmission): string {
    return s.submissionId;
  }

  trackByTopic(_i: number, t: TopicPerformance): string {
    return t.topicId;
  }

  // ── Topic groups ─────────────────────────────────────────────────────────

  get strongTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'strong') ?? [];
  }

  get averageTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'average') ?? [];
  }

  get weakTopics(): TopicPerformance[] {
    return this.profile?.topicStats.filter(t => t.strength === 'weak') ?? [];
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

  // ── Streak helpers ───────────────────────────────────────────────────────

  get isPersonalBest(): boolean {
    if (!this.profile) return false;
    return this.profile.streak.currentStreak > 0 &&
           this.profile.streak.currentStreak >= this.profile.streak.longestStreak;
  }

  // ── Solved ring data ─────────────────────────────────────────────────────

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
}
