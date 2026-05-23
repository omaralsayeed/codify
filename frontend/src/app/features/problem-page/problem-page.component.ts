import { Component, inject, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-problem-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './problem-page.component.html',
  styleUrl: './problem-page.component.scss'
})
export class ProblemPageComponent {
  protected readonly auth = inject(AuthService);

  isLeftPanelVisible: boolean = true;
  isRightPanelVisible: boolean = true;
  isEditorFullscreen: boolean = false;
  isBottomPanelOpen: boolean = true;
  activeTab: 'description' | 'editorial' | 'solutions' | 'submissions' = 'description';
  selectedLanguage: string = 'python';
  splitPosition: number = 50; // percentage for the draggable divider
  isSolved: boolean = false; // problem solved status
  isAutocompleteEnabled: boolean = true;
  editorCode: string = `def twoSum(nums, target):
    # Write your solution here
    pass`;
  cursorLine: number = 1;
  cursorColumn: number = 1;
  activeTestCase: number = 0; // index of active test case tab
  activeView: 'problem' | 'editor' = 'problem'; // mobile view switcher
  testCases = [
    { id: 1, label: 'Case 1', nums: '[2,7,11,15]', target: '9', expected: '[0,1]' },
    { id: 2, label: 'Case 2', nums: '[3,2,4]', target: '6', expected: '[1,2]' },
    { id: 3, label: 'Case 3', nums: '[3,3]', target: '6', expected: '[0,1]' }
  ];

  private isDragging: boolean = false;
  private readonly MIN_PANEL_WIDTH = 20; // minimum 20%
  private readonly MAX_PANEL_WIDTH = 80; // maximum 80%

  // ── Toolbar action stubs ──────────────────────────────────────────────────
  onRun(): void {}
  onSubmit(): void {}
  onCopy(): void {}
  onAiHint(): void {}
  onSettings(): void {}

  // ── Tab switching ─────────────────────────────────────────────────────────
  setActiveTab(tab: 'description' | 'editorial' | 'solutions' | 'submissions'): void {
    this.activeTab = tab;
  }

  // ── Problem actions ───────────────────────────────────────────────────────
  toggleSolved(): void {
    this.isSolved = !this.isSolved;
  }

  onTopicsClick(): void {}
  onCompaniesClick(): void {}
  onHintClick(): void {
    this.onAiHint(); // delegate to toolbar AI Hint handler
  }

  // ── Editor actions ────────────────────────────────────────────────────────
  onLanguageChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedLanguage = target.value;
    // TODO: Update editor template based on language
  }

  toggleAutocomplete(): void {
    this.isAutocompleteEnabled = !this.isAutocompleteEnabled;
  }

  toggleFullscreen(): void {
    this.isEditorFullscreen = !this.isEditorFullscreen;
  }

  onEditorInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.editorCode = target.value;
    this.updateCursorPosition(target);
  }

  private updateCursorPosition(textarea: HTMLTextAreaElement): void {
    const text = textarea.value.substring(0, textarea.selectionStart);
    const lines = text.split('\n');
    this.cursorLine = lines.length;
    this.cursorColumn = lines[lines.length - 1].length + 1;
  }

  // ── Bottom panel actions ──────────────────────────────────────────────────
  toggleBottomPanel(): void {
    this.isBottomPanelOpen = !this.isBottomPanelOpen;
  }

  setActiveTestCase(index: number): void {
    this.activeTestCase = index;
  }

  addTestCase(): void {
    const newId = this.testCases.length + 1;
    this.testCases.push({
      id: newId,
      label: `Case ${newId}`,
      nums: '[]',
      target: '0',
      expected: '[]'
    });
    this.activeTestCase = this.testCases.length - 1;
  }

  get currentTestCase() {
    return this.testCases[this.activeTestCase];
  }

  // ── Mobile view switching ─────────────────────────────────────────────────
  setActiveView(view: 'problem' | 'editor'): void {
    this.activeView = view;
  }

  // ── Panel visibility toggles ──────────────────────────────────────────────
  toggleLeftPanel(): void {
    this.isLeftPanelVisible = !this.isLeftPanelVisible;
  }

  toggleRightPanel(): void {
    this.isRightPanelVisible = !this.isRightPanelVisible;
  }

  // ── Resizer drag logic ────────────────────────────────────────────────────
  onResizerMouseDown(event: MouseEvent): void {
    event.preventDefault();
    this.isDragging = true;
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent): void {
    if (!this.isDragging) return;

    const container = document.querySelector('.split-container') as HTMLElement;
    if (!container) return;

    const containerRect = container.getBoundingClientRect();
    const offsetX = event.clientX - containerRect.left;
    const newSplitPosition = (offsetX / containerRect.width) * 100;

    // Clamp between min and max
    this.splitPosition = Math.max(
      this.MIN_PANEL_WIDTH,
      Math.min(this.MAX_PANEL_WIDTH, newSplitPosition)
    );
  }

  @HostListener('document:mouseup')
  onMouseUp(): void {
    if (this.isDragging) {
      this.isDragging = false;
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    }
  }
}
