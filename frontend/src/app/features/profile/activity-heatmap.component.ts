import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  AfterViewInit,
  SimpleChanges,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ElementRef,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HeatmapGrid, HeatmapCell } from '../../utils/heatmap-calendar.util';

// ── Fixed layout constants ────────────────────────────────────────────────────
const GAP_PX        = 3;   // gap between cells within a column, and between columns
const MONTH_GAP_PX  = 5;   // extra left-margin on the first week of a new month
// No day-label gutter — grid starts at the left edge of the container
const GUTTER_PX     = 0;

// Cell size bounds
const CELL_MIN_PX = 10;
const CELL_MAX_PX = 16;
const CELL_DEFAULT_PX = 11; // used before the container is measured

// ── Internal types ────────────────────────────────────────────────────────────

interface RenderCell {
  date:      string | null;
  count:     number;
  level:     0 | 1 | 2 | 3 | 4;
  modifiers: string;  // CSS modifier classes after 'heatmap-cell '
  label:     string;  // tooltip text — '' for padding / future cells
  col:       number;  // 0-based week index
  row:       number;  // 0-based day-of-week (0 = Sun)
  isToday:   boolean;
  isFuture:  boolean;
  isPadding: boolean;
}

interface RenderWeek {
  cells:      RenderCell[];
  marginLeft: number;  // 0 for normal weeks; MONTH_GAP_PX for month-start weeks (col > 0)
}

interface Tooltip {
  text:  string;
  x:     number;   // px from left edge of .heatmap-grid-wrap
  y:     number;   // px from top edge of .heatmap-grid-wrap
  below: boolean;  // true → render below cell (top-3-rows rule)
}

