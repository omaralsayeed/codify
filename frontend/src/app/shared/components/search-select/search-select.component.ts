import {
  Component, Input, Output, EventEmitter,
  signal, computed, ChangeDetectionStrategy,
  OnDestroy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

/** Minimal shape every item must satisfy */
export interface SelectItem {
  id: string;
  label: string;
  /** Optional badge text (e.g. difficulty, initials) */
  badge?: string;
  /** Optional CSS class for the badge */
  badgeClass?: string;
}

const DEBOUNCE_MS = 200;
const MAX_SUGGESTIONS = 8;

/**
 * SearchSelectComponent
 *
 * Reusable search-and-select widget. Accepts a `searchFn` that maps a query
 * string to a list of SelectItems — this indirection means the caller can swap
 * in a real HTTP search later without changing this component.
 *
 * Usage:
 *   <app-search-select
 *     label="Problems"
 *     placeholder="Search problems…"
 *     [searchFn]="searchProblems"
 *     [selected]="selectedProblems()"
 *     (selectItem)="addProblem($event)"
 *     (deselectItem)="removeProblem($event)"
 *   />
 */
@Component({
  selector: 'app-search-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-select.component.html',
  styleUrl: './search-select.component.scss',
})
export class SearchSelectComponent implements OnDestroy {
  /** Label shown above the search input */
  @Input() label = '';
  /** Placeholder text for the search input */
  @Input() placeholder = 'Search…';
  /**
   * Search function — receives the current query string, returns matching items.
   * Cap results to MAX_SUGGESTIONS internally — the caller doesn't need to.
   * Returning an empty array hides the suggestions panel.
   */
  @Input() searchFn: (query: string) => SelectItem[] = () => [];
  /** Currently selected items (passed in from parent, source of truth lives there) */
  @Input() selected: SelectItem[] = [];

  @Output() selectItem   = new EventEmitter<SelectItem>();
  @Output() deselectItem = new EventEmitter<SelectItem>();

  // ── Internal state ────────────────────────────────────────────────────────

  readonly query = signal('');
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;

  /** Suggestions from searchFn, capped at MAX_SUGGESTIONS, excluding already-selected */
  readonly suggestions = computed<SelectItem[]>(() => {
    const q = this.query().trim();
    if (!q) return [];
    const selectedIds = new Set(this.selected.map(s => s.id));
    return this.searchFn(q)
      .filter(item => !selectedIds.has(item.id))
      .slice(0, MAX_SUGGESTIONS);
  });

  readonly hasQuery = computed(() => this.query().trim().length > 0);

  // ── Handlers ──────────────────────────────────────────────────────────────

  onQueryInput(value: string): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => {
      this.query.set(value);
    }, DEBOUNCE_MS);
  }

  pick(item: SelectItem): void {
    this.selectItem.emit(item);
    // Clear search after picking
    this.query.set('');
  }

  remove(item: SelectItem): void {
    this.deselectItem.emit(item);
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
  }
}
