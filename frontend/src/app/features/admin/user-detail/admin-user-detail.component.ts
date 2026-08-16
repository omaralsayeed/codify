import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AdminService, AdminUserDetail } from '../../../core/services/admin.service';

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
  private readonly adminSvc = inject(AdminService);

  readonly user          = signal<AdminUserDetail | 'not-found' | null>(null);
  readonly confirmAction = signal<'activate' | 'set-pending' | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.adminSvc.getUserById(id).subscribe({
      next: (detail) => {
        this.user.set(detail);
      },
      error: () => {
        this.user.set('not-found');
      }
    });
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
    this.adminSvc.updateUserStatus(current.id, newStatus).subscribe({
      next: () => {
        this.user.set({ ...current, status: newStatus });
        this.confirmAction.set(null);
      }
    });
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
    if (status === 'WrongAnswer' || status === 'Wrong Answer') return 'status--wrong';
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
