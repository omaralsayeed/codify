import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  ElementRef,
  ViewChild,
  SimpleChanges,
} from '@angular/core';
import { Chart, registerables, ChartDataset, Point } from 'chart.js';
import { SuccessRateDataPoint } from '../../core/models/analytics.model';

Chart.register(...registerables);

@Component({
  selector: 'app-success-rate-chart',
  standalone: true,
  template: `
    <div class="sr-chart-wrap">
      <canvas #canvas
              role="img"
              aria-label="Line chart of success rate over time"></canvas>
    </div>
  `,
  styles: [`
    .sr-chart-wrap {
      position: relative;
      width: 100%;
      height: 220px;
    }
    canvas { display: block; width: 100% !important; height: 100% !important; }
  `],
})
export class SuccessRateChartComponent implements OnChanges, OnDestroy {
  @Input() dataPoints: SuccessRateDataPoint[] = [];
  @ViewChild('canvas', { static: true }) canvasRef!: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['dataPoints'] && this.dataPoints.length) {
      this.buildChart();
    }
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  /** Called by the parent when the time-range toggle changes. */
  refresh(): void {
    this.buildChart();
  }

  private buildChart(): void {
    const canvas = this.canvasRef?.nativeElement;
    if (!canvas) return;

    this.chart?.destroy();

    const cs     = getComputedStyle(document.documentElement);
    const blue   = cs.getPropertyValue('--blue').trim();
    const teal   = cs.getPropertyValue('--teal').trim();
    const navy   = cs.getPropertyValue('--navy').trim();
    const muted  = cs.getPropertyValue('--muted').trim();
    const border = cs.getPropertyValue('--border').trim();

    // Hollow point for days with solved === 0 — different radius / fill
    const pointBackgroundColors = this.dataPoints.map(p =>
      p.solved === 0 ? 'transparent' : blue
    );
    const pointBorderColors = this.dataPoints.map(p =>
      p.solved === 0 ? muted : blue
    );
    const pointRadii = this.dataPoints.map(p =>
      p.solved === 0 ? 5 : 3
    );
    const pointHoverRadii = this.dataPoints.map(p =>
      p.solved === 0 ? 7 : 6
    );

    const dataset: ChartDataset<'line'> = {
      label:            'Success Rate',
      data:             this.dataPoints.map(p => p.successRate),
      borderColor:      blue,
      backgroundColor:  blue + '18',
      fill:             true,
      tension:          0.42,
      borderWidth:      2.5,
      pointBackgroundColor: pointBackgroundColors,
      pointBorderColor:     pointBorderColors,
      pointBorderWidth:     2,
      pointRadius:          pointRadii,
      pointHoverRadius:     pointHoverRadii,
    };

    this.chart = new Chart(canvas, {
      type: 'line',
      data: {
        labels:   this.dataPoints.map(p => p.label),
        datasets: [dataset],
      },
      options: {
        responsive:          true,
        maintainAspectRatio: false,
        animation:           { duration: 800, easing: 'easeOutQuart' },
        interaction:         { mode: 'index', intersect: false },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: navy,
            titleFont:  { family: 'DM Sans', size: 12 },
            bodyFont:   { family: 'DM Sans', size: 13 },
            padding:    10,
            callbacks: {
              label: ctx => {
                const idx  = ctx.dataIndex;
                const pt   = this.dataPoints[idx];
                const rate = ctx.parsed.y;
                const note = pt.solved === 0 ? ' (no activity)' : ` · ${pt.solved} solved`;
                return ` ${rate}%${note}`;
              },
            },
          },
        },
        scales: {
          x: {
            grid:  { color: border },
            ticks: {
              color: muted,
              font:  { family: 'DM Sans', size: 11 },
              maxRotation: 0,
            },
          },
          y: {
            min: 0,
            max: 100,
            grid:  { color: border },
            ticks: {
              color:    muted,
              font:     { family: 'DM Sans', size: 11 },
              callback: v => `${v}%`,
              stepSize: 25,
            },
          },
        },
      },
    });
  }
}
