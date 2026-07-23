import { Component } from '@angular/core';

/** Stub — replaced in Chunk 4 with full per-student progress view. */
@Component({
  selector: 'app-instructor-student-detail',
  standalone: true,
  template: `
    <section class="section-placeholder">
      <h1 class="section-title">Student detail</h1>
      <p class="section-sub">Full per-student view coming in the next chunk.</p>
    </section>
  `,
  styles: `
    .section-title { font-family: var(--ff-display); font-size: 20px; color: var(--navy); margin-bottom: 8px; }
    .section-sub   { font-size: 14px; color: var(--muted); }
  `,
})
export class InstructorStudentDetailComponent {}
