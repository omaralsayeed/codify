import {
  Component, inject, signal, computed,
  ChangeDetectionStrategy, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { UpdateProfileDto } from '../../../core/models/user.model';

type Mode = 'view' | 'edit';

@Component({
  selector: 'app-instructor-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-profile.component.html',
  styleUrl: './instructor-profile.component.scss',
})
export class InstructorProfileComponent implements OnInit {
  readonly auth   = inject(AuthService);
  private readonly router = inject(Router);

  // ── View / edit mode ──────────────────────────────────────────────────────
  readonly mode = signal<Mode>('view');

  // ── Edit form state ───────────────────────────────────────────────────────
  form: UpdateProfileDto = {
    fullName: '', headline: '', bio: '', organization: '',
    social: { linkedin: '', github: '', twitter: '' },
  };

  readonly saving    = signal(false);
  readonly saveError = signal<string | null>(null);
  readonly saveOk    = signal(false);

  // ── Avatar upload state ───────────────────────────────────────────────────
  readonly avatarUploading = signal(false);
  readonly avatarError     = signal<string | null>(null);

  // ── Derived ───────────────────────────────────────────────────────────────
  readonly user = computed(() => this.auth.currentUser());

  readonly avatarColor = computed(() => {
    const initials = this.user()?.avatarInitials ?? 'IN';
    const palette  = ['#2E86AB', '#1D9E75', '#C8A951', '#7B1FA2', '#E65100'];
    const idx = (initials.charCodeAt(0) + (initials.charCodeAt(1) || 0)) % palette.length;
    return palette[idx];
  });

  readonly joinedYear = computed(() => {
    const j = this.user()?.joinedAt;
    return j ? new Date(j).getFullYear() : null;
  });

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    if (!this.auth.isLoggedIn()) {
      this.router.navigate(['/auth/login']);
      return;
    }
    this.resetForm();
  }

  // ── Edit helpers ──────────────────────────────────────────────────────────

  startEdit(): void {
    this.resetForm();
    this.saveError.set(null);
    this.saveOk.set(false);
    this.mode.set('edit');
  }

  cancelEdit(): void {
    this.mode.set('view');
  }

  private resetForm(): void {
    const u = this.user();
    if (!u) return;
    this.form = {
      fullName:     u.name,
      headline:     u.headline     ?? '',
      bio:          u.bio          ?? '',
      organization: u.organization ?? '',
      social: {
        linkedin: u.social?.linkedin ?? '',
        github:   u.social?.github   ?? '',
        twitter:  u.social?.twitter  ?? '',
      },
    };
  }

  // ── Validation ────────────────────────────────────────────────────────────

  private validate(): string | null {
    if (!this.form.fullName?.trim()) return 'Full name is required.';
    if (this.form.fullName.trim().length < 2) return 'Full name must be at least 2 characters.';
    const links: [string, string | undefined][] = [
      ['LinkedIn', this.form.social?.linkedin],
      ['GitHub',   this.form.social?.github],
      ['Twitter',  this.form.social?.twitter],
    ];
    for (const [label, val] of links) {
      if (val?.trim() && !val.trim().startsWith('http')) {
        return `${label} URL must start with http:// or https://`;
      }
    }
    return null;
  }

  // ── Save ──────────────────────────────────────────────────────────────────

  onSave(): void {
    const err = this.validate();
    if (err) { this.saveError.set(err); return; }

    this.saveError.set(null);
    this.saving.set(true);

    const payload: UpdateProfileDto = {
      fullName:     this.form.fullName.trim(),
      headline:     this.form.headline?.trim()     || undefined,
      bio:          this.form.bio?.trim()           || undefined,
      organization: this.form.organization?.trim() || undefined,
      social: {
        linkedin: this.form.social?.linkedin?.trim() || undefined,
        github:   this.form.social?.github?.trim()   || undefined,
        twitter:  this.form.social?.twitter?.trim()  || undefined,
      },
    };

    this.auth.updateProfile(payload).subscribe(result => {
      this.saving.set(false);
      if (result.success) {
        this.saveOk.set(true);
        setTimeout(() => { this.saveOk.set(false); this.mode.set('view'); }, 1000);
      } else {
        this.saveError.set(result.error ?? 'Could not save profile.');
      }
    });
  }

  // ── Avatar upload ─────────────────────────────────────────────────────────

  onAvatarChange(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;

    if (!file.type.startsWith('image/')) {
      this.avatarError.set('Please select an image file.');
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.avatarError.set('Image must be under 5 MB.');
      return;
    }

    this.avatarError.set(null);
    this.avatarUploading.set(true);

    // Read as base64 for local preview — in production this goes to Cloudinary
    const reader = new FileReader();
    reader.onload = () => {
      const base64 = reader.result as string;
      // Use existing setAvatarUrl which handles local + backend persistence
      this.auth.setAvatarUrl(base64);
      this.avatarUploading.set(false);
    };
    reader.onerror = () => {
      this.avatarError.set('Could not read the image. Please try again.');
      this.avatarUploading.set(false);
    };
    reader.readAsDataURL(file);
  }

  triggerAvatarInput(): void {
    document.getElementById('avatar-input')?.click();
  }

  // ── Misc helpers ──────────────────────────────────────────────────────────

  get bioLength(): number { return this.form.bio?.length ?? 0; }
}
