import {
  Component, inject, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { ProblemService } from '../../../core/services/problem.service';
import { InstructorService } from '../../../core/services/instructor.service';
import { Problem } from '../../../core/models/problem.model';
import { InstructorStudentSummary } from '../../../core/models/instructor.model';

interface FormState {
  title: string;
  description: string;
  startAt: string;
  endAt: string;
}

@Component({
  selector: 'app-instructor-contest-create',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-contest-create.component.html',
  styleUrl: './instructor-contest-create.component.scss',
})
export class InstructorContestCreateComponent {
  private readonly router        = inject(Router);
  private readonly contestSvc    = inject(ContestService);
  private readonly problemSvc    = inject(ProblemService);
  private readonly instructorSvc = inject(InstructorService);

  // ── Source data ───────────────────────────────────────────────────────────
  readonly problems:  Problem[]                   = this.problemSvc.getAll();
  readonly students:  InstructorStudentSummary[]  = this.instructorSvc.getStudents();

  // ── Form fields ───────────────────────────────────────────────────────────
  form: FormState = {
    title:       '',
    description: '',
    startAt:     '',
    endAt:       '',
  };

  selectedProblemIds  = signal<Set<string>>(new Set());
  selectedStudentIds  = signal<Set<string>>(new Set());

  // ── UI state ──────────────────────────────────────────────────────────────
  readonly submitting = signal(false);
  readonly errors     = signal<string[]>([]);

  // ── Multi-select toggle helpers ───────────────────────────────────────────

  toggleProblem(id: string): void {
    this.selectedProblemIds.update(s => {
      const next = new Set(s);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  toggleStudent(id: string): void {
    this.selectedStudentIds.update(s => {
      const next = new Set(s);
      next.has(id) ? next.delete(id) : next.add(id);
      return next;
    });
  }

  isProblemSelected(id: string):  boolean { return this.selectedProblemIds().has(id); }
  isStudentSelected(id: string):  boolean { return this.selectedStudentIds().has(id); }

  difficultyClass(d: string): string {
    if (d === 'easy')   return 'diff--easy';
    if (d === 'medium') return 'diff--medium';
    return 'diff--hard';
  }

  // ── Validation ────────────────────────────────────────────────────────────

  private validate(): string[] {
    const errs: string[] = [];
    if (!this.form.title.trim())               errs.push('Title is required.');
    if (this.selectedProblemIds().size === 0)  errs.push('Select at least one problem.');
    if (this.selectedStudentIds().size === 0)  errs.push('Assign to at least one student.');
    if (!this.form.startAt)                    errs.push('Start date/time is required.');
    if (!this.form.endAt)                      errs.push('End date/time is required.');
    if (this.form.startAt && this.form.endAt &&
        new Date(this.form.endAt) <= new Date(this.form.startAt)) {
      errs.push('End date/time must be after start.');
    }
    return errs;
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  onSubmit(): void {
    const errs = this.validate();
    if (errs.length > 0) {
      this.errors.set(errs);
      return;
    }

    this.errors.set([]);
    this.submitting.set(true);

    try {
      const created = this.contestSvc.createContest({
        title:              this.form.title.trim(),
        description:        this.form.description.trim(),
        problemIds:         [...this.selectedProblemIds()],
        assignedStudentIds: [...this.selectedStudentIds()],
        startAt:            new Date(this.form.startAt).toISOString(),
        endAt:              new Date(this.form.endAt).toISOString(),
      });

      // Navigate to the new contest's detail view
      this.router.navigate(['instructor', 'dashboard', 'contests', created.id]);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Something went wrong.';
      this.errors.set([msg]);
      this.submitting.set(false);
    }
  }
}
