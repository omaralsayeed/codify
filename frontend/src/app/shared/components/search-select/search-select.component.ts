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

const DEBOUNCE_MS = 150;
const MAX_SUGGESTIONS = 10;

@Component({
  selector: 'app-search-select',
  standalone: true,
  imports: [CommonModule, FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './search-select.component.html',
  styleUrl: './search-select.component.scss',
})
export class SearchSelectComponent implements OnDestroy {
  @Input() label = '';
  @Input() placeholder = 'Search or click to select…';
  @Input() searchFn: (query: string) => SelectItem[] = () => [];
  @Input() selected: SelectItem[] = [];

  @Output() selectItem   = new EventEmitter<SelectItem>();
  @Output() deselectItem = new EventEmitter<SelectItem>();

  readonly query = signal('');
  readonly isOpen = signal(false);
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private blurTimer: ReturnType<typeof setTimeout> | null = null;

  readonly suggestions = computed<SelectItem[]>(() => {
    const q = this.query().trim();
    const selectedIds = new Set(this.selected.map(s => s.id));
    return this.searchFn(q)
      .filter(item => !selectedIds.has(item.id))
      .slice(0, MAX_SUGGESTIONS);
  });

  readonly hasQuery = computed(() => this.query().trim().length > 0);

  onFocus(): void {
    if (this.blurTimer) clearTimeout(this.blurTimer);
    this.isOpen.set(true);
  }

  onBlur(): void {
    this.blurTimer = setTimeout(() => {
      this.isOpen.set(false);
    }, 250);
  }

  onQueryInput(value: string): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.isOpen.set(true);
    this.debounceTimer = setTimeout(() => {
      this.query.set(value);
    }, DEBOUNCE_MS);
  }

  pick(item: SelectItem): void {
    this.selectItem.emit(item);
    this.query.set('');
    this.isOpen.set(false);
  }

  remove(item: SelectItem): void {
    this.deselectItem.emit(item);
  }

  ngOnDestroy(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    if (this.blurTimer) clearTimeout(this.blurTimer);
  }
}
