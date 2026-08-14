import { Component } from '@angular/core';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  template: `
    <div class="placeholder">
      <h1>Users</h1>
      <p>User management — coming soon.</p>
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
export class AdminUsersComponent {}