@Component({
  selector: 'app-activity-heatmap',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="heatmap-scroll">
      <div class="heatmap-inner"
           [class.heatmap-inner--fade]="fading()">

        <!-- ── Month labels ─────────────────────────────────────────── -->
        <!-- No gutter — grid starts at left edge, labels align with it  -->
        <div class="heatmap-months" aria-hidden="true">
          <div class="heatmap-months__track" [style.width.px]="trackWidth">
            @for (m of monthLabels; track m.label + m.offsetPx) {
              <span class="heatmap-month" [style.left.px]="m.offsetPx">{{ m.label }}</span>
            }
          </div>
        </div>

        <!-- ── Grid + tooltip host ──────────────────────────────────── -->
        <div class="heatmap-grid-wrap" (mouseleave)="hideTooltip()">

          <div class="heatmap-grid"
               role="grid"
               [attr.aria-label]="'Activity heatmap, ' + totalActive + ' active days'"
               [style.--cell]="cellPx() + 'px'"
               [style.--gap]="GAP_PX + 'px'">
            @for (week of weeks; track $index) {
              <div class="heatmap-week"
                   role="row"
                   [style.margin-left.px]="week.marginLeft">
                @for (cell of week.cells; track cell.date || ($index + '__pad')) {
                  <span
                    [class]="'heatmap-cell ' + cell.modifiers + (hoveredCell === cell ? ' heatmap-cell--hover' : '')"
                    role="gridcell"
                    [attr.aria-label]="cell.label || null"
                    [style.animation-delay]="colDelay(cell.col)"
                    (mouseenter)="showTooltip(cell)"
                    (mouseleave)="hideTooltip()">
                  </span>
                }
              </div>
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

        <!-- ── Legend ──────────────────────────────────────────────── -->
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
    /* width: 100% fills the parent column. overflow-x: auto is a safety
       net for very narrow viewports where CELL_MIN_PX would still overflow. */
    .heatmap-scroll {
      display: block;
      width: 100%;
      overflow-x: auto;
      -webkit-overflow-scrolling: touch;
      padding-bottom: 4px;
      scrollbar-width: thin;
      scrollbar-color: rgba(26, 43, 74, 0.2) transparent;
      &::-webkit-scrollbar        { height: 4px; }
      &::-webkit-scrollbar-track  { background: transparent; }
      &::-webkit-scrollbar-thumb  { background: rgba(26,43,74,0.2); border-radius: 4px; }
    }

    /* Cells, labels, and legend are decorative — never accidentally selected */
    .heatmap-grid,
    .heatmap-months,
    .heatmap-legend {
      user-select: none;
    }

    /* ── Mode-switch fade ──────────────────────────────────────────── */
    .heatmap-inner {
      display: flex;
      flex-direction: column;
      gap: 4px;
      width: 100%;
      opacity: 1;
      transition: opacity 150ms ease-out;
    }
    .heatmap-inner--fade { opacity: 0; }

    /* ── Month label row ───────────────────────────────────────────── */
    /* No gutter div — track starts at left: 0, matching the grid edge */
    .heatmap-months {
      display: block;
      position: relative;
      height: 16px;
    }
    .heatmap-months__track {
      position: relative;
      height: 16px;
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

    /* ── Grid wrapper — tooltip anchor ────────────────────────────── */
    .heatmap-grid-wrap {
      position: relative;
    }

    /* ── Week columns: outer flex row ─────────────────────────────── */
    .heatmap-grid {
      display: flex;
      flex-direction: row;
      align-items: flex-start;
      gap: var(--gap, 3px);
    }

    /* ── Single week: inner flex column (7 cells, Sun→Sat) ─────────── */
    .heatmap-week {
      display: flex;
      flex-direction: column;
      gap: var(--gap, 3px);
      flex-shrink: 0;
    }

    /* ── Cell reveal animation ─────────────────────────────────────── */
    @keyframes cellReveal {
      from { opacity: 0; }
      to   { opacity: 1; }
    }

    /* ── Base cell — size from --cell custom property ──────────────── */
    .heatmap-cell {
      width:  var(--cell, 11px);
      height: var(--cell, 11px);
      border-radius: 3px;
      display: block;
      box-sizing: border-box;
      flex-shrink: 0;
      animation: cellReveal 120ms ease-out both;
      transition: filter 80ms ease, outline-color 80ms ease;
    }

    /* ── Padding cells (before range start) ───────────────────────── */
    .heatmap-cell--padding {
      background: transparent !important;
      pointer-events: none;
      cursor: default;
      outline: none !important;
      animation: none;
    }

    /* ── Future cells (after today in current week) ────────────────── */
    .heatmap-cell--future {
      background: var(--ivory2);
      pointer-events: none;
      cursor: default;
      outline: none !important;
    }

    /* ── Today's cell ──────────────────────────────────────────────── */
    .heatmap-cell--today {
      outline: 1.5px solid var(--blue, #2E86AB);
      outline-offset: 0;
    }

    /* ── Level colors ──────────────────────────────────────────────── */
    .heatmap-cell--0 { background: var(--ivory2); }
    .heatmap-cell--1 { background: #9be9a8; }
    .heatmap-cell--2 { background: #44c566; }
    .heatmap-cell--3 { background: #2ea04e; }
    .heatmap-cell--4 { background: #1a6b32; }

    /* ── Interactive cell states ───────────────────────────────────── */
    .heatmap-cell:not(.heatmap-cell--padding):not(.heatmap-cell--future) {
      cursor: default;
      outline: 1.5px solid transparent;
      outline-offset: 0;
    }
    .heatmap-cell--hover {
      filter: brightness(1.12);
      outline-color: rgba(46, 160, 78, 0.6);
      position: relative;
      z-index: 1;
    }

    /* ── Tooltip ───────────────────────────────────────────────────── */
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

      &::after {
        content: '';
        position: absolute;
        top: 100%; left: 50%;
        transform: translateX(-50%);
        border: 4px solid transparent;
        border-top-color: var(--navy);
      }
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
    .heatmap-legend .heatmap-cell {
      pointer-events: none;
      animation: none;
      flex-shrink: 0;
      outline: none !important;
    }

    /* ── Reduced motion ────────────────────────────────────────────── */
    :host-context(.no-anim) * {
      animation: none !important;
      transition: none !important;
    }
  `],
})
export class ActivityHeatmapComponent implements OnChanges, AfterViewInit, OnDestroy {
  @Input() grid: HeatmapGrid | null = null;

  // ── Reactive cell size ────────────────────────────────────────────────────
  // Driven by ResizeObserver. Everything that was a constant CELL_PX now reads
  // from this signal so template + offset math always agree.
  cellPx = signal(CELL_DEFAULT_PX);

  weeks:       RenderWeek[]                                           = [];
  monthLabels: { label: string; weekCol: number; offsetPx: number }[] = [];
  totalActive  = 0;
  hoveredCell: RenderCell | null = null;
  tooltip      = signal<Tooltip | null>(null);
  fading       = signal(false);

  trackWidth   = 0;

  readonly GAP_PX = GAP_PX;

  private readonly cdr      = inject(ChangeDetectorRef);
  private readonly el       = inject(ElementRef);

  private fadeTimer:   ReturnType<typeof setTimeout> | null = null;
  private resizeTimer: ReturnType<typeof setTimeout> | null = null;
  private resizeObs:   ResizeObserver | null = null;

  // Pre-computed left-edge pixel offset per column (after month gaps).
  // Used for tooltip x positioning.
  private colOffsetPx: number[] = [];

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngAfterViewInit(): void {
    // Observe the scroll container. ResizeObserver fires on mount and on every
    // size change — sidebar toggling, window resize, orientation change, etc.
    const scrollEl = (this.el.nativeElement as HTMLElement)
      .querySelector('.heatmap-scroll');

    if (!scrollEl) return;

    this.resizeObs = new ResizeObserver(() => {
      // Debounce: coalesce rapid-fire resize events into one recalculation
      if (this.resizeTimer) clearTimeout(this.resizeTimer);
      this.resizeTimer = setTimeout(() => this.onResize(scrollEl as HTMLElement), 100);
    });
    this.resizeObs.observe(scrollEl);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (!changes['grid'] || !this.grid) return;

    if (this.weeks.length > 0) {
      this.fading.set(true);
      if (this.fadeTimer) clearTimeout(this.fadeTimer);
      this.fadeTimer = setTimeout(() => {
        this.buildRenderData();
        this.fading.set(false);
        this.cdr.markForCheck();
      }, 150);
    } else {
      this.buildRenderData();
    }
  }

  ngOnDestroy(): void {
    this.resizeObs?.disconnect();
    if (this.fadeTimer)   clearTimeout(this.fadeTimer);
    if (this.resizeTimer) clearTimeout(this.resizeTimer);
  }

  // ── Resize handler ────────────────────────────────────────────────────────

  private onResize(scrollEl: HTMLElement): void {
    const numWeeks = this.grid?.weeks.length ?? 0;
    if (numWeeks === 0) return;

    const containerWidth = scrollEl.getBoundingClientRect().width;
    const newCell = this.computeCellPx(containerWidth, numWeeks);

    if (newCell === this.cellPx()) return;  // nothing changed — skip redraw

    this.cellPx.set(newCell);
    this.recomputeOffsets(newCell);
    this.cdr.markForCheck();
  }

  // ── Cell size formula ─────────────────────────────────────────────────────
  //
  // Available pixel width for the cell grid:
  //   containerWidth
  //   − GUTTER_PX          (day-label column + gap to grid)
  //   − (numWeeks − 1) * GAP_PX      (gaps between week columns)
  //   − numMonthGaps * MONTH_GAP_PX  (extra gaps at month boundaries)
  //
  // Divided by numWeeks → raw cell size, then clamped to [CELL_MIN_PX, CELL_MAX_PX].
  //
  // numMonthGaps is the number of month boundaries (≈ 11 for a year, ≈ 11 for
  // rolling). We use the actual count from the current grid for accuracy.

  private computeCellPx(containerWidth: number, numWeeks: number): number {
    if (numWeeks === 0) return CELL_DEFAULT_PX;

    const numMonthGaps = this.countMonthGaps();
    const totalGaps =
      (numWeeks - 1) * GAP_PX +
      numMonthGaps * MONTH_GAP_PX;

    const available = containerWidth - GUTTER_PX - totalGaps;
    const raw = available / numWeeks;

    return Math.min(CELL_MAX_PX, Math.max(CELL_MIN_PX, raw));
  }

  private countMonthGaps(): number {
    if (!this.grid) return 0;
    // Month gap appears on every week col > 0 that has a non-null monthLabel
    return this.grid.weeks.filter((w, i) => i > 0 && w.monthLabel !== null).length;
  }

  // ── Offset recalculation ──────────────────────────────────────────────────
  //
  // Called from both buildRenderData() and onResize() so the offset array
  // is always consistent with the current cellPx value.
  // Returns the updated colOffsets so buildRenderData can reuse them without
  // a second pass.

  private recomputeOffsets(cell: number): number[] {
    const gridWeeks = this.grid?.weeks ?? [];
    const numWeeks  = gridWeeks.length;
    const offsets   = new Array<number>(numWeeks);
    let monthGapsSeen = 0;

    for (let col = 0; col < numWeeks; col++) {
      const isMonthStart = col > 0 && gridWeeks[col].monthLabel !== null;
      if (isMonthStart) monthGapsSeen++;

      // Left edge of this column:
      //   col * (cell + GAP) = base position (column width + inter-column gap)
      //   + accumulated month gaps
      offsets[col] = col * (cell + GAP_PX) + monthGapsSeen * MONTH_GAP_PX;
    }

    this.colOffsetPx = offsets;
    this.trackWidth  = numWeeks > 0 ? offsets[numWeeks - 1] + cell : 0;

    return offsets;
  }

  // ── Animation delay ───────────────────────────────────────────────────────
  colDelay(col: number): string {
    return `${Math.floor(col / 4) * 30}ms`;
  }

  // ── Tooltip ───────────────────────────────────────────────────────────────

  showTooltip(cell: RenderCell): void {
    if (!cell.label) return;
    this.hoveredCell = cell;

    const cp  = this.cellPx();
    const cx  = (this.colOffsetPx[cell.col] ?? cell.col * (cp + GAP_PX)) + cp / 2;
    const cy  = cell.row * (cp + GAP_PX);

    const below = cell.row <= 2;
    const ty    = below ? cy + cp : cy;

    this.tooltip.set({ text: cell.label, x: cx, y: ty, below });
  }

  hideTooltip(): void {
    this.hoveredCell = null;
    this.tooltip.set(null);
  }

  // ── Build render data ─────────────────────────────────────────────────────

  private buildRenderData(): void {
    if (!this.grid || !this.grid.weeks.length) {
      this.weeks       = [];
      this.monthLabels = [];
      this.totalActive = 0;
      this.trackWidth  = 0;
      this.colOffsetPx = [];
      return;
    }

    const gridWeeks  = this.grid.weeks;
    const numWeeks   = gridWeeks.length;
    this.totalActive = this.grid.activeDays;

    // Recompute offsets using the current cellPx (may have been set by a prior
    // resize before this grid input arrived, or will default to CELL_DEFAULT_PX
    // on first render — ResizeObserver will correct it shortly after mount).
    const offsets = this.recomputeOffsets(this.cellPx());

    const renderWeeks: RenderWeek[] = [];
    const labels: typeof this.monthLabels = [];

    for (let col = 0; col < numWeeks; col++) {
      const week         = gridWeeks[col];
      const isMonthStart = col > 0 && week.monthLabel !== null;

      const cells: RenderCell[] = week.cells.map((c, row) => ({
        date:      c.date,
        count:     c.count,
        level:     c.level,
        modifiers: this.buildModifiers(c),
        label:     this.buildLabel(c),
        col,
        row,
        isToday:   c.isToday,
        isFuture:  c.isFuture,
        isPadding: c.date === null,
      }));

      renderWeeks.push({
        cells,
        marginLeft: isMonthStart ? MONTH_GAP_PX : 0,
      });

      if (week.monthLabel) {
        labels.push({
          label:    week.monthLabel,
          weekCol:  col + 1,
          offsetPx: offsets[col],
        });
      }
    }

    this.weeks       = renderWeeks;
    this.monthLabels = labels;
  }

  // ── CSS modifier string ───────────────────────────────────────────────────

  private buildModifiers(cell: HeatmapCell): string {
    if (cell.date === null) return 'heatmap-cell--padding';
    if (cell.isFuture)      return 'heatmap-cell--future';
    const lvl = `heatmap-cell--${cell.level}`;
    return cell.isToday ? `${lvl} heatmap-cell--today` : lvl;
  }

  // ── Tooltip label ─────────────────────────────────────────────────────────

  private buildLabel(cell: HeatmapCell): string {
    if (cell.date === null || cell.isFuture) return '';

    const d = new Date(cell.date + 'T00:00:00');
    const dateStr = d.toLocaleDateString('en-US', {
      weekday: 'long',
      month:   'short',
      day:     'numeric',
      year:    'numeric',
    });

    if (cell.count === 0) return `No submissions · ${dateStr}`;
    const noun = cell.count === 1 ? 'submission' : 'submissions';
    return `${cell.count} ${noun} · ${dateStr}`;
  }
}
