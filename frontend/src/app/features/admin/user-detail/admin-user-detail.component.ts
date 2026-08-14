import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminUserRow } from '../users/admin-users.component';

// ── Extended detail type ──────────────────────────────────────────────────────

interface AdminUserDetail extends AdminUserRow {
  streak?: number;
  avgScore?: number;
  totalSubmissions?: number;
  recentSubmissions?: {
    problemTitle: string;
    status: string;
    submittedAt: string;
  }[];
}

// ── Mock detail data (keyed by user id) ───────────────────────────────────────

const MOCK_DETAILS: Record<string, AdminUserDetail> = {
  u1: {
    id: 'u1', name: 'Karim Ahmed', initials: 'KA', email: 'karim@example.com',
    role: 'student', status: 'active', registeredAt: '2026-06-01T10:00:00Z',
    lastActiveAt: '2026-08-13T14:22:00Z', problemsSolved: 38,
    streak: 14, avgScore: 92, totalSubmissions: 61,
    recentSubmissions: [
      { problemTitle: 'Two Sum',           status: 'Accepted',    submittedAt: '2026-08-13T10:45:00Z' },
      { problemTitle: 'Valid Parentheses', status: 'WrongAnswer', submittedAt: '2026-08-12T16:20:00Z' },
      { problemTitle: 'Binary Search',     status: 'Accepted',    submittedAt: '2026-08-11T09:00:00Z' },
      { problemTitle: 'Merge Intervals',   status: 'RuntimeError', submittedAt: '2026-08-10T14:30:00Z' },
      { problemTitle: 'Max Subarray',      status: 'Accepted',    submittedAt: '2026-08-09T11:10:00Z' },
    ],
  },
  u2: {
    id: 'u2', name: 'Layla Mostafa', initials: 'LM', email: 'layla@example.com',
    role: 'student', status: 'active', registeredAt: '2026-06-03T09:00:00Z',
    lastActiveAt: '2026-08-12T11:00:00Z', problemsSolved: 34,
    streak: 7, avgScore: 88, totalSubmissions: 52,
    recentSubmissions: [
      { problemTitle: 'Climbing Stairs',   status: 'Accepted',    submittedAt: '2026-08-12T10:00:00Z' },
      { problemTitle: 'Two Sum',           status: 'Accepted',    submittedAt: '2026-08-11T15:00:00Z' },
    ],
  },
  u3: {
    id: 'u3', name: 'Omar Sherif', initials: 'OS', email: 'omar@example.com',
    role: 'student', status: 'active', registeredAt: '2026-06-05T08:30:00Z',
    lastActiveAt: '2026-08-11T16:45:00Z', problemsSolved: 31,
    streak: 5, avgScore: 85, totalSubmissions: 48,
    recentSubmissions: [
      { problemTitle: 'Linked List Cycle', status: 'Accepted',    submittedAt: '2026-08-11T14:00:00Z' },
    ],
  },
  u4: {
    id: 'u4', name: 'Sara Mahmoud', initials: 'SM', email: 'sara@example.com',
    role: 'student', status: 'active', registeredAt: '2026-06-07T11:00:00Z',
    lastActiveAt: '2026-08-10T09:30:00Z', problemsSolved: 29,
    streak: 3, avgScore: 81, totalSubmissions: 44,
    recentSubmissions: [
      { problemTitle: 'Reverse String',    status: 'Accepted',    submittedAt: '2026-08-10T09:00:00Z' },
    ],
  },
  u5: {
    id: 'u5', name: 'Ahmed Hassan', initials: 'AH', email: 'ahmed@example.com',
    role: 'student', status: 'active', registeredAt: '2026-06-10T14:00:00Z',
    lastActiveAt: '2026-08-09T13:00:00Z', problemsSolved: 21,
    streak: 2, avgScore: 74, totalSubmissions: 35, recentSubmissions: [],
  },
  u6: {
    id: 'u6', name: 'Nour Ibrahim', initials: 'NI', email: 'nour@example.com',
    role: 'student', status: 'active', registeredAt: '2026-06-12T10:30:00Z',
    lastActiveAt: '2026-08-08T10:00:00Z', problemsSolved: 18,
    streak: 1, avgScore: 70, totalSubmissions: 28, recentSubmissions: [],
  },
  i1: {
    id: 'i1', name: 'Dr. Hana Saad', initials: 'HS', email: 'hana@university.edu',
    role: 'instructor', status: 'active', registeredAt: '2026-05-15T09:00:00Z',
    lastActiveAt: '2026-08-13T08:00:00Z', organization: 'Cairo University',
    totalSubmissions: 0, recentSubmissions: [],
  },
  i2: {
    id: 'i2', name: 'Prof. Tarek Ali', initials: 'TA', email: 'tarek@university.edu',
    role: 'instructor', status: 'active', registeredAt: '2026-05-20T10:00:00Z',
    lastActiveAt: '2026-08-12T09:00:00Z', organization: 'AUC',
    totalSubmissions: 0, recentSubmissions: [],
  },
  i3: {
    id: 'i3', name: 'Mona Fawzy', initials: 'MF', email: 'mona@institute.org',
    role: 'instructor', status: 'active', registeredAt: '2026-05-25T11:00:00Z',
    lastActiveAt: '2026-08-10T15:00:00Z', organization: 'AAST',
    totalSubmissions: 0, recentSubmissions: [],
  },
  i4: {
    id: 'i4', name: 'Youssef Nabil', initials: 'YN', email: 'youssef@tech.edu',
    role: 'instructor', status: 'pending', registeredAt: '2026-08-10T12:00:00Z',
    lastActiveAt: null, organization: 'GUC',
    totalSubmissions: 0, recentSubmissions: [],
  },
  i5: {
    id: 'i5', name: 'Rania Khalil', initials: 'RK', email: 'rania@college.edu',
    role: 'instructor', status: 'pending', registeredAt: '2026-08-11T14:00:00Z',
    lastActiveAt: null, organization: 'MTI',
    totalSubmissions: 0, recentSubmissions: [],
  },
  i6: {
    id: 'i6', name: 'Sameh Gamal', initials: 'SG', email: 'sameh@edu.com',
    role: 'instructor', status: 'pending', registeredAt: '2026-08-12T09:30:00Z',
    lastActiveAt: null, organization: 'Ain Shams',
    totalSubmissions: 0, recentSubmissions: [],
  },
};

