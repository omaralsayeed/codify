import { Component } from '@angular/core';

@Component({
  selector: 'app-instructor-overview',
  standalone: true,
  template: `
    <section class="section-placeholder">
      <h1 class="section-title">Class overview</h1>
      <p class="section-sub">Metrics and topic performance will be added in the next chunk.</p>
    </section>
  `,
  styles: `
    .section-title {
      font-family: var(--ff-display);
      font-size: 20px;
      color: var(--navy);
      margin-bottom: 8px;
    }

    .section-sub {
      font-size: 14px;
      color: var(--muted);
    }
  `,
})
export class InstructorOverviewComponent {}
