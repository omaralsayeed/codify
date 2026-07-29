import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  SimpleChanges,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivityDay } from '../../core/models/analytics.model';

const CELL_PX = 11;
const GAP_PX  = 3;   // exactly 3px — intentional separation, not default

interface HeatCell {
  date:  string;
  count: number;
  level: 0 | 1 | 2 | 3 | 4;
  label: string;  // tooltip + aria-label text
  col:   number;  // 0-based week column
  row:   number;  // 0-based day-of-week (0 = Sun)
}

interface Tooltip {
  text:  string;
  x:     number;   // px from left of grid-wrap
  y:     number;   // px from top of grid-wrap
  below: boolean;  // true for top 2 rows — arrow points up
}

@Component({
  selector: 'app-activity-heatmap',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="heatmap-scroll">
      <div class="heatmap-inner"
           [style.--heatmap-cols]="totalCols"
           [class.heatmap-inner--fade]="fading()">

        <!-- Month labels: absolutely positioned, floating above columns -->
        <div class="heatmap-months" aria-hidden="true">
          <div class="heatmap-months__gutter"></div>
          <div class="heatmap-months__track" [style.width.px]="trackWidth">
            @for (m of monthLabels; track m.label + m.offsetPx) {
              <span class="heatmap-month" [style.left.px]="m.offsetPx">{{ m.label }}</span>
            }
          </div>
        </div>

        <!-- Body: day labels + grid -->
        <div class="heatmap-body">

          <!-- Mon / Wed / Fri labels only -->
          <div class="heatmap-days" aria-hidden="true"
               [style.--cell]="CELL_PX + 'px'"
               [style.--gap]="GAP_PX + 'px'">
            <span class="heatmap-day"></span>        <!-- Sun -->
            <span class="heatmap-day">Mon</span>
            <span class="heatmap-day"></span>        <!-- Tue -->
            <span class="heatmap-day">Wed</span>
            <span class="heatmap-day"></span>        <!-- Thu -->
            <span class="heatmap-day">Fri</span>
            <span class="heatmap-day"></span>        <!-- Sat -->
          </div>

          <!-- Grid + tooltip host -->
          <div class="heatmap-grid-wrap" (mouseleave)="hideTooltip()">

            <div class="heatmap-grid"
                 role="grid"
                 [attr.aria-label]="'Activity heatmap, ' + totalActive + ' active days'"
                 [style.--cell]="CELL_PX + 'px'"
                 [style.--gap]="GAP_PX + 'px'">
              @for (cell of cells; track cell.date || ($index + '__pad')) {
                <span
                  [class]="'heatmap-cell heatmap-cell--' + cell.level + (hoveredCell === cell ? ' heatmap-cell--hover' : '')"
                  role="gridcell"
                  [attr.aria-label]="cell.label || null"
                  [style.animation-delay]="colDelay(cell.col)"
                  (mouseenter)="showTooltip(cell)"
                  (mouseleave)="hideTooltip()">
                </span>
              }
            </div>

            @if (tooltip()) {
              <div class="heatmap-tooltip"
                   [class.heatmap-tooltip--below]="tooltip()!.below"
                   role="tooltip"
                   [style.left.px]="tooltip()!.x"
                   [style.top.px]="tooltip()!.y">
                {{ tooltip()!.text }}
              </div>
            }

          </div>
        </div>

        <!-- Legend -->
        <div class="heatmap-legend" aria-hidden="true">
          <span class="heatmap-legend__lbl">Less</span>
          <span class="heatmap-cell heatmap-cell--0"></span>
          <span class="heatmap-cell heatmap-cell--1"></span>
          <span class="heatmap-cell heatmap-cell--2"></span>
          <span class="heatmap-cell heatmap-cell--3"></span>
          <span class="heatmap-cell heatmap-cell--4"></span>
          <span class="heatmap-legend__lbl">More</span>
        </div>

      </div>
    </div>
  `,
  styles: [`
    /* ── Scroll shell ──────────────────────────────────────────────── */
    .heatmap-scroll {
      display: block;
      width: fit-content;
      max-width: 100%;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
      padding-bottom: 4px;
      /* Custom scrollbar — inherits page style via CSS vars */
      scrollbar-width: thin;
      scrollbar-color: rgba(26, 43, 74, 0.2) transparent;
      &::-webkit-scrollbar        { height: 4px; }
      &::-webkit-scrollbar-track  { background: transparent; }
      &::-webkit-scrollbar-thumb  { background: rgba(26,43,74,0.2); border-radius: 4px; }
    }

    /* Cells + labels are decorative — never accidentally selected */
    .heatmap-grid,
    .heatmap-months,
    .heatmap-days,
    .heatmap-legend {
      user-select: none;
    }

    /* ── Year-switch fade ──────────────────────────────────────────── */
    /* Content opacity transitions on year change — no layout shift */
    .heatmap-inner {
      display: flex;
      flex-direction: column;
      gap: 4px;
      width: fit-content;
      opacity: 1;
      transition: opacity 150ms ease-out;
    }
    .heatmap-inner--fade {
      opacity: 0;
    }

    /* ── Month label row ───────────────────────────────────────────── */
    .heatmap-months {
      display: flex;
      align-items: flex-end;
      height: 16px;
    }
    .heatmap-months__gutter {
      width: 28px;
      flex-shrink: 0;
    }
    /* Track is exact pixel-width of the grid — labels position absolutely */
    .heatmap-months__track {
      position: relative;
      height: 16px;
      flex-shrink: 0;
    }
    .heatmap-month {
      position: absolute;
      bottom: 0;
      font-family: var(--ff-body);
      font-size: 10px;
      font-weight: 500;
      color: var(--muted);
      opacity: 0.65;
      white-space: nowrap;
      line-height: 1;
      letter-spacing: 0.02em;
    }

    /* ── Body row ──────────────────────────────────────────────────── */
    .heatmap-body {
      display: flex;
      align-items: flex-start;
      gap: 4px;
    }

    /* Day labels: Mon / Wed / Fri — vertically centered on their row */
    .heatmap-days {
      display: grid;
      grid-template-rows: repeat(7, calc(var(--cell, 11px) + var(--gap, 3px)));
      width: 24px;
      flex-shrink: 0;
    }
    .heatmap-day {
      font-family: var(--ff-body);
      font-size: 9px;
      font-weight: 500;
      color: var(--muted);
      opacity: 0.6;
      text-align: right;
      display: flex;
      align-items: center;
      justify-content: flex-end;
      line-height: 1;
    }

    .heatmap-grid-wrap { position: relative; }

    /* ── Cell grid ─────────────────────────────────────────────────── */
    .heatmap-grid {
      display: grid;
      grid-template-columns: repeat(var(--heatmap-cols, 53), var(--cell, 11px));
      grid-template-rows: repeat(7, var(--cell, 11px));
      grid-auto-flow: column;
      gap: var(--gap, 3px);
    }

    /* ── Cell reveal animation ─────────────────────────────────────── */
    /*
      Cells appear column by column, left to right.
      4 columns per batch, 30ms between batches.
      All 7 cells in a column appear simultaneously (same delay = same col/4 * 30ms).
      Opacity only — no transform on cells.
    */
    @keyframes cellReveal {
      from { opacity: 0; }
      to   { opacity: 1; }
    }

    /* ── Individual cell ───────────────────────────────────────────── */
    .heatmap-cell {
      width: var(--cell, 11px);
      height: var(--cell, 11px);
      border-radius: 3px;   /* 3px — hand-crafted, not default */
      display: block;
      cursor: default;
      box-sizing: border-box;
      outline: 1.5px solid transparent;
      outline-offset: 0;

      /* Four distinct greens matching the screenshot */
      &--0 { background: var(--ivory2); }
      &--1 { background: #9be9a8; }
      &--2 { background: #44c566; }
      &--3 { background: #2ea04e; }
      &--4 { background: #1a6b32; }

      /* Cell reveal — delay set per-cell inline via colDelay() */
      animation: cellReveal 120ms ease-out both;

      /* Hover: brightness lift + outline. Instant — no delay. */
      transition: filter 80ms ease, outline-color 80ms ease;
      &--hover {
        filter: brightness(1.12);
        outline-color: rgba(46, 160, 78, 0.6);
        position: relative;
        z-index: 1;
      }

      @media (max-width: 767px) {
        width: 10px;
        height: 10px;
      }
    }

    /* ── Tooltip ───────────────────────────────────────────────────── */
    /* Default: above cell. --below: flips to below for top 2 rows. */
    .heatmap-tooltip {
      position: absolute;
      transform: translateX(-50%) translateY(-100%);
      margin-top: -6px;
      background: var(--navy);
      color: #fff;
      font-family: var(--ff-body);
      font-size: 11px;
      font-weight: 400;
      white-space: nowrap;
      padding: 5px 9px;
      border-radius: 6px;
      pointer-events: none;
      z-index: 20;
      box-shadow: 0 4px 12px rgba(0,0,0,0.25), 0 1px 3px rgba(0,0,0,0.15);

      /* Arrow down (tooltip above) */
      &::after {
        content: '';
        position: absolute;
        top: 100%; left: 50%;
        transform: translateX(-50%);
        border: 4px solid transparent;
        border-top-color: var(--navy);
      }

      /* Flipped: tooltip below, arrow up */
      &--below {
        transform: translateX(-50%) translateY(0);
        margin-top: 6px;
        &::after {
          top: auto;
          bottom: 100%;
          border-top-color: transparent;
          border-bottom-color: var(--navy);
        }
      }
    }

    /* ── Legend ────────────────────────────────────────────────────── */
    .heatmap-legend {
      display: flex;
      align-items: center;
      gap: 3px;
      justify-content: flex-end;
      padding-right: 2px;
      margin-top: 4px;
    }
    .heatmap-legend__lbl {
      font-family: var(--ff-body);
      font-size: 10px;
      font-weight: 500;
      color: var(--muted);
      opacity: 0.6;
      margin: 0 4px;
    }

    /* ── Reduced motion ────────────────────────────────────────────── */
    :host-context(.no-anim) * {
      animation: none !important;
      transition: none !important;
    }
  `],
})
export class ActivityHeatmapComponent implements OnChanges, OnDestroy {
  @Input() days: ActivityDay[] = [];

  cells:       HeatCell[]                                          = [];
  monthLabels: { label: string; weekCol: number; offsetPx: number }[] = [];
  totalActive  = 0;
  hoveredCell: HeatCell | null = null;
  tooltip      = signal<Tooltip | null>(null);
  fading       = signal(false);

  /** Exact pixel width of the month-labels track (= grid width) */
  trackWidth = 0;
  totalCols  = 53;

  readonly CELL_PX = CELL_PX;
  readonly GAP_PX  = GAP_PX;

  private readonly cdr = inject(ChangeDetectorRef);
  private fadeTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['days'] || !this.days.length) return;

    if (this.cells.length > 0) {
      // Year switch: fade out, rebuild, fade back in
      this.fading.set(true);
      if (this.fadeTimer) clearTimeout(this.fadeTimer);
      this.fadeTimer = setTimeout(() => {
        this.buildGrid();
        this.fading.set(false);
        this.cdr.markForCheck();
      }, 150);
    } else {
      this.buildGrid();
    }
  }

  ngOnDestroy(): void {
    if (this.fadeTimer) clearTimeout(this.fadeTimer);
  }

  // ── Column reveal delay ───────────────────────────────────────────────────
  // 4 columns per batch, 30ms between batches.
  // All cells in the same column share the same delay → appear simultaneously.
  colDelay(col: number): string {
    return `${Math.floor(col / 4) * 30}ms`;
  }

  // ── Tooltip ───────────────────────────────────────────────────────────────

  showTooltip(cell: HeatCell): void {
    if (!cell.label) return;
    this.hoveredCell = cell;

    const step = CELL_PX + GAP_PX;
    const cx   = cell.col * step + CELL_PX / 2;
    const cy   = cell.row * step;

    // Top 2 rows: put tooltip below to avoid clipping at the top edge
    const below = cell.row <= 1;
    const ty    = below ? cy + CELL_PX : cy;

    this.tooltip.set({ text: cell.label, x: cx, y: ty, below });
  }

  hideTooltip(): void {
    this.hoveredCell = null;
    this.tooltip.set(null);
  }

  // ── Grid builder ──────────────────────────────────────────────────────────

  private buildGrid(): void {
    const raw = [...this.days];
    if (!raw.length) return;

    const firstDate = new Date(raw[0].date + 'T00:00:00');
    const startPad  = firstDate.getDay();  // 0=Sun…6=Sat

    const padded: (ActivityDay | null)[] = [
      ...Array(startPad).fill(null),
      ...raw,
    ];
    while (padded.length % 7 !== 0) padded.push(null);

    this.totalCols   = padded.length / 7;
    this.totalActive = raw.filter(d => d.count > 0).length;
    this.trackWidth  = this.totalCols * CELL_PX + (this.totalCols - 1) * GAP_PX;

    this.cells = padded.map((d, idx) => {
      const col = Math.floor(idx / 7);
      const row = idx % 7;
      if (!d) return { date: '', count: 0, level: 0 as const, label: '', col, row };
      return {
        date:  d.date,
        count: d.count,
        level: this.countToLevel(d.count),
        label: this.buildLabel(d.count, d.date),
        col,
        row,
      };
    });

    // Month labels — absolutely positioned at exact pixel offsets
    this.monthLabels = [];
    const seen = new Set<string>();
    padded.forEach((d, idx) => {
      if (!d) return;
      const key = d.date.slice(0, 7);
      if (seen.has(key)) return;
      seen.add(key);
      const weekIdx = Math.floor(idx / 7);
      const dt = new Date(d.date + 'T00:00:00');
      this.monthLabels.push({
        label:    dt.toLocaleDateString('en-US', { month: 'short' }),
        weekCol:  weekIdx + 1,
        offsetPx: weekIdx * (CELL_PX + GAP_PX),
      });
    });
  }

  private countToLevel(count: number): 0 | 1 | 2 | 3 | 4 {
    if (count === 0)  return 0;
    if (count <= 2)   return 1;
    if (count <= 5)   return 2;
    if (count <= 9)   return 3;
    return 4;
  }

  /**
   * "2 submissions · Wednesday, Jan 15"
   * "No submissions · Wednesday, Jan 15"
   * Count always leads. Middle dot separator. Full weekday name.
   */
  private buildLabel(count: number, iso: string): string {
    const d = new Date(iso + 'T00:00:00');
    const dateStr = d.toLocaleDateString('en-US', {
      weekday: 'long',
      month:   'short',
      day:     'numeric',
    });
    if (count === 0) return `No submissions · ${dateStr}`;
    const noun = count === 1 ? 'submission' : 'submissions';
    return `${count} ${noun} · ${dateStr}`;
  }
}
