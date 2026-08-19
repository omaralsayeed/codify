import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  AdminService, CreateProblemBody,
} from '../../../core/services/admin.service';
import { difficultyToNumber, Difficulty } from '../../../core/utils/enum-mappers';

// ── Types ─────────────────────────────────────────────────────────────────────

interface TestCase {
  input: string;
  expectedOutput: string;
}

interface ProblemFormData {
  title: string;
  difficulty: 'easy' | 'medium' | 'hard' | '';
  tags: string[];
  statement: string;
  constraints: string;
  testCases: TestCase[];
  isActive: boolean;
  timeLimitMs: number;
  memoryLimitMb: number;
}

const ALL_AVAILABLE_TAGS = [
  'Arrays', 'Hash Map', 'Graphs', 'BFS', 'DFS',
  'Dynamic Programming', 'Recursion', 'Greedy',
  'Sorting', 'Binary Search', 'Trees', 'Intervals',
  'Linked List', 'Stack', 'Two Pointers', 'String', 'Math',
];

// ── Component ─────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-admin-problem-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-problem-form.component.html',
  styleUrl:    './admin-problem-form.component.scss',
})
export class AdminProblemFormComponent implements OnInit {
  private readonly route    = inject(ActivatedRoute);
  private readonly router   = inject(Router);
  private readonly adminSvc = inject(AdminService);

  readonly availableTags = ALL_AVAILABLE_TAGS;

  // ── Mode ───────────────────────────────────────────────────────────────────
  readonly isEditMode   = signal(false);
  readonly problemId    = signal<string | null>(null);
  readonly pageTitle    = computed(() => this.isEditMode() ? 'Edit Problem' : 'Add Problem');
  readonly submitLabel  = computed(() => this.isEditMode() ? 'Save Changes' : 'Create Problem');

  // ── Loading state (edit mode only) ─────────────────────────────────────────
  readonly isLoadingProblem = signal(false);
  readonly loadError        = signal<string | null>(null);

  // ── Form state ─────────────────────────────────────────────────────────────
  readonly form = signal<ProblemFormData>({
    title:         '',
    difficulty:    '',
    tags:          [],
    statement:     '',
    constraints:   '',
    testCases:     [{ input: '', expectedOutput: '' }],
    isActive:      true,
    timeLimitMs:   2000,
    memoryLimitMb: 256,
  });

  // ── Submit state ───────────────────────────────────────────────────────────
  readonly touched       = signal(false);
  readonly isSubmitting  = signal(false);
  readonly submitSuccess = signal(false);
  readonly formError     = signal<string | null>(null);

  // ── Validation ─────────────────────────────────────────────────────────────
  readonly errors = computed(() => {
    if (!this.touched()) return {} as Record<string, string>;
    const f = this.form();
    const e: Record<string, string> = {};

    if (!f.title.trim())                     e['title']      = 'Title is required.';
    else if (f.title.trim().length < 3)      e['title']      = 'Title must be at least 3 characters.';
    if (!f.difficulty)                       e['difficulty'] = 'Difficulty is required.';
    if (f.tags.length === 0)                 e['tags']       = 'At least one tag is required.';
    if (!f.statement.trim())                 e['statement']  = 'Problem statement is required.';
    else if (f.statement.trim().length < 50) e['statement']  = 'Statement must be at least 50 characters.';
    if (f.testCases.length === 0)            e['testCases']  = 'At least one test case is required.';
    else if (f.testCases.some(tc => !tc.input.trim() || !tc.expectedOutput.trim()))
                                             e['testCases']  = 'All test cases must have input and expected output.';
    return e;
  });

  readonly isValid = computed(() =>
    Object.keys(this.errors()).length === 0 && this.touched()
  );

