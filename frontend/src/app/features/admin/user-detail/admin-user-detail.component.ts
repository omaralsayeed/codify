import {
  Component, inject, OnInit, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  AdminService, AdminUserDetail,
} from '../../../core/services/admin.service';

@Component({
  selector: 'app-admin-user-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-user-detail.component.html',
  styleUrl:    './admin-user-detail.component.scss',
})
export class AdminUserDetailComponent implements OnInit {
  private readonly route    = inject(ActivatedRoute);
  private readonly adminSvc = inject(AdminService);

  // ── State ──────────────────────────────────────────────────────────────────
  readonly user          = signal<AdminUserDetail | 'not-found' | null>(null);
  readonly isLoading     = signal(true);
  readonly error         = signal<string | null>(null);

  // ── Modal state ────────────────────────────────────────────────────────────
  readonly confirmAction = signal<'activate' | 'set-pending' | null>(null);
  readonly isToggling    = signal(false);
  readonly toggleError   = signal<string | null>(null);

  // ── Init ───────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadUser(id);
  }

  private loadUser(id: string): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.adminSvc.getUserById(id).subscribe({
      next: user => {
        this.user.set(user);
        this.isLoading.set(false);
      },
      error: err => {
        if (err.status === 404) {
          this.user.set('not-found');
        } else {
          this.error.set('Failed to load user. Make sure the backend is running.');
        }
        this.isLoading.set(false);
      },
    });
  }

  // ── Status toggle ──────────────────────────────────────────────────────────
  requestStatusChange(action: 'activate' | 'set-pending'): void {
    this.toggleError.set(null);
    this.confirmAction.set(action);
  }

  confirmChange(): void {
    const action  = this.confirmAction();
    const current = this.user();
    if (!action || !current || current === 'not-found') return;

    const newStatus = action === 'activate' ? 'active' : 'pending';
    this.isToggling.set(true);

    this.adminSvc.updateUserStatus(current.id, newStatus).subscribe({
      next: updated => {
        this.user.set(updated);      // patch signal with fresh server response
        this.isToggling.set(false);
        this.closeModal();
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
    this.confirmAction.set(null);
    this.toggleError.set(null);
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  avatarBg(role: string): string {
    return role === 'instructor' ? 'avatar--gold' : 'avatar--blue';
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
