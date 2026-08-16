import { Component, ChangeDetectionStrategy, inject, OnInit, signal, computed } from '@angular/core';
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

  readonly stats = signal<AdminStats>({
    totalUsers: 0,
    activeStudents: 0,
    pendingInstructors: 0,
    activeInstructors: 0,
    totalProblems: 0,
    totalSubmissions: 0,
    passRatePercent: 0,
    totalContests: 0,
    aiFlagsCount: 0,
  });

  ngOnInit(): void {
    this.adminSvc.getStats().subscribe(data => {
      if (data) {
        this.stats.set(data);
      }
    });
  }

  // ── Top stat cards ──────────────────────────────────────────────────────────
  readonly statCards = computed(() => {
    const s = this.stats();
    return [
      {
        label:     'Total Users',
        value:     s.totalUsers,
        sub:       `${s.activeStudents} active students`,
        icon:      '👥',
        colorClass: 'card--blue',
      },
      {
        label:     'Total Problems',
        value:     s.totalProblems,
        sub:       'active in database',
        icon:      '🗂',
        colorClass: 'card--teal',
      },
      {
        label:     'Pending Instructors',
        value:     s.pendingInstructors,
        sub:       `${s.activeInstructors} active`,
        icon:      '⏳',
        colorClass: s.pendingInstructors > 0 ? 'card--orange' : 'card--teal',
      },
      {
        label:     'Submissions Solved',
        value:     s.totalSubmissions,
        sub:       `${s.passRatePercent}% pass rate`,
        icon:      '⚡',
        colorClass: 'card--gold',
      },
    ];
  });

  // ── Secondary info row ──────────────────────────────────────────────────────
  readonly infoItems = computed(() => {
    const s = this.stats();
    return [
      { label: 'Active Students',    value: s.activeStudents },
      { label: 'Active Instructors', value: s.activeInstructors },
      { label: 'Active Contests',    value: s.totalContests },
      { label: 'Total Submissions',  value: s.totalSubmissions.toLocaleString() },
    ];
  });
}
