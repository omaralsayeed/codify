import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

// ── Types ─────────────────────────────────────────────────────────────────────

export interface AdminUserRow {
  id: string;
  name: string;
  initials: string;
  email: string;
  role: 'student' | 'instructor' | 'admin';
  status: 'active' | 'pending';
  registeredAt: string;   // ISO date
  lastActiveAt: string | null;
  problemsSolved?: number;
  organization?: string;
}

type RoleFilter   = 'all' | 'student' | 'instructor';
type StatusFilter = 'all' | 'active' | 'pending';
type SortField    = 'name' | 'registeredAt' | 'lastActiveAt' | 'role';
type SortDir      = 'asc' | 'desc';

// ── Mock data ─────────────────────────────────────────────────────────────────

const MOCK_USERS: AdminUserRow[] = [
  {
    id: 'u1', name: 'Karim Ahmed',    initials: 'KA', email: 'karim@example.com',
    role: 'student',    status: 'active',  registeredAt: '2026-06-01T10:00:00Z',
    lastActiveAt: '2026-08-13T14:22:00Z', problemsSolved: 38,
  },
  {
    id: 'u2', name: 'Layla Mostafa',  initials: 'LM', email: 'layla@example.com',
    role: 'student',    status: 'active',  registeredAt: '2026-06-03T09:00:00Z',
    lastActiveAt: '2026-08-12T11:00:00Z', problemsSolved: 34,
  },
  {
    id: 'u3', name: 'Omar Sherif',    initials: 'OS', email: 'omar@example.com',
    role: 'student',    status: 'active',  registeredAt: '2026-06-05T08:30:00Z',
    lastActiveAt: '2026-08-11T16:45:00Z', problemsSolved: 31,
  },
  {
    id: 'u4', name: 'Sara Mahmoud',   initials: 'SM', email: 'sara@example.com',
    role: 'student',    status: 'active',  registeredAt: '2026-06-07T11:00:00Z',
    lastActiveAt: '2026-08-10T09:30:00Z', problemsSolved: 29,
  },
  {
    id: 'u5', name: 'Ahmed Hassan',   initials: 'AH', email: 'ahmed@example.com',
    role: 'student',    status: 'active',  registeredAt: '2026-06-10T14:00:00Z',
    lastActiveAt: '2026-08-09T13:00:00Z', problemsSolved: 21,
  },
  {
    id: 'u6', name: 'Nour Ibrahim',   initials: 'NI', email: 'nour@example.com',
    role: 'student',    status: 'active',  registeredAt: '2026-06-12T10:30:00Z',
    lastActiveAt: '2026-08-08T10:00:00Z', problemsSolved: 18,
  },
  {
    id: 'i1', name: 'Dr. Hana Saad',  initials: 'HS', email: 'hana@university.edu',
    role: 'instructor', status: 'active',  registeredAt: '2026-05-15T09:00:00Z',
    lastActiveAt: '2026-08-13T08:00:00Z', organization: 'Cairo University',
  },
  {
    id: 'i2', name: 'Prof. Tarek Ali', initials: 'TA', email: 'tarek@university.edu',
    role: 'instructor', status: 'active',  registeredAt: '2026-05-20T10:00:00Z',
    lastActiveAt: '2026-08-12T09:00:00Z', organization: 'AUC',
  },
  {
    id: 'i3', name: 'Mona Fawzy',     initials: 'MF', email: 'mona@institute.org',
    role: 'instructor', status: 'active',  registeredAt: '2026-05-25T11:00:00Z',
    lastActiveAt: '2026-08-10T15:00:00Z', organization: 'AAST',
  },
  {
    id: 'i4', name: 'Youssef Nabil',  initials: 'YN', email: 'youssef@tech.edu',
    role: 'instructor', status: 'pending', registeredAt: '2026-08-10T12:00:00Z',
    lastActiveAt: null, organization: 'GUC',
  },
  {
    id: 'i5', name: 'Rania Khalil',   initials: 'RK', email: 'rania@college.edu',
    role: 'instructor', status: 'pending', registeredAt: '2026-08-11T14:00:00Z',
    lastActiveAt: null, organization: 'MTI',
  },
  {
    id: 'i6', name: 'Sameh Gamal',    initials: 'SG', email: 'sameh@edu.com',
    role: 'instructor', status: 'pending', registeredAt: '2026-08-12T09:30:00Z',
    lastActiveAt: null, organization: 'Ain Shams',
  },
];

