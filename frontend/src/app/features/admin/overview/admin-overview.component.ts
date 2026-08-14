import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

// ── Mock data types ───────────────────────────────────────────────────────────

interface AdminStats {
  totalUsers: number;
  totalStudents: number;
  totalInstructors: number;
  activeInstructors: number;
  pendingInstructors: number;
  totalProblems: number;
  totalSubmissions: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  submissionsToday: number;
}

// ── Mock data (replace with HTTP call when backend is ready) ──────────────────

const MOCK_STATS: AdminStats = {
  totalUsers:          124,
  totalStudents:       118,
  totalInstructors:      6,
  activeInstructors:     3,
  pendingInstructors:    3,
  totalProblems:        32,
  totalSubmissions:   4820,
  newUsersToday:         5,
  newUsersThisWeek:     18,
  submissionsToday:     87,
};

// ── Component ─────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-admin-overview',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-overview.component.html',
  styleUrl: './admin-overview.component.scss',
})
export class AdminOverviewComponent {

  readonly stats = MOCK_STATS;

  // ── Top stat cards ──────────────────────────────────────────────────────────
  readonly statCards = [
    {
      label:     'Total Users',
      value:     this.stats.totalUsers,
      sub:       `↑ ${this.stats.newUsersThisWeek} this week`,
      icon:      '👥',
      colorClass: 'card--blue',
    },
    {
      label:     'Total Problems',
      value:     this.stats.totalProblems,
      sub:       'active on platform',
      icon:      '🗂',
      colorClass: 'card--teal',
    },
    {
      label:     'Pending Instructors',
      value:     this.stats.pendingInstructors,
      sub:       `${this.stats.activeInstructors} active`,
      icon:      '⏳',
      colorClass: this.stats.pendingInstructors > 0 ? 'card--orange' : 'card--teal',
    },
    {
      label:     'Submissions Today',
      value:     this.stats.submissionsToday,
      sub:       `${this.stats.totalSubmissions.toLocaleString()} all time`,
      icon:      '⚡',
      colorClass: 'card--gold',
    },
  ];

  // ── Secondary info row ──────────────────────────────────────────────────────
  readonly infoItems = [
    { label: 'Students',           value: this.stats.totalStudents },
    { label: 'Active Instructors', value: this.stats.activeInstructors },
    { label: 'New Today',          value: this.stats.newUsersToday },
    { label: 'Total Submissions',  value: this.stats.totalSubmissions.toLocaleString() },
  ];
}
