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
    const diff  = Date.now() - new Date(iso).getTime();
    const mins  = Math.floor(diff / 60_000);
    if (mins < 1)    return 'just now';
    if (mins < 60)   return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)    return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days === 1)  return 'Yesterday';
    if (days < 7)    return `${days} days ago`;
    const weeks = Math.floor(days / 7);
    if (weeks < 5)   return `${weeks}w ago`;
    const months = Math.floor(days / 30);
    if (months < 12) return `${months}mo ago`;
    return `${Math.floor(months / 12)}y ago`;
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
