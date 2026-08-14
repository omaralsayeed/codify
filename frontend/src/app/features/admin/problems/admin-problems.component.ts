import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-problems',
  standalone: true,
  template: `
    <div class="placeholder">
      <h1>Problems</h1>
      <p>Problem management — coming soon.</p>
    </div>
  `,
  styles: [`
    .placeholder {
      padding: 40px;
      h1 { font-size: 24px; margin-bottom: 8px; }
      p  { color: #6B6B68; }
    }
  `]
})
export class AdminProblemsComponent {}