  // ── Init ───────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.problemId.set(id);
      this.loadProblem(id);
    }
  }

  private loadProblem(id: string): void {
    this.isLoadingProblem.set(true);
    this.loadError.set(null);

    this.adminSvc.getProblemById(id).subscribe({
      next: raw => {
        // Map backend detail shape → form data
        this.form.set({
          title:         raw.title ?? '',
          difficulty:    raw.difficulty ?? '',   // already mapped by AdminService
          tags:          raw.tags ?? [],
          statement:     raw.statement ?? raw.description ?? '',
          constraints:   Array.isArray(raw.constraints)
                           ? raw.constraints.join('\n')
                           : (raw.constraints ?? ''),
          testCases:     (raw.sampleTestCases ?? raw.examples ?? []).map((tc: any) => ({
            input:          tc.input ?? '',
            expectedOutput: tc.expectedOutput ?? tc.output ?? '',
          })),
          isActive:      raw.isActive ?? true,
          timeLimitMs:   raw.timeLimitMs ?? 2000,
          memoryLimitMb: raw.memoryLimitMb ?? 256,
        });
        this.isLoadingProblem.set(false);
      },
      error: err => {
        this.loadError.set(
          err.status === 404
            ? 'Problem not found.'
            : 'Failed to load problem. Make sure the backend is running.'
        );
        this.isLoadingProblem.set(false);
      },
    });
  }

  // ── Field helpers ──────────────────────────────────────────────────────────
  updateField<K extends keyof ProblemFormData>(key: K, value: ProblemFormData[K]): void {
    this.form.update(f => ({ ...f, [key]: value }));
  }

  // ── Tag management ─────────────────────────────────────────────────────────
  toggleTag(tag: string): void {
    this.form.update(f => {
      const tags = f.tags.includes(tag)
        ? f.tags.filter(t => t !== tag)
        : [...f.tags, tag];
      return { ...f, tags };
    });
  }

  hasTag(tag: string): boolean {
    return this.form().tags.includes(tag);
  }

  // ── Test cases ─────────────────────────────────────────────────────────────
  addTestCase(): void {
    this.form.update(f => ({
      ...f,
      testCases: [...f.testCases, { input: '', expectedOutput: '' }],
    }));
  }

  removeTestCase(index: number): void {
    this.form.update(f => ({
      ...f,
      testCases: f.testCases.filter((_, i) => i !== index),
    }));
  }

  updateTestCase(index: number, field: 'input' | 'expectedOutput', value: string): void {
    this.form.update(f => {
      const testCases = f.testCases.map((tc, i) =>
        i === index ? { ...tc, [field]: value } : tc
      );
      return { ...f, testCases };
    });
  }

  // ── Submit ─────────────────────────────────────────────────────────────────
  onSubmit(): void {
    this.touched.set(true);
    this.formError.set(null);
    if (!this.isValid()) return;

    const f = this.form();
    const body: CreateProblemBody = {
      title:           f.title.trim(),
      difficulty:      difficultyToNumber(f.difficulty as Difficulty),
      tags:            f.tags,
      statement:       f.statement.trim(),
      constraints:     f.constraints.trim(),
      sampleTestCases: f.testCases,
      isActive:        f.isActive,
      timeLimitMs:     f.timeLimitMs,
      memoryLimitMb:   f.memoryLimitMb,
    };

    this.isSubmitting.set(true);

    const call$ = this.isEditMode()
      ? this.adminSvc.updateProblem(this.problemId()!, body)
      : this.adminSvc.createProblem(body);

    call$.subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.submitSuccess.set(true);
        setTimeout(() => this.navigateToList(), 1200);
      },
      error: err => {
        this.isSubmitting.set(false);
        const code = err?.error?.errorCode;
        if (code === 'CONFLICT') {
          this.formError.set('A problem with this title already exists.');
        } else if (err.status === 400) {
          this.formError.set(err.error?.message || 'Validation error. Check your fields.');
        } else {
          this.formError.set('Something went wrong. Please try again.');
        }
      },
    });
  }

  onCancel(): void {
    this.navigateToList();
  }

  private navigateToList(): void {
    // From /admin/problems/new or /admin/problems/:id/edit → go up to /admin/problems
    const segments = this.isEditMode()
      ? ['../../problems']   // from problems/:id/edit — go up 2 levels
      : ['../problems'];     // from problems/new — go up 1 level
    this.router.navigate(segments, { relativeTo: this.route });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────
  difficultyClass(d: string): string {
    if (d === 'easy')   return 'badge--easy';
    if (d === 'medium') return 'badge--medium';
    if (d === 'hard')   return 'badge--hard';
    return '';
  }

  trackByIndex(index: number): number { return index; }
}
