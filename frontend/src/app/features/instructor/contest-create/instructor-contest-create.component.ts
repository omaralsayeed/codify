import {
  Component, inject, ChangeDetectionStrategy, signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { ProblemService } from '../../../core/services/problem.service';
import { InstructorService } from '../../../core/services/instructor.service';
import { SearchSelectComponent, SelectItem } from '../../../shared/components/search-select/search-select.component';

interface FormState {
  title: string;
  description: string;
  startAt: string;
  endAt: string;
}

@Component({
  selector: 'app-instructor-contest-create',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, SearchSelectComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './instructor-contest-create.component.html',
  styleUrl: './instructor-contest-create.component.scss',
})
export class InstructorContestCreateComponent {
  private readonly router        = inject(Router);
  private readonly contestSvc    = inject(ContestService);
  private readonly problemSvc    = inject(ProblemService);
  private readonly instructorSvc = inject(InstructorService);

  // ── Form fields ───────────────────────────────────────────────────────────

  form: FormState = {
    title:       '',
    description: '',
    startAt:     '',
    endAt:       '',
  };

  // ── Selected items (SelectItem shape for the shared component) ────────────

  readonly selectedProblems  = signal<SelectItem[]>([]);
  readonly selectedStudents  = signal<SelectItem[]>([]);

  // ── UI state ──────────────────────────────────────────────────────────────

  readonly submitting = signal(false);
  readonly errors     = signal<string[]>([]);

  // ── Search functions (passed to SearchSelectComponent via @Input) ─────────
  // Arrow functions so `this` is bound correctly when passed as a reference.

  readonly searchProblems = (query: string): SelectItem[] =>
    this.problemSvc.searchSync(query).map(p => ({
      id:         p.id,
      label:      p.title,
      badge:      p.difficulty,
      badgeClass: `diff--${p.difficulty}`,
    }));

  readonly searchStudents = (query: string): SelectItem[] =>
    this.instructorSvc.searchStudents(query).map(s => ({
      id:         s.id,
      label:      s.name,
      badge:      s.initials,
      badgeClass: 'avatar-badge',
    }));

  // ── Selection handlers ────────────────────────────────────────────────────

  addProblem(item: SelectItem): void {
    this.selectedProblems.update(list =>
      list.find(x => x.id === item.id) ? list : [...list, item]
    );
  }

  removeProblem(item: SelectItem): void {
    this.selectedProblems.update(list => list.filter(x => x.id !== item.id));
  }

  addStudent(item: SelectItem): void {
    this.selectedStudents.update(list =>
      list.find(x => x.id === item.id) ? list : [...list, item]
    );
  }

  removeStudent(item: SelectItem): void {
    this.selectedStudents.update(list => list.filter(x => x.id !== item.id));
  }

  // ── Validation ────────────────────────────────────────────────────────────

  private validate(): string[] {
    const errs: string[] = [];
    if (!this.form.title.trim())                    errs.push('Title is required.');
    if (this.selectedProblems().length === 0)       errs.push('Select at least one problem.');
    if (this.selectedStudents().length === 0)       errs.push('Assign to at least one student.');
    if (!this.form.startAt)                         errs.push('Start date/time is required.');
    if (!this.form.endAt)                           errs.push('End date/time is required.');
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
        problemIds:         this.selectedProblems().map(p => p.id),
        assignedStudentIds: this.selectedStudents().map(s => s.id),
        startAt:            new Date(this.form.startAt).toISOString(),
        endAt:              new Date(this.form.endAt).toISOString(),
      });

      this.router.navigate(['instructor', 'dashboard', 'contests', created.id]);
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Something went wrong.';
      this.errors.set([msg]);
      this.submitting.set(false);
    }
  }
}
