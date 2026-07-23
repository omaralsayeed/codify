import {
  Component,
  Input,
  OnChanges,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';

export interface RingDifficultyData {
  easySolved:   number;
  mediumSolved: number;
  hardSolved:   number;
  easyTotal:    number;
  mediumTotal:  number;
  hardTotal:    number;
  totalSolved:  number;
  totalAttempted: number;
  acceptanceRate: number;  // 0–100
  totalSubmissions: number;
}

interface ArcSegment {
  id:          'easy' | 'medium' | 'hard';
  label:       string;
  color:       string;
  solved:      number;
  total:       number;
  /** stroke-dasharray value  */
  dashArray:   string;
  /** stroke-dashoffset value */
  dashOffset:  string;
}

const TWO_PI        = 2 * Math.PI;
const GAP_DEG       = 4;         // degrees of gap between each segment
const RING_RADIUS   = 54;        // px — centre of stroke
const CIRCUMFERENCE = TWO_PI * RING_RADIUS;
const GAP_FRAC      = GAP_DEG / 360;
const TOTAL_GAP_FRAC = 3 * GAP_FRAC;  // 3 gaps total

// Design-system colors (mirrors variables.scss)
const COLOR_EASY   = '#1D9E75';
const COLOR_MEDIUM = '#C8A951';
const COLOR_HARD   = '#D32F2F';
const COLOR_TRACK  = '#ECE9E1';

@Component({
  selector: 'app-solved-ring',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="ring-card">

      <!-- ── Left: SVG ring + centre content ─────────────────────────── -->
      <div class="ring-wrap"
           (mouseenter)="hovered = true"
           (mouseleave)="hovered = false; hoveredSegment = null"
           [attr.aria-label]="'Problems solved: ' + data.totalSolved + ' out of ' + data.totalAttempted + ' attempted. Acceptance rate: ' + data.acceptanceRate + '%'">

        <svg class="ring-svg"
             width="130" height="130"
             viewBox="0 0 130 130"
             aria-hidden="true">

          <!-- Background track -->
          <circle
            cx="65" cy="65"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="TRACK_COLOR"
            stroke-width="10"
            stroke-linecap="round"/>

          <!-- Easy segment -->
          <circle
            cx="65" cy="65"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="easyArc.color"
            stroke-width="10"
            stroke-linecap="butt"
            [attr.stroke-dasharray]="easyArc.dashArray"
            [attr.stroke-dashoffset]="easyArc.dashOffset"
            [class.ring-seg--lit]="hoveredSegment === 'easy'"
            class="ring-seg ring-seg--easy"
            transform="rotate(-90 65 65)"/>

          <!-- Medium segment -->
          <circle
            cx="65" cy="65"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="mediumArc.color"
            stroke-width="10"
            stroke-linecap="butt"
            [attr.stroke-dasharray]="mediumArc.dashArray"
            [attr.stroke-dashoffset]="mediumArc.dashOffset"
            [class.ring-seg--lit]="hoveredSegment === 'medium'"
            class="ring-seg ring-seg--medium"
            transform="rotate(-90 65 65)"/>

          <!-- Hard segment -->
          <circle
            cx="65" cy="65"
            [attr.r]="RADIUS"
            fill="none"
            [attr.stroke]="hardArc.color"
            stroke-width="10"
            stroke-linecap="butt"
            [attr.stroke-dasharray]="hardArc.dashArray"
            [attr.stroke-dashoffset]="hardArc.dashOffset"
            [class.ring-seg--lit]="hoveredSegment === 'hard'"
            class="ring-seg ring-seg--hard"
            transform="rotate(-90 65 65)"/>

        </svg>

        <!-- Centre: default state -->
        <div class="ring-centre ring-centre--default"
             [class.ring-centre--hidden]="hovered">
          <span class="ring-centre__big">{{ data.totalSolved }}</span>
          <span class="ring-centre__lbl">✓ Solved</span>
          <span class="ring-centre__sub">{{ data.totalAttempted }} attempted</span>
        </div>

        <!-- Centre: hover state -->
        <div class="ring-centre ring-centre--hover"
             [class.ring-centre--visible]="hovered">
          <span class="ring-centre__big ring-centre__big--rate">
            {{ data.acceptanceRate }}<span class="ring-centre__unit">%</span>
          </span>
          <span class="ring-centre__lbl">Acceptance</span>
          <span class="ring-centre__sub">{{ data.totalSubmissions }} submissions</span>
        </div>

      </div><!-- /ring-wrap -->

      <!-- ── Right: E/M/H stats ────────────────────────────────────────── -->
      <div class="ring-stats">

        <div class="ring-stat ring-stat--easy"
             (mouseenter)="hoveredSegment = 'easy'"
             (mouseleave)="hoveredSegment = null">
          <span class="ring-stat__dot" style="background: #1D9E75"></span>
          <span class="ring-stat__label">Easy</span>
          <span class="ring-stat__count ring-stat__count--easy">
            {{ data.easySolved }}<span class="ring-stat__total">/{{ data.easyTotal }}</span>
          </span>
        </div>

        <div class="ring-stat ring-stat--medium"
             (mouseenter)="hoveredSegment = 'medium'"
             (mouseleave)="hoveredSegment = null">
          <span class="ring-stat__dot" style="background: #C8A951"></span>
          <span class="ring-stat__label">Med.</span>
          <span class="ring-stat__count ring-stat__count--medium">
            {{ data.mediumSolved }}<span class="ring-stat__total">/{{ data.mediumTotal }}</span>
          </span>
        </div>

        <div class="ring-stat ring-stat--hard"
             (mouseenter)="hoveredSegment = 'hard'"
             (mouseleave)="hoveredSegment = null">
          <span class="ring-stat__dot" style="background: #D32F2F"></span>
          <span class="ring-stat__label">Hard</span>
          <span class="ring-stat__count ring-stat__count--hard">
            {{ data.hardSolved }}<span class="ring-stat__total">/{{ data.hardTotal }}</span>
          </span>
        </div>

      </div><!-- /ring-stats -->

    </div><!-- /ring-card -->
  `,
  styles: [`
    /* ── Card shell ───────────────────────────────────────────────── */
    .ring-card {
      display: flex;
      align-items: center;
      gap: 20px;
    }

    /* ── SVG + centre overlay ─────────────────────────────────────── */
    .ring-wrap {
      position: relative;
      width: 130px;
      height: 130px;
      flex-shrink: 0;
      cursor: default;
    }

    .ring-svg {
      display: block;
    }

    /* Segment brightness transition */
    .ring-seg {
      transition: opacity 0.18s ease;
      opacity: 0.88;
    }
    .ring-seg--lit {
      opacity: 1;
      filter: brightness(1.18);
    }

    /* ── Centre content ───────────────────────────────────────────── */
    .ring-centre {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      gap: 0;
      pointer-events: none;
      transition: opacity 0.2s ease;
    }

    .ring-centre--default  { opacity: 1; }
    .ring-centre--hidden   { opacity: 0; }
    .ring-centre--hover    { opacity: 0; }
    .ring-centre--visible  { opacity: 1; }

    .ring-centre__big {
      font-family: var(--ff-display);
      font-size: 26px;
      color: var(--navy);
      line-height: 1;
      letter-spacing: -0.02em;
    }

    .ring-centre__big--rate {
      font-size: 19px;
      letter-spacing: -0.01em;
    }

    .ring-centre__unit {
      font-size: 13px;
      font-family: var(--ff-body);
      color: var(--navy2);
      font-weight: 500;
    }

    .ring-centre__lbl {
      font-size: 10px;
      font-family: var(--ff-body);
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.06em;
      color: var(--muted);
      margin-top: 2px;
    }

    .ring-centre__sub {
      font-size: 9px;
      font-family: var(--ff-mono);
      color: var(--muted);
      margin-top: 2px;
    }

    /* ── Right stats column ───────────────────────────────────────── */
    .ring-stats {
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: 0;
    }

    .ring-stat {
      display: flex;
      align-items: center;
      gap: 7px;
      cursor: default;
      border-radius: 6px;
      padding: 4px 6px 4px 2px;
      transition: background 0.14s;

      &:hover {
        background: rgba(26, 43, 74, 0.04);
      }
    }

    .ring-stat__dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .ring-stat__label {
      font-size: 12px;
      font-family: var(--ff-body);
      font-weight: 600;
      color: var(--charcoal);
      width: 28px;
      flex-shrink: 0;
    }

    .ring-stat__count {
      font-family: var(--ff-mono);
      font-size: 12px;
      font-weight: 700;
      white-space: nowrap;
    }

    .ring-stat__count--easy   { color: #1D9E75; }
    .ring-stat__count--medium { color: #C8A951; }
    .ring-stat__count--hard   { color: #D32F2F; }

    .ring-stat__total {
      font-weight: 400;
      color: var(--muted);
      font-size: 11px;
    }
  `],
})
export class SolvedRingComponent implements OnChanges {
  @Input() data!: RingDifficultyData;

  // Expose constants to template
  readonly RADIUS      = RING_RADIUS;
  readonly TRACK_COLOR = COLOR_TRACK;

  hovered = false;
  hoveredSegment: 'easy' | 'medium' | 'hard' | null = null;

  easyArc:   ArcSegment = this.emptyArc('easy',   'Easy',   COLOR_EASY);
  mediumArc: ArcSegment = this.emptyArc('medium', 'Medium', COLOR_MEDIUM);
  hardArc:   ArcSegment = this.emptyArc('hard',   'Hard',   COLOR_HARD);

  ngOnChanges(): void {
    if (this.data) {
      this.buildArcs();
    }
  }

  private buildArcs(): void {
    const { easySolved, mediumSolved, hardSolved } = this.data;
    const total = easySolved + mediumSolved + hardSolved;

    // Available arc fraction after removing gaps
    const usable = 1 - TOTAL_GAP_FRAC;

    // Each difficulty gets a share proportional to solved count, with a
    // minimum visible arc (2° / 360) if they have at least 1 solved problem
    const MIN_FRAC = 2 / 360;

    let easyFrac   = total > 0 ? (easySolved   / total) * usable : 0;
    let mediumFrac = total > 0 ? (mediumSolved  / total) * usable : 0;
    let hardFrac   = total > 0 ? (hardSolved    / total) * usable : 0;

    // Enforce minimum visible arc
    if (easySolved   > 0 && easyFrac   < MIN_FRAC) easyFrac   = MIN_FRAC;
    if (mediumSolved > 0 && mediumFrac < MIN_FRAC) mediumFrac = MIN_FRAC;
    if (hardSolved   > 0 && hardFrac   < MIN_FRAC) hardFrac   = MIN_FRAC;

    // Convert fractions → dasharray/dashoffset values
    // SVG draws from the top (after rotate(-90)), each segment sits at:
    //   easyOffset   = 0 (starts at top)
    //   mediumOffset = easy arc + gap
    //   hardOffset   = easy arc + gap + medium arc + gap
    const gapLen = GAP_FRAC * CIRCUMFERENCE;

    this.easyArc = {
      ...this.emptyArc('easy', 'Easy', COLOR_EASY),
      solved:     easySolved,
      total:      this.data.easyTotal,
      dashArray:  `${easyFrac * CIRCUMFERENCE} ${CIRCUMFERENCE}`,
      dashOffset: `${0}`,
    };

    const mediumStart = easyFrac * CIRCUMFERENCE + gapLen;
    this.mediumArc = {
      ...this.emptyArc('medium', 'Medium', COLOR_MEDIUM),
      solved:     mediumSolved,
      total:      this.data.mediumTotal,
      dashArray:  `${mediumFrac * CIRCUMFERENCE} ${CIRCUMFERENCE}`,
      dashOffset: `${-mediumStart}`,
    };

    const hardStart = mediumStart + mediumFrac * CIRCUMFERENCE + gapLen;
    this.hardArc = {
      ...this.emptyArc('hard', 'Hard', COLOR_HARD),
      solved:     hardSolved,
      total:      this.data.hardTotal,
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
      solved:     0,
      total:      0,
      dashArray:  `0 ${CIRCUMFERENCE}`,
      dashOffset: '0',
    };
  }
}
