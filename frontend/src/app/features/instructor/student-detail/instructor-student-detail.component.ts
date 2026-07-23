import { Component, inject, OnInit, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { InstructorService } from '../../../core/services/instructor.service';
import { InstructorStudentDetail } from '../../../core/models/instructor.model';

@Component({
  selector: 'app-instructor-student-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-student-detail.component.html',
  styleUrl: './instructor-student-detail.component.scss',
})
export class InstructorStudentDetailComponent implements OnInit {
  private readonly route          = inject(ActivatedRoute);
  private readonly instructorSvc  = inject(InstructorService);

  readonly student = signal<InstructorStudentDetail | null | 'not-found'>('not-found');

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    const found = this.instructorSvc.getStudentById(id);
    this.student.set(found ?? 'not-found');
  }

  // ── Helpers ───────────────────────────────────────────────────────────────

  barColor(pct: number): string {
    if (pct >= 75) return 'bar--teal';
    if (pct >= 55) return 'bar--blue';
    if (pct >= 40) return 'bar--gold';
    return 'bar--red';
  }

  integrityClass(status: string): string {
    if (status === 'flagged') return 'badge--flagged';
    if (status === 'review')  return 'badge--review';
    return 'badge--clean';
  }

  statusClass(status: string): string {
    if (status === 'Accepted')    return 'status--accepted';
    if (status === 'WrongAnswer') return 'status--wrong';
    return 'status--other';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }
}
