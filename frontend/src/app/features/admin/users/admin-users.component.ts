import { Component, ChangeDetectionStrategy, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { AdminService, AdminUserRow } from '../../../core/services/admin.service';

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
  private readonly route = inject(ActivatedRoute);

  // ── State ──────────────────────────────────────────────────────────────────
  readonly searchQuery   = signal('');
  readonly roleFilter    = signal<RoleFilter>('all');
  readonly statusFilter  = signal<StatusFilter>('all');
  readonly sortField     = signal<SortField>('registeredAt');
  readonly sortDir       = signal<SortDir>('desc');

  // confirmation modal state
  readonly confirmUser   = signal<AdminUserRow | null>(null);
  readonly confirmAction = signal<'activate' | 'set-pending' | null>(null);

  readonly hasActiveFilters = computed(() =>
    this.searchQuery().trim() !== '' ||
    this.roleFilter() !== 'all' ||
    this.statusFilter() !== 'all'
  );

  clearFilters(): void {
    this.searchQuery.set('');
    this.roleFilter.set('all');
    this.statusFilter.set('all');
  }

  private allUsers = signal<AdminUserRow[]>([]);

  ngOnInit(): void {
    const statusParam = this.route.snapshot.queryParamMap.get('status');
    if (statusParam === 'pending') {
      this.statusFilter.set('pending');
    }

    this.loadUsers();
  }

  loadUsers(): void {
    this.adminSvc.getUsers().subscribe({
      next: (users) => {
        this.allUsers.set(users.filter(u => u.role !== 'admin'));
      },
      error: () => {
        this.allUsers.set([]);
      }
    });
  }

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
      this.sortDir.set('asc');
    }
  }

  sortIcon(field: SortField): string {
    if (this.sortField() !== field) return '↕';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  // ── Quick approve (for pending instructors in the table) ────────────────────
  quickApprove(user: AdminUserRow, event: Event): void {
    event.stopPropagation();
    this.adminSvc.approveInstructor(user.id).subscribe({
      next: () => {
        this.allUsers.update(list =>
          list.map(u => u.id === user.id ? { ...u, status: 'active' } : u)
        );
      }
    });
  }

  // ── Modal confirmation ─────────────────────────────────────────────────────
  openConfirm(user: AdminUserRow, action: 'activate' | 'set-pending'): void {
    this.confirmUser.set(user);
    this.confirmAction.set(action);
  }

  closeConfirm(): void {
    this.confirmUser.set(null);
    this.confirmAction.set(null);
  }

  closeModal(): void {
    this.closeConfirm();
  }

  confirmChange(): void {
    this.executeConfirm();
  }

  executeConfirm(): void {
    const u = this.confirmUser();
    const action = this.confirmAction();
    if (!u || !action) return;

    if (action === 'activate') {
      this.adminSvc.approveInstructor(u.id).subscribe({
        next: () => {
          this.allUsers.update(list =>
            list.map(user => user.id === u.id ? { ...user, status: 'active' } : user)
          );
          this.closeConfirm();
        }
      });
    } else {
      this.adminSvc.updateUserStatus(u.id, 'pending').subscribe({
        next: () => {
          this.allUsers.update(list =>
            list.map(user => user.id === u.id ? { ...user, status: 'pending' } : user)
          );
          this.closeConfirm();
        }
      });
    }
  }

  requestStatusChange(user: AdminUserRow, event?: Event): void {
    if (event) event.stopPropagation();
    const action = user.status === 'active' ? 'set-pending' : 'activate';
    this.openConfirm(user, action);
  }

  // ── Template formatters ────────────────────────────────────────────────────
  formatDate(iso: string | null): string {
    if (!iso) return '—';
    return new Date(iso).toLocaleDateString('en-US', {
      year:  'numeric',
      month: 'short',
      day:   'numeric',
    });
  }

  timeAgo(iso: string | null): string {
    if (!iso) return 'Never';
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    if (days < 30) return `${days}d ago`;
    return `${Math.floor(days / 30)}mo ago`;
  }

  formatDateTime(iso: string | null): string {
    if (!iso) return 'Never';
    const d = new Date(iso);
    return `${d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} at ${d.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })}`;
  }

  avatarClass(role: string): string {
    if (role === 'admin') return 'avatar--admin';
    if (role === 'instructor') return 'avatar--instructor';
    return 'avatar--student';
  }

  roleBadgeClass(role: string): string {
    if (role === 'admin') return 'badge--admin';
    if (role === 'instructor') return 'badge--instructor';
    return 'badge--student';
  }

  statusBadgeClass(status: string): string {
    if (status === 'active') return 'badge--active';
    if (status === 'pending') return 'badge--pending';
    return 'badge--inactive';
  }
}