// ── Component ─────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-admin-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-user-detail.component.html',
  styleUrl:    './admin-user-detail.component.scss',
})
export class AdminUserDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);

  readonly user          = signal<AdminUserDetail | 'not-found' | null>(null);
  readonly confirmAction = signal<'activate' | 'set-pending' | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    const found = MOCK_DETAILS[id];
    this.user.set(found ?? 'not-found');
  }

  // ── Status toggle ──────────────────────────────────────────────────────────
  requestStatusChange(action: 'activate' | 'set-pending'): void {
    this.confirmAction.set(action);
  }

  confirmChange(): void {
    const action = this.confirmAction();
    if (!action) return;
    const current = this.user();
    if (!current || current === 'not-found') return;

    const newStatus = action === 'activate' ? 'active' : 'pending';
    this.user.set({ ...current, status: newStatus });
    this.confirmAction.set(null);
  }

  closeModal(): void {
    this.confirmAction.set(null);
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  avatarBg(role: string): string {
    if (role === 'instructor') return 'avatar--gold';
    return 'avatar--blue';
  }

  roleBadgeClass(role: string): string {
    if (role === 'instructor') return 'badge--gold';
    if (role === 'admin')      return 'badge--red';
    return 'badge--blue';
  }

  statusBadgeClass(status: string): string {
    return status === 'active' ? 'badge--active' : 'badge--pending';
  }

  statusClass(status: string): string {
    if (status === 'Accepted')    return 'status--accepted';
    if (status === 'WrongAnswer') return 'status--wrong';
    return 'status--other';
  }

  formatDate(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  timeAgo(iso: string | null): string {
    if (!iso) return 'Never';
    const diff  = Date.now() - new Date(iso).getTime();
    const mins  = Math.floor(diff / 60000);
    if (mins < 60)  return `${mins}m ago`;
    const hrs   = Math.floor(mins / 60);
    if (hrs < 24)   return `${hrs}h ago`;
    const days  = Math.floor(hrs / 24);
    if (days < 30)  return `${days}d ago`;
    return `${Math.floor(days / 30)}mo ago`;
  }
}
