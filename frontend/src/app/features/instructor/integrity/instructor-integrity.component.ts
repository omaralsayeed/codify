import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { InstructorService } from '../../../core/services/instructor.service';
import { IntegrityFlag } from '../../../core/models/instructor.model';

/** Severity sort order — high first */
const SEVERITY_ORDER: Record<string, number> = { high: 0, medium: 1, low: 2 };

@Component({
  selector: 'app-instructor-integrity',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-integrity.component.html',
  styleUrl: './instructor-integrity.component.scss',
})
export class InstructorIntegrityComponent {
  private readonly instructorSvc = inject(InstructorService);

  /** Flags sorted: highest severity first, then most recent within the same severity. */
  readonly flags: IntegrityFlag[] = this.instructorSvc
    .getIntegrityFlags()
    .slice()
    .sort((a, b) => {
      const sevDiff = (SEVERITY_ORDER[a.severity] ?? 3) - (SEVERITY_ORDER[b.severity] ?? 3);
      if (sevDiff !== 0) return sevDiff;
      return new Date(b.detectedAt).getTime() - new Date(a.detectedAt).getTime();
    });

  // ── Badge helpers ─────────────────────────────────────────────────────────

  severityClass(severity: string): string {
    if (severity === 'high')   return 'badge--high';
    if (severity === 'medium') return 'badge--medium';
    return 'badge--low';
  }

  severityLabel(severity: string): string {
    return severity.charAt(0).toUpperCase() + severity.slice(1);
  }

  // ── Date helpers ──────────────────────────────────────────────────────────

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  relativeTime(iso: string): string {
    const diff = Date.now() - new Date(iso).getTime();
    const mins  = Math.floor(diff / 60_000);
    const hours = Math.floor(diff / 3_600_000);
    const days  = Math.floor(diff / 86_400_000);

    if (mins  < 60)  return `${mins}m ago`;
    if (hours < 24)  return `${hours}h ago`;
    if (days  === 1) return 'Yesterday';
    if (days  < 7)   return `${days} days ago`;
    return this.formatDate(iso);
  }
}
