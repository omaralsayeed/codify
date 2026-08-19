import {
  Component, ChangeDetectionStrategy, OnInit, inject,
  signal, computed, effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  AdminService, AdminProblemRow,
} from '../../../core/services/admin.service';
import { difficultyToNumber, Difficulty } from '../../../core/utils/enum-mappers';

// Re-export so problem-form can import the type
export type { AdminProblemRow };

type DifficultyFilter = 'all' | 'easy' | 'medium' | 'hard';
type StatusFilter     = 'all' | 'active' | 'inactive';
type SortField        = 'title' | 'difficulty' | 'solvedCount' | 'createdAt';
type SortDir          = 'asc' | 'desc';

const ALL_TAGS = [
  'Arrays', 'Hash Map', 'Graphs', 'BFS', 'DFS',
  'Dynamic Programming', 'Recursion', 'Greedy',
  'Sorting', 'Binary Search', 'Trees', 'Intervals',
  'Linked List', 'Stack', 'Two Pointers',
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
  private readonly adminSvc = inject(AdminService);

  readonly allTags = ALL_TAGS;

  // ── Filter & sort state ────────────────────────────────────────────────────
  readonly searchQuery      = signal('');
  readonly difficultyFilter = signal<DifficultyFilter>('all');
  readonly tagFilter        = signal('all');
  readonly statusFilter     = signal<StatusFilter>('all');
  readonly sortField        = signal<SortField>('createdAt');
  readonly sortDir          = signal<SortDir>('desc');

  // ── Data state ─────────────────────────────────────────────────────────────
  readonly isLoading    = signal(true);
  readonly error        = signal<string | null>(null);
  readonly allProblems  = signal<AdminProblemRow[]>([]);
  readonly serverTotal  = signal(0);

  // ── Modal state ────────────────────────────────────────────────────────────
  readonly confirmProblem    = signal<AdminProblemRow | null>(null);
  readonly confirmToggleType = signal<'activate' | 'deactivate' | null>(null);
  readonly isToggling        = signal(false);
  readonly toggleError       = signal<string | null>(null);

  // ── Delete confirm ─────────────────────────────────────────────────────────
  readonly confirmDeleteProblem = signal<AdminProblemRow | null>(null);
  readonly isDeleting           = signal(false);
  readonly deleteError          = signal<string | null>(null);

  // ── Summary counts (from full loaded list) ─────────────────────────────────
  readonly totalCount    = computed(() => this.serverTotal());
  readonly activeCount   = computed(() => this.allProblems().filter(p => p.isActive).length);
  readonly inactiveCount = computed(() => this.allProblems().filter(p => !p.isActive).length);
  readonly easyCount     = computed(() => this.allProblems().filter(p => p.difficulty === 'easy').length);
  readonly mediumCount   = computed(() => this.allProblems().filter(p => p.difficulty === 'medium').length);
  readonly hardCount     = computed(() => this.allProblems().filter(p => p.difficulty === 'hard').length);

  // ── Client-side filter on loaded data ─────────────────────────────────────
  readonly filteredProblems = computed(() => {
    const q    = this.searchQuery().toLowerCase().trim();
    const diff = this.difficultyFilter();
    const tag  = this.tagFilter();
    const st   = this.statusFilter();
    const field = this.sortField();
    const dir   = this.sortDir();

    let list = this.allProblems();

    if (q)          list = list.filter(p => p.title.toLowerCase().includes(q));
    if (diff !== 'all') list = list.filter(p => p.difficulty === diff);
    if (tag  !== 'all') list = list.filter(p => p.tags.includes(tag));
    if (st   !== 'all') list = list.filter(p =>
      st === 'active' ? p.isActive : !p.isActive
    );

    return [...list].sort((a, b) => {
      let cmp = 0;
      if (field === 'title')       cmp = a.title.localeCompare(b.title);
      else if (field === 'difficulty') {
        const order = { easy: 0, medium: 1, hard: 2 };
        cmp = order[a.difficulty] - order[b.difficulty];
      }
      else if (field === 'solvedCount') cmp = a.solvedCount - b.solvedCount;
      else if (field === 'createdAt')   cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      return dir === 'asc' ? cmp : -cmp;
    });
  });

  constructor() {
    // Re-fetch from backend when sort changes
    effect(() => {
      this.sortField();
      this.sortDir();
      this.loadProblems();
    });
  }

  ngOnInit(): void {
    // Initial load handled by effect
  }

  // ── HTTP load ──────────────────────────────────────────────────────────────
  loadProblems(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.adminSvc.getProblems({
      sortBy:   this.sortField(),
      sortDir:  this.sortDir(),
      pageSize: 100,
    }).subscribe({
      next: ({ problems, total }) => {
        this.allProblems.set(problems);
        this.serverTotal.set(total);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load problems. Make sure the backend is running.');
        this.isLoading.set(false);
      },
    });
  }

  // ── Sort ───────────────────────────────────────────────────────────────────
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

  // ── Badge helpers ──────────────────────────────────────────────────────────
  difficultyClass(d: string): string {
    if (d === 'easy')   return 'badge--easy';
    if (d === 'medium') return 'badge--medium';
    return 'badge--hard';
  }

  acceptRate(p: AdminProblemRow): string {
    if (!p.totalSubmissions) return '—';
    return Math.round((p.solvedCount / p.totalSubmissions) * 100) + '%';
  }

  // ── Toggle status ──────────────────────────────────────────────────────────
  requestToggle(p: AdminProblemRow, event: Event): void {
    event.stopPropagation();
    this.toggleError.set(null);
    this.confirmProblem.set(p);
    this.confirmToggleType.set(p.isActive ? 'deactivate' : 'activate');
  }

  confirmToggle(): void {
    const p = this.confirmProblem();
    if (!p) return;

    this.isToggling.set(true);
    this.adminSvc.updateProblemStatus(p.id, !p.isActive).subscribe({
      next: ({ isActive }) => {
        // Patch locally — no full re-fetch needed for a single field change
        this.allProblems.update(list =>
          list.map(x => x.id === p.id ? { ...x, isActive } : x)
        );
        this.isToggling.set(false);
        this.closeModal();
      },
      error: () => {
        this.isToggling.set(false);
        this.toggleError.set('Failed to update problem status. Please try again.');
      },
    });
  }

  // ── Delete ─────────────────────────────────────────────────────────────────
  requestDelete(p: AdminProblemRow, event: Event): void {
    event.stopPropagation();
    this.deleteError.set(null);
    this.confirmDeleteProblem.set(p);
  }

  confirmDelete(): void {
    const p = this.confirmDeleteProblem();
    if (!p) return;

    this.isDeleting.set(true);
    this.adminSvc.deleteProblem(p.id).subscribe({
      next: () => {
        this.allProblems.update(list => list.filter(x => x.id !== p.id));
        this.serverTotal.update(t => t - 1);
        this.isDeleting.set(false);
        this.closeDeleteModal();
      },
      error: () => {
        this.isDeleting.set(false);
        this.deleteError.set('Failed to delete problem. Please try again.');
      },
    });
  }

  closeModal(): void {
    this.confirmProblem.set(null);
    this.confirmToggleType.set(null);
    this.toggleError.set(null);
  }

  closeDeleteModal(): void {
    this.confirmDeleteProblem.set(null);
    this.deleteError.set(null);
  }

  // ── Filters ────────────────────────────────────────────────────────────────
  clearFilters(): void {
    this.searchQuery.set('');
    this.difficultyFilter.set('all');
    this.tagFilter.set('all');
    this.statusFilter.set('all');
  }

  get hasActiveFilters(): boolean {
    return this.searchQuery() !== '' ||
           this.difficultyFilter() !== 'all' ||
           this.tagFilter() !== 'all' ||
           this.statusFilter() !== 'all';
  }

  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }
}
