import {
  Component, inject, Output, EventEmitter,
  signal, ChangeDetectionStrategy, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UpdateProfileDto } from '../../../core/models/user.model';

@Component({
  selector: 'app-edit-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './edit-profile.component.html',
  styleUrl: './edit-profile.component.scss',
})
export class EditProfileComponent implements OnInit {
  private readonly authSvc = inject(AuthService);

  @Output() close = new EventEmitter<void>();
  @Output() saved = new EventEmitter<void>();

  form: UpdateProfileDto = {
    fullName: '', headline: '', bio: '', organization: '',
    social: { linkedin: '', github: '', twitter: '' },
  };

  readonly saving  = signal(false);
  readonly error   = signal<string | null>(null);
  readonly success = signal(false);

  ngOnInit(): void {
    const u = this.authSvc.currentUser();
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

  private validate(): string | null {
    if (!this.form.fullName?.trim())          return 'Full name is required.';
    if (this.form.fullName.trim().length < 2) return 'Full name must be at least 2 characters.';
    for (const [label, val] of [
      ['LinkedIn', this.form.social?.linkedin],
      ['GitHub',   this.form.social?.github],
      ['Twitter',  this.form.social?.twitter],
    ] as [string, string | undefined][]) {
      if (val?.trim() && !val.trim().startsWith('http'))
        return `${label} URL must start with http:// or https://`;
    }
    return null;
  }

  onSubmit(): void {
    const err = this.validate();
    if (err) { this.error.set(err); return; }
    this.error.set(null);
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

    this.authSvc.updateProfile(payload).subscribe(result => {
      this.saving.set(false);
      if (result.success) {
        this.success.set(true);
        this.saved.emit();
        setTimeout(() => this.close.emit(), 900);
      } else {
        this.error.set(result.error ?? 'Something went wrong.');
      }
    });
  }

  onCancel(): void { this.close.emit(); }

  onBackdropClick(e: MouseEvent): void {
    if ((e.target as HTMLElement).classList.contains('ep-backdrop')) this.close.emit();
  }

  onKeydown(e: KeyboardEvent): void {
    if (e.key === 'Escape') this.close.emit();
  }
}
