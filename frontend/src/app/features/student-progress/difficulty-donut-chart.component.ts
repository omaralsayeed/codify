import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  ElementRef,
  ViewChild,
  SimpleChanges,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Chart, registerables, Plugin } from 'chart.js';
import { DifficultyBreakdown } from '../../core/models/analytics.model';

Chart.register(...registerables);

@Component({
  selector: 'app-difficulty-donut-chart',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="donut-wrap">

      <!-- Chart canvas with centre-text overlay -->
      <div class="donut-canvas-wrap">
        <canvas #canvas
                role="img"
                [attr.aria-label]="'Donut chart: ' + total + ' problems solved'">
        </canvas>
        <!-- Centre label rendered in HTML to avoid Canvas font-scaling issues -->
        <div class="donut-centre" aria-hidden="true">
          <span class="donut-centre__num">{{ total }}</span>
          <span class="donut-centre__lbl">Solved</span>
        </div>
      </div>

      <!-- Legend -->
      <div class="donut-legend" role="list" aria-label="Difficulty legend">
        <span class="donut-legend__item" role="listitem">
          <span class="donut-legend__dot donut-legend__dot--easy"></span>
          Easy ({{ breakdown.easy }})
        </span>
        <span class="donut-legend__sep" aria-hidden="true">·</span>
        <span class="donut-legend__item" role="listitem">
          <span class="donut-legend__dot donut-legend__dot--medium"></span>
          Medium ({{ breakdown.medium }})
        </span>
        <span class="donut-legend__sep" aria-hidden="true">·</span>
        <span class="donut-legend__item" role="listitem">
          <span class="donut-legend__dot donut-legend__dot--hard"></span>
          Hard ({{ breakdown.hard }})
        </span>
      </div>

    </div>
  `,
  styles: [`
    .donut-wrap {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 16px;
      width: 100%;
    }

    .donut-canvas-wrap {
      position: relative;
      width: min(220px, 100%);
      aspect-ratio: 1 / 1;
    }

    canvas {
      display: block;
      width: 100% !important;
      height: 100% !important;
    }

    /* Centre text sits on top of the canvas via absolute positioning */
    .donut-centre {
      position: absolute;
      inset: 0;
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      pointer-events: none;
      /* font values inline to avoid needing host SCSS — colours via CSS vars */
      &__num {
        font-family: var(--ff-display);
        font-size: 32px;
        color: var(--navy);
        line-height: 1;
      }
      &__lbl {
        font-family: var(--ff-body);
        font-size: 11px;
        color: var(--muted);
        text-transform: uppercase;
        letter-spacing: 0.06em;
        font-weight: 600;
        margin-top: 2px;
      }
    }

    .donut-legend {
      display: flex;
      align-items: center;
      gap: 6px;
      flex-wrap: wrap;
      justify-content: center;
      font-family: var(--ff-body);
      font-size: 12px;
      color: var(--muted);
    }

    .donut-legend__item {
      display: flex;
      align-items: center;
      gap: 5px;
    }

    .donut-legend__dot {
      width: 9px;
      height: 9px;
      border-radius: 50%;
      flex-shrink: 0;
      &--easy   { background: var(--teal); }
      &--medium { background: var(--gold); }
      &--hard   { background: var(--red);  }
    }

    .donut-legend__sep {
      color: var(--border);
      font-size: 16px;
      line-height: 1;
    }
  `],
})
export class DifficultyDonutChartComponent implements OnChanges, OnDestroy {
  @Input() breakdown!: DifficultyBreakdown;
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;

  get total(): number {
    return (this.breakdown?.easy ?? 0)
         + (this.breakdown?.medium ?? 0)
         + (this.breakdown?.hard ?? 0);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['breakdown'] && this.breakdown) {
      this.buildChart();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private buildChart(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;

    this.chart?.destroy();

    const cs   = getComputedStyle(document.documentElement);
    const teal = cs.getPropertyValue('--teal').trim();
    const gold = cs.getPropertyValue('--gold').trim();
    const red  = cs.getPropertyValue('--red').trim();
    const navy = cs.getPropertyValue('--navy').trim();

    this.chart = new Chart(canvas, {
      type: 'doughnut',
      data: {
        labels:   ['Easy', 'Medium', 'Hard'],
        datasets: [{
          data:             [this.breakdown.easy, this.breakdown.medium, this.breakdown.hard],
          backgroundColor:  [teal, gold, red],
          borderColor:      '#ffffff',
          borderWidth:      3,
          hoverOffset:      6,
        }],
      },
      options: {
        responsive:          true,
        maintainAspectRatio: true,
        cutout:              '68%',
        animation:           {
          animateRotate: true,
          animateScale:  false,
          duration:      900,
          easing:        'easeOutQuart',
        },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: navy,
            titleFont:  { family: 'DM Sans', size: 12 },
            bodyFont:   { family: 'DM Sans', size: 13 },
            padding:    10,
            callbacks: {
              label: ctx => {
                const val = ctx.parsed;
                const pct = this.total > 0
                  ? Math.round((val / this.total) * 100)
                  : 0;
                return ` ${val} problems (${pct}%)`;
              },
            },
          },
        },
      },
    });
  }
}
