import { Component } from '@angular/core';

@Component({
  selector: 'app-instructor-students',
  standalone: true,
  template: `
    <section class="section-placeholder">
      <h1 class="section-title">Students</h1>
      <p class="section-sub">The student list view will be added in chunk 3.</p>
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
export class InstructorStudentsComponent {}
