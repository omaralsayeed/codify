import {
  Component,
  Input,
  OnChanges,
  SimpleChanges,
  ChangeDetectionStrategy,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivityDay } from '../../core/models/analytics.model';

// ── Cell size constants — kept in sync with the CSS vars set by the parent ────
const CELL_PX = 11;
const GAP_PX  = 2;

interface HeatCell {
  date:    string;
  count:   number;
  level:   0 | 1 | 2 | 3 | 4;
  /** Full human-readable label used for aria-label and tooltip text */
  label:   string;
  /** Column index (0-based) in the padded grid — used for tooltip x */
  col:     number;
  /** Row index (0-based) — used for tooltip y */
  row:     number;
}

interface Tooltip {
  text:  string;
  /** px from left edge of the grid scroll area */
  x:     number;
  /** px from top of the grid area */
  y:     number;
}

@Component({
  selector: 'app-activity-heatmap',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Outer scroll wrapper — overflows on mobile -->
    <div class="heatmap-scroll" #scrollWrap>

      <!-- Fixed-width inner that never reflows -->
      <div class="heatmap-inner"
           [style.--heatmap-cols]="totalCols">

        <!-- Month labels row -->
        <div class="heatmap-months" aria-hidden="true">
          <!-- 28px left gutter for day labels -->
          <div class="heatmap-months__gutter"></div>
          <div class="heatmap-months__labels">
            @for (m of monthLabels; track m.label + m.weekCol) {
              <span
                class="heatmap-month"
                [style.grid-column]="m.weekCol">{{ m.label }}</span>
            }
          </div>
        </div>

        <!-- Body row: day labels + grid + tooltip host -->
        <div class="heatmap-body">

          <!-- Day-of-week labels (Mon/Wed/Fri only) -->
          <div class="heatmap-days" aria-hidden="true">
            <span></span>          <!-- Sun -->
            <span>Mon</span>
            <span></span>          <!-- Tue -->
            <span>Wed</span>
            <span></span>          <!-- Thu -->
            <span>Fri</span>
            <span></span>          <!-- Sat -->
          </div>

          <!-- Grid + tooltip in a positioned container -->
          <div class="heatmap-grid-wrap"
               (mouseleave)="hideTooltip()">

            <!-- Cell grid -->
            <div class="heatmap-grid"
                 role="grid"
                 [attr.aria-label]="'Activity heatmap, ' + totalActive + ' active days in the past year'">
              @for (cell of cells; track cell.date || ($index + '__pad')) {
                <span
                  class="heatmap-cell"
                  [class]="'heatmap-cell heatmap-cell--' + cell.level + (hoveredCell === cell ? ' heatmap-cell--active' : '')"
                  role="gridcell"
                  [attr.aria-label]="cell.label || null"
                  (mouseenter)="showTooltip(cell)"
                  (mouseleave)="hideTooltip()">
                </span>
              }
            </div>

            <!-- Tooltip -->
            @if (tooltip()) {
              <div class="heatmap-tooltip"
                   role="tooltip"
                   [style.left.px]="tooltip()!.x"
                   [style.top.px]="tooltip()!.y">
                {{ tooltip()!.text }}
              </div>
            }

          </div><!-- /heatmap-grid-wrap -->
        </div><!-- /heatmap-body -->

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

      </div><!-- /heatmap-inner -->
    </div><!-- /heatmap-scroll -->
  `,
  styles: [`
    /* ── Scroll container ──────────────────────────────────────────── */
    .heatmap-scroll {
      /* Takes only as much width as the grid needs on desktop.
         On mobile the parent is narrower so overflow-x kicks in. */
      display: block;
      width: fit-content;
      max-width: 100%;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
      padding-bottom: 4px;
    }

    /* Fixed-width inner so the grid never reflows */
    .heatmap-inner {
      display: flex;
      flex-direction: column;
      gap: 4px;
      width: fit-content;   /* shrink-wraps to exact grid width — no trailing gap */
    }

    /* ── Month label row ───────────────────────────────────────────── */
    .heatmap-months {
      display: flex;
      align-items: flex-end;
    }

    .heatmap-months__gutter {
      width: 28px;
      flex-shrink: 0;
    }

    /* Sits over the grid columns — same template-columns as the grid */
    .heatmap-months__labels {
      display: grid;
      grid-template-columns: repeat(var(--heatmap-cols, 53), 11px);
      gap: 2px;
    }

    .heatmap-month {
      font-family: var(--ff-body);
      font-size: 10px;
      color: var(--muted);
      white-space: nowrap;
      /* Overflow: let it spill right (next empty column takes it) */
      overflow: visible;
      line-height: 1;
    }

    /* ── Body row (day labels + grid) ─────────────────────────────── */
    .heatmap-body {
      display: flex;
      align-items: flex-start;
      gap: 4px;
    }

    /* Day-of-week labels column */
    .heatmap-days {
      display: grid;
      grid-template-rows: repeat(7, 11px);
      gap: 2px;
      width: 24px;
      flex-shrink: 0;
      font-family: var(--ff-body);
      font-size: 9px;
      color: var(--muted);
      line-height: 11px;
      text-align: right;
    }

    /* Positioned container for grid + tooltip */
    .heatmap-grid-wrap {
      position: relative;
    }

    /* Main grid — dynamic cols × 7 rows, filled column-first */
    .heatmap-grid {
      display: grid;
      grid-template-columns: repeat(var(--heatmap-cols, 53), 11px);
      grid-template-rows: repeat(7, 11px);
      grid-auto-flow: column;
      gap: 2px;
    }

    /* ── Individual cell ───────────────────────────────────────────── */
    .heatmap-cell {
      width: 11px;
      height: 11px;
      border-radius: 2px;
      display: block;
      cursor: default;
      /* Smooth border appearance on hover */
      box-sizing: border-box;
      transition: outline-color 0.08s;
      outline: 1px solid transparent;

      /* Level colours — 0 = background, 1–4 = green progression */
      &--0 { background: var(--ivory2); }
      &--1 { background: #9be9a8; }
      &--2 { background: #40c463; }
      &--3 { background: #30a14e; }
      &--4 { background: #216e39; }

      /* Active (hovered) — outline only, fill unchanged */
      &--active {
        outline: 1px solid var(--blue);
        outline-offset: 0;
        z-index: 1;
        position: relative;
      }
    }

    /* ── Tooltip ───────────────────────────────────────────────────── */
    .heatmap-tooltip {
      position: absolute;
      /* Positioned by JS — top/left set via [style] bindings */
      transform: translateX(-50%) translateY(-100%);
      margin-top: -6px;
      background: var(--navy);
      color: #fff;
      font-family: var(--ff-body);
      font-size: 11px;
      white-space: nowrap;
      padding: 4px 8px;
      border-radius: 4px;
      pointer-events: none;
      z-index: 10;
      /* Tiny drop-shadow for depth */
      box-shadow: 0 2px 8px rgba(0,0,0,0.22);

      /* Arrow pointing down */
      &::after {
        content: '';
        position: absolute;
        top: 100%;
        left: 50%;
        transform: translateX(-50%);
        border: 4px solid transparent;
        border-top-color: var(--navy);
      }
    }

    /* ── Legend ────────────────────────────────────────────────────── */
    .heatmap-legend {
      display: flex;
      align-items: center;
      gap: 3px;
      justify-content: flex-end;
      padding-right: 2px;
      margin-top: 2px;
    }

    .heatmap-legend__label {
      font-family: var(--ff-body);
      font-size: 10px;
      color: var(--muted);
      margin: 0 3px;
    }
  `],
})
export class ActivityHeatmapComponent implements OnChanges {
  @Input() days: ActivityDay[] = [];

  cells:       HeatCell[]                              = [];
  monthLabels: { label: string; weekCol: number }[]   = [];
  totalActive  = 0;
  hoveredCell: HeatCell | null                         = null;
  tooltip      = signal<Tooltip | null>(null);

  // Total columns (weeks) in the padded grid — recalculated in buildGrid
  totalCols = 53;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['days'] && this.days.length) {
      this.buildGrid();
    }
  }

  // ── Tooltip handlers ──────────────────────────────────────────────────────

  showTooltip(cell: HeatCell): void {
    if (!cell.label) return;   // padding cell — no tooltip
    this.hoveredCell = cell;

    // Centre of the hovered cell relative to the grid-wrap left edge.
    // col × (cellSize + gap) + halfCell
    const cellStep = CELL_PX + GAP_PX;
    const cx = cell.col * cellStep + CELL_PX / 2;
    // Top of the hovered cell
    const cy = cell.row * cellStep;

    this.tooltip.set({ text: cell.label, x: cx, y: cy });
  }

  hideTooltip(): void {
    this.hoveredCell = null;
    this.tooltip.set(null);
  }

  // ── Grid builder ──────────────────────────────────────────────────────────

  private buildGrid(): void {
    // Use all passed days — the parent already filters by year if needed
    const raw = [...this.days];

    if (!raw.length) return;

    // Pad front so the first cell lands on Sunday (0)
    const firstDate = new Date(raw[0].date + 'T00:00:00');
    const startPad  = firstDate.getDay(); // 0=Sun…6=Sat

    const padded: (ActivityDay | null)[] = [
      ...Array(startPad).fill(null),
      ...raw,
    ];

    // Pad tail to complete the final week
    while (padded.length % 7 !== 0) padded.push(null);

    this.totalCols   = padded.length / 7;
    this.totalActive = raw.filter(d => d.count > 0).length;

    this.cells = padded.map((d, idx) => {
      const col = Math.floor(idx / 7);   // column = week index
      const row = idx % 7;               // row = day-of-week

      if (!d) {
        return { date: '', count: 0, level: 0 as const, label: '', col, row };
      }
      const level = this.countToLevel(d.count);
      const label = d.count === 0
        ? `No submissions on ${this.formatDate(d.date)}`
        : `${d.count} submission${d.count > 1 ? 's' : ''} on ${this.formatDate(d.date)}`;
      return { date: d.date, count: d.count, level, label, col, row };
    });

    // Month labels — one per new month, placed at the week column where it first appears
    this.monthLabels = [];
    const seen = new Set<string>();
    padded.forEach((d, idx) => {
      if (!d) return;
      const key     = d.date.slice(0, 7); // YYYY-MM
      const weekCol = Math.floor(idx / 7) + 1; // 1-based CSS grid-column
      if (!seen.has(key)) {
        seen.add(key);
        const dt = new Date(d.date + 'T00:00:00');
        this.monthLabels.push({
          label:   dt.toLocaleDateString('en-US', { month: 'short' }),
          weekCol,
        });
      }
    });
  }

  private countToLevel(count: number): 0 | 1 | 2 | 3 | 4 {
    if (count === 0)  return 0;
    if (count <= 2)   return 1;   // 1–2
    if (count <= 5)   return 2;   // 3–5
    if (count <= 9)   return 3;   // 6–9
    return 4;                     // 10+
  }

  private formatDate(iso: string): string {
    const d = new Date(iso + 'T00:00:00');
    return d.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
  }
}
