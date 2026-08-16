import { Component, ChangeDetectionStrategy, signal, computed, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProblemService } from '../../../core/services/problem.service';

// ── Types ─────────────────────────────────────────────────────────────────────

export interface AdminProblemRow {
  id: string;
  title: string;
  difficulty: 'easy' | 'medium' | 'hard';
  tags: string[];
  solvedCount: number;
  totalSubmissions: number;
  isActive: boolean;
  createdAt: string;
}

type DifficultyFilter = 'all' | 'easy' | 'medium' | 'hard';
type StatusFilter     = 'all' | 'active' | 'inactive';
type SortField        = 'title' | 'difficulty' | 'solvedCount' | 'createdAt';
type SortDir          = 'asc' | 'desc';

const ALL_TAGS = [
  'Arrays', 'Hash Map', 'Graphs', 'BFS', 'DFS',
  'Dynamic Programming', 'Recursion', 'Greedy',
  'Sorting', 'Binary Search', 'Trees', 'Intervals',
  'Linked List', 'Stack', 'Two Pointers', 'String', 'Math',
];

@Component({
  selector: 'app-admin-problems',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-problems.component.html',
  styleUrl:    './admin-problems.component.scss',
})
export class AdminProblemsComponent implements OnInit {
  private readonly problemSvc = inject(ProblemService);

  readonly allTags = ALL_TAGS;

  // ── Filters & sort ─────────────────────────────────────────────────────────
  readonly searchQuery       = signal('');
  readonly difficultyFilter  = signal<DifficultyFilter>('all');
  readonly tagFilter         = signal('all');
  readonly statusFilter      = signal<StatusFilter>('all');
  readonly sortField         = signal<SortField>('createdAt');
  readonly sortDir           = signal<SortDir>('desc');

  // confirm deactivate/activate modal
  readonly confirmProblem    = signal<AdminProblemRow | null>(null);
  readonly confirmToggleType = signal<'activate' | 'deactivate' | null>(null);

  private allProblems = signal<AdminProblemRow[]>([]);

  ngOnInit(): void {
    this.problemSvc.getAll({ pageSize: 100 }).subscribe({
      next: (problems) => {
        if (!problems) return;
        const liveRows: AdminProblemRow[] = problems.map(p => ({
          id: p.id,
          title: p.title,
          difficulty: p.difficulty,
          tags: p.topicLabel ? p.topicLabel.split(' · ') : [p.topic],
          solvedCount: p.solvedCount || 0,
          totalSubmissions: p.solvedCount || 0,
          isActive: true,
          createdAt: new Date().toISOString(),
        }));
        this.allProblems.set(liveRows);
      },
      error: () => {
        this.allProblems.set([]);
      }
    });
  }

  // ── Derived list ───────────────────────────────────────────────────────────
  readonly filteredProblems = computed(() => {
    const q          = this.searchQuery().toLowerCase().trim();
    const difficulty = this.difficultyFilter();
    const tag        = this.tagFilter();
    const status     = this.statusFilter();
    const field      = this.sortField();
    const dir        = this.sortDir();

    let list = this.allProblems();

    if (q)                  list = list.filter(p => p.title.toLowerCase().includes(q));
    if (difficulty !== 'all') list = list.filter(p => p.difficulty === difficulty);
    if (tag !== 'all')        list = list.filter(p => p.tags.includes(tag));
    if (status !== 'all')     list = list.filter(p =>
      status === 'active' ? p.isActive : !p.isActive
    );

    return [...list].sort((a, b) => {
      let cmp = 0;
      if (field === 'title')       cmp = a.title.localeCompare(b.title);
      else if (field === 'difficulty') {
        const order = { easy: 0, medium: 1, hard: 2 };
        cmp = order[a.difficulty] - order[b.difficulty];
      }
      else if (field === 'solvedCount')  cmp = a.solvedCount - b.solvedCount;
      else if (field === 'createdAt')    cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  readonly totalCount    = computed(() => this.allProblems().length);
  readonly activeCount   = computed(() => this.allProblems().filter(p => p.isActive).length);
  readonly inactiveCount = computed(() => this.allProblems().filter(p => !p.isActive).length);
  readonly easyCount     = computed(() => this.allProblems().filter(p => p.difficulty === 'easy').length);
  readonly mediumCount   = computed(() => this.allProblems().filter(p => p.difficulty === 'medium').length);
  readonly hardCount     = computed(() => this.allProblems().filter(p => p.difficulty === 'hard').length);

  readonly hasActiveFilters = computed(() =>
    this.searchQuery().trim() !== '' ||
    this.difficultyFilter() !== 'all' ||
    this.tagFilter() !== 'all' ||
    this.statusFilter() !== 'all'
  );

  clearFilters(): void {
    this.searchQuery.set('');
    this.difficultyFilter.set('all');
    this.tagFilter.set('all');
    this.statusFilter.set('all');
  }

  // ── Sort helpers ───────────────────────────────────────────────────────────
  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.sortDir.update(d => d === 'asc' ? 'desc' : 'asc');
    } else {
      this.sortField.set(field);
      this.sortDir.set('asc');
    }
  }

  sortIcon(field: SortField): string {
    if (this.sortField() !== field) return '↕';
    return this.sortDir() === 'asc' ? '↑' : '↓';
  }

  // ── Modal confirmation ─────────────────────────────────────────────────────
  openToggleConfirm(problem: AdminProblemRow): void {
    this.confirmProblem.set(problem);
    this.confirmToggleType.set(problem.isActive ? 'deactivate' : 'activate');
  }

  closeModal(): void {
    this.confirmProblem.set(null);
    this.confirmToggleType.set(null);
  }

  executeToggle(): void {
    const p = this.confirmProblem();
    if (!p) return;
    this.allProblems.update(list =>
      list.map(item => item.id === p.id ? { ...item, isActive: !item.isActive } : item)
    );
    this.closeModal();
  }

  // ── Template helpers ───────────────────────────────────────────────────────
  diffBadgeClass(d: string): string {
    if (d === 'easy')   return 'badge--easy';
    if (d === 'medium') return 'badge--medium';
    return 'badge--hard';
  }

  difficultyClass(d: string): string {
    return this.diffBadgeClass(d);
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }

  requestToggle(p: AdminProblemRow, event?: Event): void {
    if (event) event.stopPropagation();
    this.openToggleConfirm(p);
  }

  acceptRate(p: AdminProblemRow): string {
    if (!p.totalSubmissions || p.totalSubmissions === 0) return '0%';
    return `${Math.round((p.solvedCount / p.totalSubmissions) * 100)}%`;
  }
}
