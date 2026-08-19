import {
  Component, inject, ChangeDetectionStrategy, signal, OnInit, ChangeDetectorRef, HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ContestService } from '../../../core/services/contest.service';
import { ProblemService } from '../../../core/services/problem.service';
import { InstructorService } from '../../../core/services/instructor.service';
import { SearchSelectComponent, SelectItem } from '../../../shared/components/search-select/search-select.component';
import { HttpClient, HttpHeaders } from '@angular/common/http';

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
export class InstructorContestCreateComponent implements OnInit {
  private readonly router        = inject(Router);
  private readonly contestSvc    = inject(ContestService);
  private readonly problemSvc    = inject(ProblemService);
  private readonly instructorSvc = inject(InstructorService);
  private readonly cdr           = inject(ChangeDetectorRef);
  private readonly http          = inject(HttpClient);
  private readonly apiBase       = 'http://localhost:5237/api';

  // ── Form fields ───────────────────────────────────────────────────────────

  form: FormState = {
    title:       '',
    description: '',
    startAt:     '',
    endAt:       '',
  };

  // ── Selected items ────────────────────────────────────────────────────────

  readonly selectedProblems  = signal<SelectItem[]>([]);
  readonly selectedStudents  = signal<SelectItem[]>([]);
  readonly selectedEmails    = signal<string[]>([]);
  emailInput = '';  // plain property — compatible with two-way [(ngModel)] under OnPush

  // ── Available items from Database ─────────────────────────────────────────
  private readonly allProblems = signal<SelectItem[]>([]);
  readonly availableStudents   = signal<{ id: string; name: string; email: string }[]>([]);

  // ── UI state ──────────────────────────────────────────────────────────────

  readonly submitting   = signal(false);
  readonly errors       = signal<string[]>([]);
  readonly showSuggest  = signal(false);
  activeIndex           = -1;   // keyboard-highlighted row index

  /**
   * Plain getter — recomputed on every CD cycle triggered by onEmailInput().
   * Cannot use computed() here because emailInput is a plain property (not a signal).
   */
  get emailSuggestions(): { id: string; name: string; email: string }[] {
    const q = this.emailInput.trim().toLowerCase();
    const added = this.selectedEmails();
    if (!q) return [];
    return this.availableStudents()
      .filter(s =>
        !added.includes(s.email) &&
        (s.email.toLowerCase().includes(q) || s.name.toLowerCase().includes(q))
      )
      .slice(0, 8);
  }

  @HostListener('document:click')
  onOutsideClick(): void {
    this.showSuggest.set(false);
  }

  ngOnInit(): void {
    // 1. Fetch real problems from DB
    this.problemSvc.getAll({ pageSize: 100 }).subscribe({
      next: (problems) => {
        if (!problems) return;
        this.allProblems.set(
          problems.map(p => ({
            id:         p.id,
            label:      p.title,
            badge:      p.difficulty,
            badgeClass: `diff--${p.difficulty}`,
          }))
        );
      },
    });

    // 2. Fetch all students for suggestions
    this.contestSvc.searchStudents$().subscribe({
      next: (students) => {
        if (students && students.length > 0) {
          this.availableStudents.set(students);
          this.cdr.markForCheck();
        }
      },
    });
  }

  // ── Search functions ──────────────────────────────────────────────────────

  readonly searchProblems = (query: string): SelectItem[] => {
    const q = query.toLowerCase().trim();
    const list = this.allProblems();
    if (!q) return list.slice(0, 10);
    return list.filter(p => p.label.toLowerCase().includes(q));
  };

  // ── Selection handlers ────────────────────────────────────────────────────

  addProblem(item: SelectItem): void {
    this.selectedProblems.update(list =>
      list.find(x => x.id === item.id) ? list : [...list, item]
    );
  }

  removeProblem(item: SelectItem): void {
    this.selectedProblems.update(list => list.filter(x => x.id !== item.id));
  }

  // ── Email chip handlers ───────────────────────────────────────────────────

  addEmailChip(emailStr: string): void {
    const raw = (emailStr ?? '').trim().toLowerCase();
    if (!raw) return;

    // Handle comma or space separated emails
    const emails = raw.split(/[\s,]+/).filter(e => e.length > 0);
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    this.selectedEmails.update(current => {
      const next = [...current];
      for (const e of emails) {
        if (emailRegex.test(e) && !next.includes(e)) {
          next.push(e);
        }
      }
      return next;
    });

    this.emailInput = '';
    this.showSuggest.set(false);
    this.activeIndex = -1;
    this.cdr.markForCheck();
  }

  onEmailKeydown(event: KeyboardEvent): void {
    const suggestions = this.emailSuggestions;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.activeIndex = Math.min(this.activeIndex + 1, suggestions.length - 1);
      this.cdr.markForCheck();
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.activeIndex = Math.max(this.activeIndex - 1, -1);
      this.cdr.markForCheck();
      return;
    }
    if (event.key === 'Escape') {
      this.showSuggest.set(false);
      this.activeIndex = -1;
      return;
    }
    if ((event.key === 'Enter' || event.key === ',') && this.activeIndex >= 0 && suggestions[this.activeIndex]) {
      event.preventDefault();
      this.pickSuggestion(suggestions[this.activeIndex]);
      return;
    }
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault();
      this.addEmailChip(this.emailInput);
    }
  }

  onEmailInput(): void {
    this.activeIndex = -1;
    const hasMatches = this.emailSuggestions.length > 0;
    this.showSuggest.set(hasMatches);
    this.cdr.markForCheck();
  }

  onEmailBlur(): void {
    const raw = (this.emailInput ?? '').trim();
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (emailRegex.test(raw)) {
      this.addEmailChip(raw);
    }
  }

  pickSuggestion(student: { id: string; name: string; email: string }): void {
    if (student?.email) {
      this.addEmailChip(student.email);
    }
  }

  removeEmail(email: string): void {
    this.selectedEmails.update(list => list.filter(e => e !== email));
  }

  // ── Validation ────────────────────────────────────────────────────────────

  private validate(): string[] {
    const errs: string[] = [];
    if (!this.form.title.trim())                    errs.push('Title is required.');
    if (this.selectedProblems().length === 0)       errs.push('Select at least one problem.');
    if (this.selectedEmails().length === 0)         errs.push('Assign at least one student email for invitation.');
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
    // Also include any leftover text in email input
    if (this.emailInput.trim()) {
      this.addEmailChip(this.emailInput);
    }

    const errs = this.validate();
    if (errs.length > 0) {
      this.errors.set(errs);
      return;
    }

    this.errors.set([]);
    this.submitting.set(true);

    const payload = {
      title:              this.form.title.trim(),
      description:        this.form.description.trim(),
      problemIds:         this.selectedProblems().map(p => p.id),
      studentEmails:      this.selectedEmails(),
      startAt:            new Date(this.form.startAt).toISOString(),
      endAt:              new Date(this.form.endAt).toISOString(),
    };

    this.contestSvc.createContest$(payload).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.router.navigate(['instructor', 'dashboard', 'contests', created.id]);
      },
      error: (e) => {
        const msg = e?.error?.message ?? e?.message ?? 'Failed to create contest.';
        this.errors.set([msg]);
        this.submitting.set(false);
      }
    });
  }
}
