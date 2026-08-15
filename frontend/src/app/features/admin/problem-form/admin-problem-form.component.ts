import {
  Component, ChangeDetectionStrategy, signal, computed, inject, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AdminProblemRow } from '../problems/admin-problems.component';

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

// ── Mock problem data for edit mode (same source as problems list) ─────────────

const MOCK_PROBLEMS: Record<string, AdminProblemRow & { statement: string; constraints: string; testCases: TestCase[] }> = {
  p01: {
    id: 'p01', title: 'Two Sum', difficulty: 'easy', tags: ['Arrays', 'Hash Map'],
    solvedCount: 36045, totalSubmissions: 48200, isActive: true, createdAt: '2026-04-01T10:00:00Z',
    statement: 'Given an array of integers `nums` and an integer `target`, return indices of the two numbers such that they add up to `target`.\n\nYou may assume that each input would have exactly one solution, and you may not use the same element twice.',
    constraints: '2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9\n-10^9 <= target <= 10^9\nOnly one valid answer exists.',
    testCases: [
      { input: 'nums = [2,7,11,15], target = 9', expectedOutput: '[0,1]' },
      { input: 'nums = [3,2,4], target = 6',     expectedOutput: '[1,2]' },
    ],
  },
  p06: {
    id: 'p06', title: 'Merge Intervals', difficulty: 'medium', tags: ['Sorting', 'Intervals'],
    solvedCount: 16884, totalSubmissions: 24000, isActive: true, createdAt: '2026-04-11T10:00:00Z',
    statement: 'Given an array of intervals where intervals[i] = [starti, endi], merge all overlapping intervals, and return an array of the non-overlapping intervals that cover all the intervals in the input.',
    constraints: '1 <= intervals.length <= 10^4\nintervals[i].length == 2\n0 <= starti <= endi <= 10^4',
    testCases: [
      { input: 'intervals = [[1,3],[2,6],[8,10],[15,18]]', expectedOutput: '[[1,6],[8,10],[15,18]]' },
    ],
  },
};

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
  private readonly route  = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly availableTags = ALL_AVAILABLE_TAGS;

  // ── Mode ───────────────────────────────────────────────────────────────────
  readonly isEditMode = signal(false);
  readonly problemId  = signal<string | null>(null);
  readonly pageTitle  = computed(() => this.isEditMode() ? 'Edit Problem' : 'Add Problem');
  readonly submitLabel = computed(() => this.isEditMode() ? 'Save Changes' : 'Create Problem');

  // ── Form state ─────────────────────────────────────────────────────────────
  readonly form = signal<ProblemFormData>({
    title:        '',
    difficulty:   '',
    tags:         [],
    statement:    '',
    constraints:  '',
    testCases:    [{ input: '', expectedOutput: '' }],
    isActive:     true,
    timeLimitMs:  2000,
    memoryLimitMb: 256,
  });

  // ── Validation ─────────────────────────────────────────────────────────────
  readonly touched      = signal(false);
  readonly isSubmitting = signal(false);
  readonly submitSuccess = signal(false);

  readonly errors = computed(() => {
    if (!this.touched()) return {} as Record<string, string>;
    const f = this.form();
    const e: Record<string, string> = {};

    if (!f.title.trim())                        e['title']      = 'Title is required.';
    else if (f.title.trim().length < 3)         e['title']      = 'Title must be at least 3 characters.';
    if (!f.difficulty)                          e['difficulty'] = 'Difficulty is required.';
    if (f.tags.length === 0)                    e['tags']       = 'At least one tag is required.';
    if (!f.statement.trim())                    e['statement']  = 'Problem statement is required.';
    else if (f.statement.trim().length < 50)    e['statement']  = 'Statement must be at least 50 characters.';
    if (f.testCases.length === 0)               e['testCases']  = 'At least one test case is required.';
    else if (f.testCases.some(tc => !tc.input.trim() || !tc.expectedOutput.trim()))
                                                e['testCases']  = 'All test cases must have input and expected output.';

    return e;
  });

  readonly isValid = computed(() => Object.keys(this.errors()).length === 0 && this.touched());

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
    const found = MOCK_PROBLEMS[id];
    if (!found) return;
    this.form.set({
      title:         found.title,
      difficulty:    found.difficulty,
      tags:          [...found.tags],
      statement:     found.statement,
      constraints:   found.constraints,
      testCases:     found.testCases.map(tc => ({ ...tc })),
      isActive:      found.isActive,
      timeLimitMs:   2000,
      memoryLimitMb: 256,
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
    if (!this.isValid()) return;

    this.isSubmitting.set(true);

    // Simulate API call — replace with real HTTP POST/PATCH when backend ready
    setTimeout(() => {
      this.isSubmitting.set(false);
      this.submitSuccess.set(true);
      // Navigate back to problems list after short delay
      setTimeout(() => this.router.navigate(['../../../problems'], { relativeTo: this.route }), 1200);
    }, 800);
  }

  onCancel(): void {
    this.router.navigate(['../../../problems'], { relativeTo: this.route });
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