// ── Component ─────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-users.component.html',
  styleUrl:    './admin-users.component.scss',
})
export class AdminUsersComponent {

  // ── State ──────────────────────────────────────────────────────────────────
  readonly searchQuery   = signal('');
  readonly roleFilter    = signal<RoleFilter>('all');
  readonly statusFilter  = signal<StatusFilter>('all');
  readonly sortField     = signal<SortField>('registeredAt');
  readonly sortDir       = signal<SortDir>('desc');

  // confirmation modal state
  readonly confirmUser   = signal<AdminUserRow | null>(null);
  readonly confirmAction = signal<'activate' | 'set-pending' | null>(null);

  private allUsers = signal<AdminUserRow[]>(
    // Admins are never shown in this list
    MOCK_USERS.filter(u => u.role !== 'admin')
  );

  // ── Derived list ───────────────────────────────────────────────────────────
  readonly filteredUsers = computed(() => {
    const q      = this.searchQuery().toLowerCase().trim();
    const role   = this.roleFilter();
    const status = this.statusFilter();
    const field  = this.sortField();
    const dir    = this.sortDir();

    let list = this.allUsers();

    if (q)            list = list.filter(u => u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q));
    if (role !== 'all')   list = list.filter(u => u.role === role);
    if (status !== 'all') list = list.filter(u => u.status === status);

    return [...list].sort((a, b) => {
      let cmp = 0;
      if (field === 'name') {
        cmp = a.name.localeCompare(b.name);
      } else if (field === 'role') {
        cmp = a.role.localeCompare(b.role);
      } else if (field === 'registeredAt') {
        cmp = new Date(a.registeredAt).getTime() - new Date(b.registeredAt).getTime();
      } else if (field === 'lastActiveAt') {
        const aT = a.lastActiveAt ? new Date(a.lastActiveAt).getTime() : 0;
        const bT = b.lastActiveAt ? new Date(b.lastActiveAt).getTime() : 0;
        cmp = aT - bT;
      }
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  readonly totalCount     = computed(() => this.allUsers().length);
  readonly pendingCount   = computed(() => this.allUsers().filter(u => u.status === 'pending').length);
  readonly studentCount   = computed(() => this.allUsers().filter(u => u.role === 'student').length);
  readonly instructorCount = computed(() => this.allUsers().filter(u => u.role === 'instructor').length);

  // ── Sort helpers ───────────────────────────────────────────────────────────
  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDir.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDir.set('desc');
    }
  }

  sortIcon(field: SortField): string {
    if (this.sortField() !== field) return '↕';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  // ── Badge helpers ──────────────────────────────────────────────────────────
  roleBadgeClass(role: string): string {
    if (role === 'instructor') return 'badge--gold';
    if (role === 'admin')      return 'badge--red';
    return 'badge--blue';
  }

  statusBadgeClass(status: string): string {
    return status === 'active' ? 'badge--active' : 'badge--pending';
  }

  avatarClass(role: string): string {
    if (role === 'instructor') return 'avatar--gold';
    if (role === 'admin')      return 'avatar--red';
    return 'avatar--blue';
  }

  // ── Date formatting ────────────────────────────────────────────────────────
  formatDate(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
  }

  timeAgo(iso: string | null): string {
    if (!iso) return 'Never';
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60)   return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24)    return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days < 30)   return `${days}d ago`;
    const months = Math.floor(days / 30);
    return `${months}mo ago`;
  }

  // ── Status toggle (with confirmation modal) ────────────────────────────────
  requestStatusChange(user: AdminUserRow, event: Event): void {
    event.stopPropagation();
    this.confirmUser.set(user);
    this.confirmAction.set(user.status === 'active' ? 'set-pending' : 'activate');
  }

  confirmChange(): void {
    const user   = this.confirmUser();
    const action = this.confirmAction();
    if (!user || !action) return;

    const newStatus = action === 'activate' ? 'active' : 'pending';
    this.allUsers.update(list =>
      list.map(u => u.id === user.id ? { ...u, status: newStatus } : u)
    );
    this.closeModal();
  }

  closeModal(): void {
    this.confirmUser.set(null);
    this.confirmAction.set(null);
  }

  // ── Filter reset ───────────────────────────────────────────────────────────
  clearFilters(): void {
    this.searchQuery.set('');
    this.roleFilter.set('all');
    this.statusFilter.set('all');
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery() !== '' ||
           this.roleFilter() !== 'all' ||
           this.statusFilter() !== 'all';
  }
}
