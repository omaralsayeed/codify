import {
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivityDay } from '../../core/models/analytics.model';

interface HeatCell {
  date: string;
  count: number;
  level: 0 | 1 | 2 | 3 | 4;   // 0 = none, 4 = most
  label: string;               // tooltip text
}

@Component({
  selector: 'app-activity-heatmap',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="heatmap-wrap">

      <!-- Month labels -->
      <div class="heatmap-months" aria-hidden="true">
        @for (m of monthLabels; track m.label + m.col) {
          <span class="heatmap-month" [style.grid-column]="m.col">{{ m.label }}</span>
        }
      </div>

      <!-- Day-of-week labels -->
      <div class="heatmap-days" aria-hidden="true">
        <span></span>
        <span>Mon</span>
        <span></span>
        <span>Wed</span>
        <span></span>
        <span>Fri</span>
        <span></span>
      </div>

      <!-- Grid -->
      <div class="heatmap-grid"
           role="grid"
           [attr.aria-label]="'Activity heatmap, ' + totalActive + ' active days in the past year'">
        @for (cell of cells; track cell.date) {
          <span class="heatmap-cell heatmap-cell--{{ cell.level }}"
                role="gridcell"
                [attr.aria-label]="cell.label"
                [attr.title]="cell.label">
          </span>
        }
      </div>

      <!-- Legend -->
      <div class="heatmap-legend" aria-hidden="true">
        <span class="heatmap-legend__label">Less</span>
        <span class="heatmap-cell heatmap-cell--0"></span>
        <span class="heatmap-cell heatmap-cell--1"></span>
        <span class="heatmap-cell heatmap-cell--2"></span>
        <span class="heatmap-cell heatmap-cell--3"></span>
        <span class="heatmap-cell heatmap-cell--4"></span>
        <span class="heatmap-legend__label">More</span>
      </div>

    </div>
  `,
  styles: [`
    .heatmap-wrap {
      display: flex;
      flex-direction: column;
      gap: 4px;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
      padding-bottom: 4px;
    }

    /* Month row — 53 columns (one per week) */
    .heatmap-months {
      display: grid;
      grid-template-columns: repeat(53, var(--cell));
      padding-left: 28px;   /* offset for day labels */
    }

    .heatmap-month {
      font-family: var(--ff-body);
      font-size: 11px;
      color: var(--muted);
      white-space: nowrap;
    }

    /* Day-of-week labels */
    .heatmap-days {
      display: grid;
      grid-template-rows: repeat(7, var(--cell));
      gap: var(--gap);
      width: 24px;
      float: left;
      margin-right: 4px;
      font-family: var(--ff-body);
      font-size: 9px;
      color: var(--muted);
      align-items: center;
    }

    /* Main grid — 53 cols × 7 rows, written column-first */
    .heatmap-grid {
      display: grid;
      grid-template-columns: repeat(53, var(--cell));
      grid-template-rows: repeat(7, var(--cell));
      grid-auto-flow: column;
      gap: var(--gap);
      padding-left: 28px;
    }

    /* Individual cell */
    .heatmap-cell {
      width: var(--cell);
      height: var(--cell);
      border-radius: var(--cell-radius);
      display: block;
      cursor: default;
      transition: opacity 0.1s;

      &:hover { opacity: 0.8; }

      /* Levels — 0 = empty grey, 1–4 = progressively darker green */
      &--0 { background: var(--ivory2); }
      &--1 { background: #9be9a8; }
      &--2 { background: #40c463; }
      &--3 { background: #30a14e; }
      &--4 { background: #216e39; }
    }

    /* Legend */
    .heatmap-legend {
      display: flex;
      align-items: center;
      gap: 3px;
      justify-content: flex-end;
      padding-right: 2px;
      margin-top: 4px;
    }

    .heatmap-legend__label {
      font-family: var(--ff-body);
      font-size: 11px;
      color: var(--muted);
      margin: 0 4px;
    }
  `],
})
export class ActivityHeatmapComponent implements OnChanges {
  @Input() days: ActivityDay[] = [];

  cells: HeatCell[] = [];
  monthLabels: { label: string; col: number }[] = [];
  totalActive = 0;

  // CSS custom properties — set on the host
  private readonly CELL    = '11px';
  private readonly GAP     = '3px';
  private readonly RADIUS  = '2px';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['days'] && this.days.length) {
      this.buildGrid();
    }
  }

  private buildGrid(): void {
    // Pad to exactly 371 cells (53 weeks × 7) so the grid fills evenly.
    // The grid always ends on today; pad the front with empty cells to
    // align to Sunday (col 0 = Sunday in most calendar conventions).
    const raw = [...this.days].slice(-365);

    // Find which weekday the first day falls on (0=Sun…6=Sat)
    const firstDate = new Date(raw[0].date + 'T00:00:00');
    const startPad  = firstDate.getDay(); // 0–6

    const padded: (ActivityDay | null)[] = [
      ...Array(startPad).fill(null),
      ...raw,
    ];

    // Trim/pad tail to complete the last week
    while (padded.length % 7 !== 0) padded.push(null);

    this.totalActive = raw.filter(d => d.count > 0).length;

    this.cells = padded.map(d => {
      if (!d) return { date: '', count: 0, level: 0 as const, label: '' };
      const level = this.countToLevel(d.count);
      const label = d.count === 0
        ? `No submissions on ${this.formatDate(d.date)}`
        : `${d.count} submission${d.count > 1 ? 's' : ''} on ${this.formatDate(d.date)}`;
      return { date: d.date, count: d.count, level, label };
    });

    // Build month labels — one label per new month, col = week index
    this.monthLabels = [];
    const seen = new Set<string>();
    padded.forEach((d, idx) => {
      if (!d) return;
      const weekCol = Math.floor(idx / 7) + 1; // 1-based for CSS grid-column
      const key = d.date.slice(0, 7); // YYYY-MM
      if (!seen.has(key)) {
        seen.add(key);
        const dt = new Date(d.date + 'T00:00:00');
        this.monthLabels.push({
          label: dt.toLocaleDateString('en-US', { month: 'short' }),
          col: weekCol,
        });
      }
    });
  }

  private countToLevel(count: number): 0 | 1 | 2 | 3 | 4 {
    if (count === 0) return 0;
    if (count === 1) return 1;
    if (count === 2) return 2;
    if (count === 3) return 3;
    return 4;
  }

  private formatDate(iso: string): string {
    const d = new Date(iso + 'T00:00:00');
    return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }
}
