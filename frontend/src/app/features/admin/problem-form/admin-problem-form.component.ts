import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-problem-form',
  standalone: true,
  template: `
    <div class="placeholder">
      <h1>Problem Form</h1>
      <p>Add / edit problem — coming soon.</p>
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
export class AdminProblemFormComponent {}
