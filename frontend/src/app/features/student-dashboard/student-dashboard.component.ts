/**
 * StudentDashboardComponent
 *
 * Personal learning dashboard — a single long scrollable page showing:
 *   • Headline stats (streak, problems solved, avg score, acceptance rate)
 *   • Hint budget progress bar
 *   • Topic mastery bars (strong / weak breakdown)
 *   • Score trend line chart (Chart.js, last 30 days)
 *   • Weekly activity bar chart (Chart.js, last 4 weeks)
 *   • Personalized problem recommendations (from Analytics Agent)
 *
 * All data flows from AnalyticsService. Loading is simulated with delay(1200)
 * on every mock observable, matching the real API latency budget.
 */

import {
  Component,
  OnInit,
  OnDestroy,
  inject,
  ElementRef,
  ViewChild,
  AfterViewInit,
  ChangeDetectorRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { Chart, registerables } from 'chart.js';

import { AnalyticsService } from '../../core/services/analytics.service';
import { AuthService }      from '../../core/services/auth.service';
import {
  StudentDashboardData,
  TopicStat,
  WeeklyActivity,
  ScorePoint,
  RecommendedProblem,
} from '../../core/models/analytics.model';

// Register all Chart.js components once at module level
Chart.register(...registerables);

@Component({
  selector: 'app-student-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './student-dashboard.component.html',
  styleUrl: './student-dashboard.component.scss',
})
export class StudentDashboardComponent implements OnInit, AfterViewInit, OnDestroy {
  // ── DI ─────────────────────────────────────────────────────────────────────
  private readonly analyticsService = inject(AnalyticsService);
  readonly authService              = inject(AuthService);
  private readonly cdr              = inject(ChangeDetectorRef);

  // ── Chart canvas refs ───────────────────────────────────────────────────────
  @ViewChild('scoreCanvas')    scoreCanvasRef!:    ElementRef<HTMLCanvasElement>;
  @ViewChild('activityCanvas') activityCanvasRef!: ElementRef<HTMLCanvasElement>;

  // ── State ───────────────────────────────────────────────────────────────────
  loading      = true;
  error        = '';
  data: StudentDashboardData | null = null;

  // Chart instances (kept for destroy)
  private scoreChart?:    Chart;
  private activityChart?: Chart;

  // Teardown
  private readonly destroy$ = new Subject<void>();

  // ── Lifecycle ───────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.analyticsService
      .getDashboard()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next:  (d) => {
          this.data    = d;
          this.loading = false;
          this.cdr.detectChanges(); // ensure *ngIf resolves before we touch canvas
          this.buildCharts();
        },
        error: () => {
          this.error   = 'Could not load your dashboard. Please try refreshing.';
          this.loading = false;
        },
      });
  }

  ngAfterViewInit(): void {
    // Charts are built in ngOnInit after data arrives + detectChanges()
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.scoreChart?.destroy();
    this.activityChart?.destroy();
  }

  // ── Computed helpers ────────────────────────────────────────────────────────

  get strongTopics(): TopicStat[] {
    return (this.data?.topicStats ?? [])
      .filter(t => t.percentage >= 65)
      .sort((a, b) => b.percentage - a.percentage);
  }

  get weakTopics(): TopicStat[] {
    return (this.data?.topicStats ?? [])
      .filter(t => t.percentage < 65)
      .sort((a, b) => a.percentage - b.percentage);
  }

  get hintFraction(): number {
    const s = this.data?.summary;
    if (!s || s.hintsLimit === 0) return 0;
    return (s.hintsUsedToday / s.hintsLimit) * 100;
  }

  difficultyClass(d: RecommendedProblem['difficulty']): string {
    return `badge badge--${d}`;
  }

  trendIcon(trend: TopicStat['trend']): string {
    return trend === 'up' ? '↑' : trend === 'down' ? '↓' : '→';
  }

  trendClass(trend: TopicStat['trend']): string {
    return trend === 'up'
      ? 'trend-up'
      : trend === 'down'
      ? 'trend-down'
      : 'trend-flat';
  }

  trackById(_i: number, item: { id: string }): string {
    return item.id;
  }

  trackByTopic(_i: number, item: TopicStat): string {
    return item.topic;
  }

  // ── Chart construction ──────────────────────────────────────────────────────

  private buildCharts(): void {
    if (!this.data) return;
    this.buildScoreChart(this.data.scoreHistory);
    this.buildActivityChart(this.data.weeklyActivity);
  }

  private buildScoreChart(history: ScorePoint[]): void {
    const canvas = this.scoreCanvasRef?.nativeElement;
    if (!canvas) return;

    this.scoreChart?.destroy();

    // Use CSS variables resolved from the document root
    const style  = getComputedStyle(document.documentElement);
    const blue   = style.getPropertyValue('--blue').trim();
    const navy   = style.getPropertyValue('--navy').trim();
    const border = style.getPropertyValue('--border').trim();
    const muted  = style.getPropertyValue('--muted').trim();

    this.scoreChart = new Chart(canvas, {
      type: 'line',
      data: {
        labels: history.map(p => this.shortDate(p.date)),
        datasets: [
          {
            label: 'Avg Score',
            data:  history.map(p => p.score),
            borderColor:     blue,
            backgroundColor: blue + '22',
            fill:            true,
            tension:         0.4,
            pointRadius:     2,
            pointHoverRadius: 5,
            borderWidth:     2,
          },
        ],
      },
      options: {
        responsive:          true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend:  { display: false },
          tooltip: {
            backgroundColor: navy,
            titleFont:  { family: 'DM Sans', size: 12 },
            bodyFont:   { family: 'DM Sans', size: 13 },
            padding:    10,
            callbacks:  { label: ctx => ` Score: ${ctx.parsed.y}` },
          },
        },
        scales: {
          x: {
            ticks: {
              color:      muted,
              font:       { family: 'DM Sans', size: 10 },
              maxTicksLimit: 8,
              maxRotation: 0,
            },
            grid: { color: border },
          },
          y: {
            min: 0,
            max: 100,
            ticks: {
              color: muted,
              font:  { family: 'DM Sans', size: 10 },
              callback: v => `${v}`,
            },
            grid: { color: border },
          },
        },
      },
    });
  }

  private buildActivityChart(activity: WeeklyActivity[]): void {
    const canvas = this.activityCanvasRef?.nativeElement;
    if (!canvas) return;

    this.activityChart?.destroy();

    const style  = getComputedStyle(document.documentElement);
    const teal   = style.getPropertyValue('--teal').trim();
    const blue   = style.getPropertyValue('--blue').trim();
    const navy   = style.getPropertyValue('--navy').trim();
    const border = style.getPropertyValue('--border').trim();
    const muted  = style.getPropertyValue('--muted').trim();

    this.activityChart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: activity.map(a => this.shortDate(a.date)),
        datasets: [
          {
            label:           'Solved',
            data:            activity.map(a => a.solved),
            backgroundColor: teal + 'CC',
            borderRadius:    4,
            borderSkipped:   false,
          },
          {
            label:           'Attempted',
            data:            activity.map(a => a.attempted),
            backgroundColor: blue + '44',
            borderRadius:    4,
            borderSkipped:   false,
          },
        ],
      },
      options: {
        responsive:          true,
        maintainAspectRatio: false,
        interaction: { mode: 'index', intersect: false },
        plugins: {
          legend: {
            position: 'top',
            align:    'end',
            labels:   {
              font:        { family: 'DM Sans', size: 12 },
              color:       navy,
              boxWidth:    12,
              boxHeight:   12,
              borderRadius: 3,
              padding:     16,
            },
          },
          tooltip: {
            backgroundColor: navy,
            titleFont:  { family: 'DM Sans', size: 12 },
            bodyFont:   { family: 'DM Sans', size: 13 },
            padding:    10,
          },
        },
        scales: {
          x: {
            ticks: {
              color:         muted,
              font:          { family: 'DM Sans', size: 10 },
              maxTicksLimit: 14,
              maxRotation:   0,
            },
            grid: { display: false },
          },
          y: {
            min: 0,
            ticks: {
              color:     muted,
              font:      { family: 'DM Sans', size: 10 },
              stepSize:  1,
            },
            grid: { color: border },
          },
        },
      },
    });
  }

  // ── Formatting helpers ──────────────────────────────────────────────────────

  private shortDate(iso: string): string {
    const d = new Date(iso);
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }
}
