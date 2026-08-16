import { Component, ChangeDetectionStrategy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

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
  'Linked List', 'Stack', 'Two Pointers',
];

// ── Mock data ─────────────────────────────────────────────────────────────────

const MOCK_PROBLEMS: AdminProblemRow[] = [
  { id: 'p01', title: 'Two Sum',                  difficulty: 'easy',   tags: ['Arrays', 'Hash Map'],          solvedCount: 36045, totalSubmissions: 48200, isActive: true,  createdAt: '2026-04-01T10:00:00Z' },
  { id: 'p02', title: 'Climbing Stairs',           difficulty: 'easy',   tags: ['Recursion', 'Dynamic Programming'], solvedCount: 22104, totalSubmissions: 29000, isActive: true,  createdAt: '2026-04-03T10:00:00Z' },
  { id: 'p03', title: 'Binary Search',             difficulty: 'easy',   tags: ['Binary Search'],               solvedCount: 28791, totalSubmissions: 34500, isActive: true,  createdAt: '2026-04-05T10:00:00Z' },
  { id: 'p04', title: 'Reverse String',            difficulty: 'easy',   tags: ['Two Pointers', 'Arrays'],      solvedCount: 31200, totalSubmissions: 38000, isActive: true,  createdAt: '2026-04-07T10:00:00Z' },
  { id: 'p05', title: 'Valid Parentheses',         difficulty: 'easy',   tags: ['Stack'],                       solvedCount: 27400, totalSubmissions: 34000, isActive: true,  createdAt: '2026-04-09T10:00:00Z' },
  { id: 'p06', title: 'Merge Intervals',           difficulty: 'medium', tags: ['Sorting', 'Intervals'],        solvedCount: 16884, totalSubmissions: 24000, isActive: true,  createdAt: '2026-04-11T10:00:00Z' },
  { id: 'p07', title: 'Coin Change II',            difficulty: 'medium', tags: ['Dynamic Programming'],         solvedCount: 13210, totalSubmissions: 20000, isActive: true,  createdAt: '2026-04-13T10:00:00Z' },
  { id: 'p08', title: 'Maximum Subarray',          difficulty: 'medium', tags: ['Greedy'],                      solvedCount: 19503, totalSubmissions: 26000, isActive: true,  createdAt: '2026-04-15T10:00:00Z' },
  { id: 'p09', title: 'Course Schedule',           difficulty: 'medium', tags: ['Graphs', 'BFS'],               solvedCount: 11762, totalSubmissions: 19000, isActive: true,  createdAt: '2026-04-17T10:00:00Z' },
  { id: 'p10', title: 'Linked List Cycle',         difficulty: 'medium', tags: ['Linked List', 'Two Pointers'], solvedCount: 18900, totalSubmissions: 25000, isActive: true,  createdAt: '2026-04-19T10:00:00Z' },
  { id: 'p11', title: 'Number of Islands',         difficulty: 'hard',   tags: ['Graphs', 'BFS', 'DFS'],        solvedCount: 9210,  totalSubmissions: 17000, isActive: true,  createdAt: '2026-04-21T10:00:00Z' },
  { id: 'p12', title: 'Lowest Common Ancestor',    difficulty: 'hard',   tags: ['Trees', 'DFS'],                solvedCount: 8540,  totalSubmissions: 15000, isActive: true,  createdAt: '2026-04-23T10:00:00Z' },
  { id: 'p13', title: 'Word Break',                difficulty: 'hard',   tags: ['Dynamic Programming'],         solvedCount: 7800,  totalSubmissions: 14000, isActive: false, createdAt: '2026-04-25T10:00:00Z' },
  { id: 'p14', title: 'Trapping Rain Water',       difficulty: 'hard',   tags: ['Two Pointers', 'Stack'],       solvedCount: 6900,  totalSubmissions: 13000, isActive: false, createdAt: '2026-04-27T10:00:00Z' },
];

// ── Component ─────────────────────────────────────────────────────────────────

@Component({
  selector: 'app-admin-problems',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-problems.component.html',
  styleUrl:    './admin-problems.component.scss',
})
export class AdminProblemsComponent {

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

  private allProblems = signal<AdminProblemRow[]>(MOCK_PROBLEMS);

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

  // ── Summary counts ─────────────────────────────────────────────────────────
  readonly totalCount    = computed(() => this.allProblems().length);
  readonly activeCount   = computed(() => this.allProblems().filter(p => p.isActive).length);
  readonly inactiveCount = computed(() => this.allProblems().filter(p => !p.isActive).length);
  readonly easyCount     = computed(() => this.allProblems().filter(p => p.difficulty === 'easy').length);
  readonly mediumCount   = computed(() => this.allProblems().filter(p => p.difficulty === 'medium').length);
  readonly hardCount     = computed(() => this.allProblems().filter(p => p.difficulty === 'hard').length);

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

  // ── Difficulty badge ───────────────────────────────────────────────────────
  difficultyClass(d: string): string {
    if (d === 'easy')   return 'badge--easy';
    if (d === 'medium') return 'badge--medium';
    return 'badge--hard';
  }

  // ── Accept rate ────────────────────────────────────────────────────────────
  acceptRate(p: AdminProblemRow): string {
    if (!p.totalSubmissions) return '—';
    return Math.round((p.solvedCount / p.totalSubmissions) * 100) + '%';
  }

  // ── Toggle status with modal ───────────────────────────────────────────────
  requestToggle(p: AdminProblemRow, event: Event): void {
    event.stopPropagation();
    this.confirmProblem.set(p);
    this.confirmToggleType.set(p.isActive ? 'deactivate' : 'activate');
  }

  confirmToggle(): void {
    const p = this.confirmProblem();
    if (!p) return;
    this.allProblems.update(list =>
      list.map(x => x.id === p.id ? { ...x, isActive: !x.isActive } : x)
    );
    this.closeModal();
  }

  closeModal(): void {
    this.confirmProblem.set(null);
    this.confirmToggleType.set(null);
  }

  // ── Filters reset ──────────────────────────────────────────────────────────
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

  // ── Date ───────────────────────────────────────────────────────────────────
  formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString('en-GB', {
      day: 'numeric', month: 'short', year: 'numeric',
    });
  }
}
