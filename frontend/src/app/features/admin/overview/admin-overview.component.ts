import {
  Component, ChangeDetectionStrategy, OnInit, inject, signal, computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminService, AdminStats } from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-overview.component.html',
  styleUrl: './admin-overview.component.scss',
})
export class AdminOverviewComponent implements OnInit {
  private readonly adminSvc = inject(AdminService);

  // ── State ──────────────────────────────────────────────────────────────────
  readonly isLoading = signal(true);
  readonly error     = signal<string | null>(null);
  readonly stats     = signal<AdminStats | null>(null);

  // ── Derived cards (only computed once stats are loaded) ────────────────────
  readonly statCards = computed(() => {
    const s = this.stats();
    if (!s) return [];
    return [
      {
        label:      'Total Users',
        value:      s.totalUsers,
        sub:        `↑ ${s.newUsersThisWeek} this week`,
        icon:       '👥',
        colorClass: 'card--blue',
      },
      {
        label:      'Total Problems',
        value:      s.totalProblems,
        sub:        'active on platform',
        icon:       '🗂',
        colorClass: 'card--teal',
      },
      {
        label:      'Pending Instructors',
        value:      s.pendingInstructors,
        sub:        `${s.activeInstructors} active`,
        icon:       '⏳',
        colorClass: s.pendingInstructors > 0 ? 'card--orange' : 'card--teal',
      },
      {
        label:      'Submissions Today',
        value:      s.submissionsToday,
        sub:        `${s.totalSubmissions.toLocaleString()} all time`,
        icon:       '⚡',
        colorClass: 'card--gold',
      },
    ];
  });

  readonly infoItems = computed(() => {
    const s = this.stats();
    if (!s) return [];
    return [
      { label: 'Students',           value: s.totalStudents },
      { label: 'Active Instructors', value: s.activeInstructors },
      { label: 'New Today',          value: s.newUsersToday },
      { label: 'Total Submissions',  value: s.totalSubmissions.toLocaleString() },
    ];
  });

  // ── Lifecycle ──────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.adminSvc.getStats().subscribe({
      next: data => {
        this.stats.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load stats. Make sure the backend is running.');
        this.isLoading.set(false);
      },
    });
  }
}
