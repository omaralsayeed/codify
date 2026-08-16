import {
  Component, inject, ChangeDetectionStrategy,
  signal, computed, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { InstructorService } from '../../../core/services/instructor.service';
import { InstructorStudentSummary } from '../../../core/models/instructor.model';

type SortField = 'name' | 'avgScore' | 'problemsSolved';
type SortDir   = 'asc' | 'desc';

@Component({
  selector: 'app-instructor-students',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-students.component.html',
  styleUrl: './instructor-students.component.scss',
})
export class InstructorStudentsComponent implements OnInit {
  private readonly instructorSvc = inject(InstructorService);

  protected readonly allStudents = signal<InstructorStudentSummary[]>(this.instructorSvc.getStudents());

  // ── Search & sort state ────────────────────────────────────────────────────
  readonly searchQuery  = signal('');
  readonly sortField    = signal<SortField>('avgScore');
  readonly sortDir      = signal<SortDir>('desc');

  ngOnInit(): void {
    this.instructorSvc.getOverview$().subscribe(() => {
      this.allStudents.set(this.instructorSvc.getStudents());
    });
  }

  // ── Derived list ──────────────────────────────────────────────────────────
  readonly students = computed(() => {
    const q     = this.searchQuery().toLowerCase().trim();
    const field = this.sortField();
    const dir   = this.sortDir();
    const list  = this.allStudents();

    const filtered = q
      ? list.filter(s => s.name.toLowerCase().includes(q))
      : [...list];

    return filtered.sort((a, b) => {
      let cmp = 0;
      if (field === 'name') {
        cmp = a.name.localeCompare(b.name);
      } else {
        cmp = a[field] - b[field];
      }
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  // ── Sort toggle ───────────────────────────────────────────────────────────
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

  // ── Integrity badge helpers ───────────────────────────────────────────────
  integrityClass(status: string): string {
    if (status === 'flagged') return 'badge--flagged';
    if (status === 'review')  return 'badge--review';
    return 'badge--clean';
  }

  integrityLabel(status: string): string {
    if (status === 'flagged') return 'Flagged';
    if (status === 'review')  return 'Review';
    return 'Clean';
  }
}
