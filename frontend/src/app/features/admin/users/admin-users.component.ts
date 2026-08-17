import {
  Component, ChangeDetectionStrategy, OnInit, inject,
  signal, computed, effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AdminService, AdminUserRow,
} from '../../../core/services/admin.service';

// Re-export so user-detail can import the type from here (backward compat)
export type { AdminUserRow };

type RoleFilter   = 'all' | 'student' | 'instructor';
type StatusFilter = 'all' | 'active' | 'pending';
type SortField    = 'name' | 'registeredAt' | 'lastActiveAt' | 'role';
type SortDir      = 'asc' | 'desc';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-users.component.html',
  styleUrl:    './admin-users.component.scss',
})
export class AdminUsersComponent implements OnInit {
  private readonly adminSvc = inject(AdminService);

  // ── Filter & sort state ────────────────────────────────────────────────────
  readonly searchQuery  = signal('');
  readonly roleFilter   = signal<RoleFilter>('all');
  readonly statusFilter = signal<StatusFilter>('all');
  readonly sortField    = signal<SortField>('registeredAt');
  readonly sortDir      = signal<SortDir>('desc');

  // ── Data state ─────────────────────────────────────────────────────────────
  readonly isLoading    = signal(true);
  readonly error        = signal<string | null>(null);
  readonly allUsers     = signal<AdminUserRow[]>([]);
  readonly serverTotal  = signal(0);

  // ── Modal state ────────────────────────────────────────────────────────────
  readonly confirmUser   = signal<AdminUserRow | null>(null);
  readonly confirmAction = signal<'activate' | 'set-pending' | null>(null);
  readonly isToggling    = signal(false);
  readonly toggleError   = signal<string | null>(null);

  // ── Derived counts (from loaded data) ─────────────────────────────────────
  readonly totalCount      = computed(() => this.serverTotal());
  readonly pendingCount    = computed(() => this.allUsers().filter(u => u.status === 'pending').length);
  readonly studentCount    = computed(() => this.allUsers().filter(u => u.role === 'student').length);
  readonly instructorCount = computed(() => this.allUsers().filter(u => u.role === 'instructor').length);

  // ── Client-side filter on loaded data (search + role + status) ────────────
  // Sorting is sent to backend; search/role/status filters happen client-side
  // on the current page result for instant UX
  readonly filteredUsers = computed(() => {
    const q      = this.searchQuery().toLowerCase().trim();
    const role   = this.roleFilter();
    const status = this.statusFilter();
    let list = this.allUsers();

    if (q)              list = list.filter(u =>
      u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)
    );
    if (role !== 'all')   list = list.filter(u => u.role === role);
    if (status !== 'all') list = list.filter(u => u.status === status);
    return list;
  });

  constructor() {
    // Re-fetch when sort changes (backend handles sort)
    effect(() => {
      this.sortField();
      this.sortDir();
      this.loadUsers();
    });
  }

  ngOnInit(): void {
    // Initial load handled by the effect above
  }

  // ── HTTP load ──────────────────────────────────────────────────────────────
  loadUsers(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.adminSvc.getUsers({
      sortBy:  this.sortField() as any,
      sortDir: this.sortDir(),
      pageSize: 100,   // load all for now; add pagination UI later
    }).subscribe({
      next: ({ users, total }) => {
        this.allUsers.set(users);
        this.serverTotal.set(total);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load users. Make sure the backend is running.');
        this.isLoading.set(false);
      },
    });
  }

  // ── Sort ───────────────────────────────────────────────────────────────────
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
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  timeAgo(iso: string | null): string {
    if (!iso) return 'Never';
    const diff   = Date.now() - new Date(iso).getTime();
    const mins   = Math.floor(diff / 60000);
    if (mins < 60)  return `${mins}m ago`;
    const hrs    = Math.floor(mins / 60);
    if (hrs < 24)   return `${hrs}h ago`;
    const days   = Math.floor(hrs / 24);
    if (days < 30)  return `${days}d ago`;
    return `${Math.floor(days / 30)}mo ago`;
  }

  // ── Status toggle ──────────────────────────────────────────────────────────
  requestStatusChange(user: AdminUserRow, event: Event): void {
    event.stopPropagation();
    this.toggleError.set(null);
    this.confirmUser.set(user);
    this.confirmAction.set(user.status === 'active' ? 'set-pending' : 'activate');
  }

  confirmChange(): void {
    const user   = this.confirmUser();
    const action = this.confirmAction();
    if (!user || !action) return;

    const newStatus = action === 'activate' ? 'active' : 'pending';
    this.isToggling.set(true);

    this.adminSvc.updateUserStatus(user.id, newStatus).subscribe({
      next: () => {
        this.isToggling.set(false);
        this.closeModal();
        this.loadUsers();   // re-fetch to get fresh server state
      },
      error: err => {
        this.isToggling.set(false);
        const code = err?.error?.errorCode;
        if (code === 'FORBIDDEN') {
          this.toggleError.set('Cannot change status of an admin account.');
        } else {
          this.toggleError.set('Failed to update status. Please try again.');
        }
      },
    });
  }

  closeModal(): void {
    this.confirmUser.set(null);
    this.confirmAction.set(null);
    this.toggleError.set(null);
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  clearFilters(): void {
    this.searchQuery.set('');
    this.roleFilter.set('all');
    this.statusFilter.set('all');
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery() !== '' ||
           this.roleFilter()  !== 'all' ||
           this.statusFilter() !== 'all';
  }
}
