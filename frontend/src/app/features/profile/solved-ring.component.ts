import {
  Component,
  Input,
  OnChanges,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface RingDifficultyData {
  easySolved:       number;
  mediumSolved:     number;
  hardSolved:       number;
  easyTotal:        number;
  mediumTotal:      number;
  hardTotal:        number;
  totalSolved:      number;
  totalAttempted:   number;
  acceptanceRate:   number;   // 0–100
  totalSubmissions: number;
}

interface ArcSegment {
  id:         'easy' | 'medium' | 'hard';
  label:      string;
  color:      string;
  solved:     number;
  total:      number;
  dashArray:  string;
  /** Final resting dashOffset — CSS animation lands here from CIRCUMFERENCE */
  dashOffset: string;
}

const TWO_PI         = 2 * Math.PI;
const GAP_DEG        = 3;
const RING_RADIUS    = 68;
const STROKE_WIDTH   = 11;
const CIRCUMFERENCE  = TWO_PI * RING_RADIUS;
const GAP_FRAC       = GAP_DEG / 360;
const TOTAL_GAP_FRAC = 3 * GAP_FRAC;

const SVG_SIZE = Math.round(2 * (RING_RADIUS + STROKE_WIDTH / 2 + 4));  // 154
const CX       = SVG_SIZE / 2;

const COLOR_EASY   = '#1D9E75';
const COLOR_MEDIUM = '#FFB700';
const COLOR_HARD   = '#D32F2F';
const COLOR_TRACK  = '#ECE9E1';

@Component({
  selector: 'app-solved-ring',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ring-card">

      <!-- ── Ring + centre ─────────────────────────────────────────── -->
      <div class="ring-wrap"
           (mouseenter)="hovered = true"
           (mouseleave)="hovered = false; hoveredSegment = null"
           [attr.aria-label]="'Problems solved: ' + data.totalSolved + ' out of ' + data.totalAttempted + ' attempted. Acceptance rate: ' + data.acceptanceRate + '%'">

        <svg class="ring-svg"
             [attr.width]="SVG_SIZE"
             [attr.height]="SVG_SIZE"
             [attr.viewBox]="'0 0 ' + SVG_SIZE + ' ' + SVG_SIZE"
             aria-hidden="true">

          <!-- Background track -->
          <circle
            [attr.cx]="CX" [attr.cy]="CX"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="TRACK_COLOR"
            [attr.stroke-width]="STROKE_WIDTH"
            stroke-linecap="round"/>

          <!--
            Draw-in: each segment animates stroke-dashoffset from CIRCUMFERENCE
            (fully hidden) to its computed target. 800ms ease-out.
            Stagger: Easy 0ms → Medium 100ms → Hard 200ms.
          -->

          <!-- Easy -->
          <circle
            [attr.cx]="CX" [attr.cy]="CX"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="easyArc.color"
            [attr.stroke-width]="STROKE_WIDTH"
            stroke-linecap="round"
            [attr.stroke-dasharray]="easyArc.dashArray"
            [attr.stroke-dashoffset]="easyArc.dashOffset"
            [class.ring-seg--lit]="hoveredSegment === 'easy'"
            class="ring-seg ring-seg--easy"
            [attr.transform]="'rotate(-90 ' + CX + ' ' + CX + ')'"/>

          <!-- Medium — 100ms stagger -->
          <circle
            [attr.cx]="CX" [attr.cy]="CX"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="mediumArc.color"
            [attr.stroke-width]="STROKE_WIDTH"
            stroke-linecap="round"
            [attr.stroke-dasharray]="mediumArc.dashArray"
            [attr.stroke-dashoffset]="mediumArc.dashOffset"
            [class.ring-seg--lit]="hoveredSegment === 'medium'"
            class="ring-seg ring-seg--medium"
            [attr.transform]="'rotate(-90 ' + CX + ' ' + CX + ')'"/>

          <!-- Hard — 200ms stagger -->
          <circle
            [attr.cx]="CX" [attr.cy]="CX"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="hardArc.color"
            [attr.stroke-width]="STROKE_WIDTH"
            stroke-linecap="round"
            [attr.stroke-dasharray]="hardArc.dashArray"
            [attr.stroke-dashoffset]="hardArc.dashOffset"
            [class.ring-seg--lit]="hoveredSegment === 'hard'"
            class="ring-seg ring-seg--hard"
            [attr.transform]="'rotate(-90 ' + CX + ' ' + CX + ')'"/>

        </svg>

        <!-- Layer 1 — default: solved count -->
        <div class="ring-centre ring-centre--default"
             [class.ring-centre--out]="hovered"
             aria-hidden="true">
          <span class="ring-centre__num">{{ data.totalSolved }}</span>
          <span class="ring-centre__solved-lbl">✓ Solved</span>
          <span class="ring-centre__sub">{{ data.totalAttempted }} attempted</span>
        </div>

        <!-- Layer 2 — hover: acceptance rate -->
        <div class="ring-centre ring-centre--hover"
             [class.ring-centre--in]="hovered"
             aria-hidden="true">
          <span class="ring-centre__num ring-centre__num--rate">
            {{ data.acceptanceRate }}<span class="ring-centre__unit">%</span>
          </span>
          <span class="ring-centre__rate-lbl">Acceptance</span>
          <span class="ring-centre__sub">{{ data.totalSubmissions }} submissions</span>
        </div>

      </div>

      <!-- ── E / M / H stat rows ───────────────────────────────────── -->
      <div class="ring-stats">

        <div class="ring-stat"
             (mouseenter)="hoveredSegment = 'easy'"
             (mouseleave)="hoveredSegment = null">
          <div class="ring-stat__top">
            <span class="ring-stat__dot" style="background:#1D9E75"></span>
            <span class="ring-stat__label">Easy</span>
            <span class="ring-stat__frac ring-stat__frac--easy">
              {{ data.easySolved }}<span class="ring-stat__frac-total">/{{ data.easyTotal }}</span>
            </span>
          </div>
          <div class="ring-stat__track" aria-hidden="true">
            <div class="ring-stat__fill ring-stat__fill--easy"
                 [style.width.%]="barPct(data.easySolved, data.easyTotal)"
                 [class.ring-stat__fill--lit]="hoveredSegment === 'easy'">
            </div>
          </div>
        </div>

        <div class="ring-stat"
             (mouseenter)="hoveredSegment = 'medium'"
             (mouseleave)="hoveredSegment = null">
          <div class="ring-stat__top">
            <span class="ring-stat__dot" style="background:#FFB700"></span>
            <span class="ring-stat__label">Med.</span>
            <span class="ring-stat__frac ring-stat__frac--medium">
              {{ data.mediumSolved }}<span class="ring-stat__frac-total">/{{ data.mediumTotal }}</span>
            </span>
          </div>
          <div class="ring-stat__track" aria-hidden="true">
            <div class="ring-stat__fill ring-stat__fill--medium"
                 [style.width.%]="barPct(data.mediumSolved, data.mediumTotal)"
                 [class.ring-stat__fill--lit]="hoveredSegment === 'medium'">
            </div>
          </div>
        </div>

        <div class="ring-stat"
             [class.ring-stat--fire]="hoveredSegment === 'hard'"
             (mouseenter)="hoveredSegment = 'hard'"
             (mouseleave)="hoveredSegment = null">
          <div class="ring-stat__top">
            <span class="ring-stat__dot ring-stat__dot--hard"></span>
            <span class="ring-stat__label ring-stat__label--hard">Hard</span>
            <span class="ring-stat__frac ring-stat__frac--hard">
              {{ data.hardSolved }}<span class="ring-stat__frac-total">/{{ data.hardTotal }}</span>
            </span>
          </div>
          <div class="ring-stat__track" aria-hidden="true">
            <div class="ring-stat__fill ring-stat__fill--hard"
                 [style.width.%]="barPct(data.hardSolved, data.hardTotal)"
                 [class.ring-stat__fill--lit]="hoveredSegment === 'hard'">
            </div>
          </div>
        </div>

      </div>

    </div>
  `,
  styles: [`
    .ring-card {
      display: flex;
      align-items: center;
      gap: 24px;
    }

    .ring-wrap {
      position: relative;
      width: 154px;
      height: 154px;
      flex-shrink: 0;
      cursor: default;
      user-select: none;  /* ring is informational, not selectable */
    }

    .ring-svg { display: block; }

    /* ── Segment draw-in ───────────────────────────────────────────── */
    /*
      The animation starts at stroke-dashoffset = CIRCUMFERENCE (fully hidden)
      and ends at the element's natural stroke-dashoffset (set by [attr]).
      fill-mode: both keeps the segment invisible before its delay fires.
    */
    @keyframes segDrawIn {
      from { stroke-dashoffset: 427.26; }
    }

    .ring-seg {
      transition: opacity 0.18s ease-out, filter 0.18s ease-out;
      opacity: 0.88;
    }
    .ring-seg--lit {
      opacity: 1;
      filter: brightness(1.18);
    }
    .ring-seg--easy   { animation: segDrawIn 800ms ease-out   0ms both; }
    .ring-seg--medium { animation: segDrawIn 800ms ease-out 100ms both; }
    .ring-seg--hard   { animation: segDrawIn 800ms ease-out 200ms both; }

    /* ── Centre layers ─────────────────────────────────────────────── */
    .ring-centre {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      pointer-events: none;
      gap: 2px;
    }
    .ring-centre--default {
      opacity: 1;
      transition: opacity 180ms ease-out;
    }
    .ring-centre--default.ring-centre--out {
      opacity: 0;
    }
    .ring-centre--hover {
      opacity: 0;
      transition: opacity 180ms ease-out;
    }
    .ring-centre--hover.ring-centre--in {
      opacity: 1;
    }

    .ring-centre__num {
      font-family: var(--ff-body);
      font-size: 34px;
      font-weight: 800;
      color: var(--navy);
      line-height: 1;
      letter-spacing: -0.03em;
    }
    .ring-centre__num--rate { font-size: 26px; }
    .ring-centre__unit {
      font-size: 14px;
      font-family: var(--ff-body);
      font-weight: 600;
      color: var(--navy2);
    }
    .ring-centre__solved-lbl {
      font-size: 9px;
      font-family: var(--ff-body);
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: #1D9E75;
      margin-top: 2px;
    }
    .ring-centre__rate-lbl {
      font-size: 9px;
      font-family: var(--ff-body);
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.1em;
      color: var(--muted);
      margin-top: 2px;
    }
    .ring-centre__sub {
      font-size: 9px;
      font-family: var(--ff-mono);
      color: var(--muted);
      opacity: 0.65;
      white-space: nowrap;
    }

    /* ── Stat rows ─────────────────────────────────────────────────── */
    .ring-stats {
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: 0;
      flex: 1;
    }
    .ring-stat {
      display: flex;
      flex-direction: column;
      gap: 5px;
      cursor: default;
      border-radius: 7px;
      padding: 4px 6px;
      transition: background 0.14s, box-shadow 0.14s;

      &:not(.ring-stat--fire):hover {
        background: rgba(26, 43, 74, 0.04);
      }
    }
    .ring-stat__top {
      display: flex;
      align-items: center;
      gap: 7px;
    }
    .ring-stat__dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      flex-shrink: 0;
      transition: box-shadow 0.2s ease;
      &--hard { background: #D32F2F; }
    }
    .ring-stat--fire .ring-stat__dot--hard {
      box-shadow: 0 0 5px 1px rgba(255,80,0,0.6);
    }
    .ring-stat__label {
      font-size: 12px;
      font-family: var(--ff-body);
      font-weight: 600;
      color: var(--charcoal);
      width: 28px;
      flex-shrink: 0;
    }
    .ring-stat__label--hard { color: #D32F2F; }
    .ring-stat--fire .ring-stat__label--hard { color: #ff4500; }

    .ring-stat__frac {
      font-family: var(--ff-mono);
      font-size: 12px;
      font-weight: 700;
      white-space: nowrap;
      margin-left: auto;
    }
    .ring-stat__frac--easy   { color: #1D9E75; }
    .ring-stat__frac--medium { color: #FFB700; }
    .ring-stat__frac--hard   { color: #D32F2F; }

    .ring-stat__frac-total {
      font-weight: 400;
      color: var(--muted);
      font-size: 10px;
      opacity: 0.7;
    }

    @keyframes ringBarGrow {
      from { transform: scaleX(0); }
      to   { transform: scaleX(1); }
    }
    .ring-stat__track {
      height: 4px;
      background: var(--ivory2);
      border-radius: 20px;
      overflow: hidden;
    }
    .ring-stat__fill {
      height: 100%;
      border-radius: 20px;
      transform-origin: left center;
      animation: ringBarGrow 600ms ease-out both;
      transition: filter 0.15s ease;

      &--easy   { background: #1D9E75; animation-delay:   0ms; }
      &--medium { background: #FFB700; animation-delay:  80ms; }
      &--hard   { background: #D32F2F; animation-delay: 160ms; }
      &--lit    { filter: brightness(1.18); }
    }

    /* ── Hard row fire effect ───────────────────────────────────────── */
    @keyframes fireBox {
      0%   {
        box-shadow: 0 0 0px 0px rgba(255,80,0,0), inset 0 0 0px rgba(255,80,0,0);
        background: rgba(26,43,74,0.04);
      }
      35%  {
        box-shadow: 0 0 8px 2px rgba(255,100,0,0.35), 0 0 18px 4px rgba(255,60,0,0.18),
                    inset 0 0 10px rgba(255,120,0,0.07);
        background: rgba(255,69,0,0.06);
      }
      65%  {
        box-shadow: 0 0 12px 3px rgba(255,130,0,0.45), 0 0 24px 6px rgba(255,60,0,0.22),
                    inset 0 0 14px rgba(255,160,0,0.09);
        background: rgba(255,100,0,0.09);
      }
      100% {
        box-shadow: 0 0 8px 2px rgba(255,80,0,0.30), 0 0 18px 4px rgba(255,60,0,0.15),
                    inset 0 0 10px rgba(255,120,0,0.06);
        background: rgba(255,69,0,0.05);
      }
    }
    .ring-stat--fire {
      animation: fireBox 1.6s ease-in-out infinite;
      border-radius: 7px;
    }

    /* ── Reduced motion: kill everything via host context ──────────── */
    :host-context(.no-anim) * {
      animation: none !important;
      transition: none !important;
    }
  `],
})
export class SolvedRingComponent implements OnChanges {
  @Input() data!: RingDifficultyData;

  readonly RADIUS       = RING_RADIUS;
  readonly STROKE_WIDTH = STROKE_WIDTH;
  readonly SVG_SIZE     = SVG_SIZE;
  readonly CX           = CX;
  readonly TRACK_COLOR  = COLOR_TRACK;

  hovered         = false;
  hoveredSegment: 'easy' | 'medium' | 'hard' | null = null;

  easyArc:   ArcSegment = this.emptyArc('easy',   'Easy',   COLOR_EASY);
  mediumArc: ArcSegment = this.emptyArc('medium', 'Medium', COLOR_MEDIUM);
  hardArc:   ArcSegment = this.emptyArc('hard',   'Hard',   COLOR_HARD);

  ngOnChanges(): void {
    if (this.data) this.buildArcs();
  }

  barPct(solved: number, total: number): number {
    return total === 0 ? 0 : Math.round((solved / total) * 100);
  }

  private buildArcs(): void {
    const { easySolved, mediumSolved, hardSolved } = this.data;
    const total  = easySolved + mediumSolved + hardSolved;
    const usable = 1 - TOTAL_GAP_FRAC;
    const MIN_FRAC = 2 / 360;

    let easyFrac   = total > 0 ? (easySolved   / total) * usable : 0;
    let mediumFrac = total > 0 ? (mediumSolved  / total) * usable : 0;
    let hardFrac   = total > 0 ? (hardSolved    / total) * usable : 0;

    if (easySolved   > 0 && easyFrac   < MIN_FRAC) easyFrac   = MIN_FRAC;
    if (mediumSolved > 0 && mediumFrac < MIN_FRAC) mediumFrac = MIN_FRAC;
    if (hardSolved   > 0 && hardFrac   < MIN_FRAC) hardFrac   = MIN_FRAC;

    const gapLen = GAP_FRAC * CIRCUMFERENCE;

    this.easyArc = {
      ...this.emptyArc('easy', 'Easy', COLOR_EASY),
      solved:    easySolved,
      total:     this.data.easyTotal,
      dashArray:  `${easyFrac * CIRCUMFERENCE} ${CIRCUMFERENCE}`,
      dashOffset: '0',
    };

    const mediumStart = easyFrac * CIRCUMFERENCE + gapLen;
    this.mediumArc = {
      ...this.emptyArc('medium', 'Medium', COLOR_MEDIUM),
      solved:    mediumSolved,
      total:     this.data.mediumTotal,
      dashArray:  `${mediumFrac * CIRCUMFERENCE} ${CIRCUMFERENCE}`,
      dashOffset: `${-mediumStart}`,
    };

    const hardStart = mediumStart + mediumFrac * CIRCUMFERENCE + gapLen;
    this.hardArc = {
      ...this.emptyArc('hard', 'Hard', COLOR_HARD),
      solved:    hardSolved,
      total:     this.data.hardTotal,
      dashArray:  `${hardFrac * CIRCUMFERENCE} ${CIRCUMFERENCE}`,
      dashOffset: `${-hardStart}`,
    };
  }

  private emptyArc(
    id: 'easy' | 'medium' | 'hard',
    label: string,
    color: string,
  ): ArcSegment {
    return {
      id, label, color,
      solved: 0, total: 0,
      dashArray:  `0 ${CIRCUMFERENCE}`,
      dashOffset: '0',
    };
  }
}
