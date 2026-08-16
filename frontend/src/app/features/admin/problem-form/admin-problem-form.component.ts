import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ProblemService } from '../../../core/services/problem.service';

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

@Component({
  selector: 'app-admin-problem-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-problem-form.component.html',
  styleUrl:    './admin-problem-form.component.scss',
})
export class AdminProblemFormComponent implements OnInit {
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly problemSvc = inject(ProblemService);

  readonly allAvailableTags = ALL_AVAILABLE_TAGS;
  readonly availableTags = ALL_AVAILABLE_TAGS;
  readonly availableDifficulties: ('easy' | 'medium' | 'hard')[] = ['easy', 'medium', 'hard'];

  readonly isEditMode = signal(false);
  readonly problemId  = signal<string | null>(null);

  readonly form = signal<ProblemFormData>({
    title:         '',
    difficulty:    '',
    tags:          [],
    statement:     '',
    constraints:   '',
    testCases:     [
      { input: '', expectedOutput: '' },
      { input: '', expectedOutput: '' },
    ],
    isActive:      true,
    timeLimitMs:   2000,
    memoryLimitMb: 256,
  });

  readonly isSubmitting = signal(false);
  readonly submitSuccess = signal(false);
  readonly submitError  = signal<string | null>(null);
  readonly touched      = signal(false);
  readonly pageTitle    = computed(() => this.isEditMode() ? 'Edit Problem' : 'New Problem');

  // ── Validation ─────────────────────────────────────────────────────────────
  readonly isValid = computed(() => {
    const f = this.form();
    return (
      f.title.trim().length > 0 &&
      f.difficulty !== '' &&
      f.tags.length > 0 &&
      f.statement.trim().length > 0 &&
      f.constraints.trim().length > 0 &&
      f.testCases.length > 0 &&
      f.testCases.every(tc => tc.input.trim().length > 0 && tc.expectedOutput.trim().length > 0)
    );
  });

  readonly submitLabel = computed(() => {
    if (this.isSubmitting()) return 'Saving...';
    return this.isEditMode() ? 'Update Problem' : 'Create Problem';
  });

  readonly errors = computed<Record<string, string>>(() => {
    if (!this.touched()) return {};
    const f = this.form();
    const errs: Record<string, string> = {};
    if (!f.title.trim()) errs['title'] = 'Title is required';
    if (!f.difficulty) errs['difficulty'] = 'Difficulty is required';
    if (f.tags.length === 0) errs['tags'] = 'At least one tag is required';
    if (!f.statement.trim()) errs['statement'] = 'Statement is required';
    if (!f.constraints.trim()) errs['constraints'] = 'Constraints are required';
    return errs;
  });

  trackByIndex(index: number): number {
    return index;
  }

  onCancel(): void {
    this.router.navigate(['/admin/problems']);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEditMode.set(true);
      this.problemId.set(id);
      this.loadProblem(id);
    }
  }

  private loadProblem(id: string): void {
    this.problemSvc.getById(id).subscribe({
      next: (found: any) => {
        if (!found) return;
        this.form.set({
          title:         found.title,
          difficulty:    found.difficulty,
          tags:          found.tags || [found.topic],
          statement:     found.statement || found.description || '',
          constraints:   Array.isArray(found.constraints) ? found.constraints.join('\n') : (found.constraints || ''),
          testCases:     (found.sampleTestCases || found.examples || []).map((tc: any) => ({
            input: tc.input,
            expectedOutput: tc.expectedOutput || tc.output || ''
          })),
          isActive:      found.isActive ?? true,
          timeLimitMs:   found.timeLimitMs || 2000,
          memoryLimitMb: found.memoryLimitMb || 256,
        });
      },
      error: () => {
        this.submitError.set('Could not load problem from database.');
      }
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

  // ── Submission ─────────────────────────────────────────────────────────────
  submit(): void {
    this.touched.set(true);
    if (!this.isValid()) return;

    this.isSubmitting.set(true);
    this.submitError.set(null);

    const f = this.form();
    const payload = {
      title: f.title,
      difficulty: f.difficulty === 'easy' ? 0 : f.difficulty === 'medium' ? 1 : 2,
      statement: f.statement,
      constraints: f.constraints,
      timeLimitMs: f.timeLimitMs,
      memoryLimitMb: f.memoryLimitMb,
      tags: f.tags,
      isPublic: true,
    };

    if (this.isEditMode() && this.problemId()) {
      this.problemSvc.update(this.problemId()!, payload as any).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.router.navigate(['/admin/problems']);
        },
        error: (err: any) => {
          this.isSubmitting.set(false);
          this.submitError.set(err?.message ?? 'Failed to update problem.');
        }
      });
    } else {
      this.problemSvc.create(payload as any).subscribe({
        next: () => {
          this.isSubmitting.set(false);
          this.router.navigate(['/admin/problems']);
        },
        error: (err: any) => {
          this.isSubmitting.set(false);
          this.submitError.set(err?.message ?? 'Failed to create problem.');
        }
      });
    }
  }

  onSubmit(): void {
    this.submit();
  }

  difficultyClass(d: string): string {
    if (d === 'easy') return 'badge--easy';
    if (d === 'medium') return 'badge--medium';
    return 'badge--hard';
  }
}
