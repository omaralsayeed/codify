import {
  Component,
  Input,
  OnChanges,
  OnDestroy,
  ElementRef,
  ViewChild,
  SimpleChanges,
} from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { TopicPerformance } from '../../core/models/analytics.model';

Chart.register(...registerables);

@Component({
  selector: 'app-topic-radar-chart',
  standalone: true,
  template: `
    <div class="radar-wrap">
      <canvas #radarCanvas
              role="img"
              aria-label="Radar chart of topic strength scores"></canvas>
    </div>
  `,
  styles: [`
    .radar-wrap {
      position: relative;
      width: 100%;
      /* keep chart square so the radar looks balanced */
      aspect-ratio: 1 / 1;
      max-height: 340px;
    }
    canvas {
      display: block;
      width: 100% !important;
      height: 100% !important;
    }
  `],
})
export class TopicRadarChartComponent implements OnChanges, OnDestroy {
  @Input() topics: TopicPerformance[] = [];
  @ViewChild('radarCanvas', { static: true })
  canvasRef!: ElementRef<HTMLCanvasElement>;

  private chart?: Chart;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['topics'] && this.topics.length) {
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

    // Pull every colour from CSS custom properties so we never hardcode
    const cs     = getComputedStyle(document.documentElement);
    const blue   = cs.getPropertyValue('--blue').trim();
    const navy   = cs.getPropertyValue('--navy').trim();
    const muted  = cs.getPropertyValue('--muted').trim();
    const border = cs.getPropertyValue('--border').trim();

    this.chart = new Chart(canvas, {
      type: 'radar',
      data: {
        labels: this.topics.map(t => t.topicName),
        datasets: [
          {
            label:            'Strength',
            data:             this.topics.map(t => t.strengthScore),
            borderColor:      blue,
            backgroundColor:  blue + '33',   // 20% opacity
            pointBackgroundColor: blue,
            pointBorderColor:    '#fff',
            pointHoverBackgroundColor: '#fff',
            pointHoverBorderColor:     blue,
            pointRadius:      4,
            pointHoverRadius: 6,
            borderWidth:      2,
          },
        ],
      },
      options: {
        responsive:          true,
        maintainAspectRatio: true,
        animation:           { duration: 900, easing: 'easeOutQuart' },
        plugins: {
          legend: { display: false },
          tooltip: {
            backgroundColor: navy,
            titleFont:  { family: 'DM Sans', size: 12 },
            bodyFont:   { family: 'DM Sans', size: 13 },
            padding:    10,
            callbacks: {
              label: ctx => ` Score: ${ctx.parsed.r}/100`,
            },
          },
        },
        scales: {
          r: {
            min: 0,
            max: 100,
            beginAtZero: true,
            ticks: {
              stepSize:    25,
              color:       muted,
              font:        { family: 'DM Sans', size: 10 },
              backdropColor: 'transparent',
            },
            grid:        { color: border },
            angleLines:  { color: border },
            pointLabels: {
              color: navy,
              font:  { family: 'DM Sans', size: 11, weight: 500 },
            },
          },
        },
      },
    });
  }
}
