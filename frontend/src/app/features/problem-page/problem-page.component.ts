import { Component, OnInit, inject, HostListener, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SubmissionService } from '../../core/services/submission.service';
import { HintService } from '../../core/services/hint.service';
import {
  RunCodeResponse,
  SubmissionDetailResponse,
  ServiceError,
} from '../../core/models/submission.model';
import { HintResponse } from '../../core/models/hint.model';

// ── Types ─────────────────────────────────────────────────────────────────────

type ActiveTab    = 'description' | 'editorial' | 'solutions' | 'submissions' | 'codify';
type SubmitPhase  = 'idle' | 'submitting' | 'done' | 'error';
type RunPhase     = 'idle' | 'running'    | 'done' | 'error';

@Component({
  selector: 'app-problem-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './problem-page.component.html',
  styleUrl: './problem-page.component.scss',
})
export class ProblemPageComponent implements OnInit {
  protected readonly auth          = inject(AuthService);
  private  readonly submissionSvc  = inject(SubmissionService);
  private  readonly hintSvc        = inject(HintService);

  // ── Language configuration ────────────────────────────────────────────────
  languages = [
    {
      name: 'Python', value: 'python',
      starterCode: `def twoSum(nums: list[int], target: int) -> list[int]:
    # Write your solution here
    pass`,
    },
    {
      name: 'C#', value: 'csharp',
      starterCode: `public class Solution {
    public int[] TwoSum(int[] nums, int target) {
        // Write your solution here
        return new int[] {};
    }
}`,
    },
    {
      name: 'JavaScript', value: 'javascript',
      starterCode: `/**
 * @param {number[]} nums
 * @param {number} target
 * @return {number[]}
 */
var twoSum = function(nums, target) {
    // Write your solution here
};`,
    },
    {
      name: 'Java', value: 'java',
      starterCode: `class Solution {
    public int[] twoSum(int[] nums, int target) {
        // Write your solution here
        return new int[]{};
    }
}`,
    },
    {
      name: 'C++', value: 'cpp',
      starterCode: `class Solution {
public:
    vector<int> twoSum(vector<int>& nums, int target) {
        // Write your solution here
        return {};
    }
};`,
    },
  ];

  // ── Panel / layout state ──────────────────────────────────────────────────
  isLeftPanelVisible   = true;
  isRightPanelVisible  = true;
  isEditorFullscreen   = false;
  isBottomPanelOpen    = true;
  activeTab: ActiveTab = 'description';
  selectedLanguage     = 'python';
  splitPosition        = 50;
  isSolved             = false;
  isAutocompleteEnabled = true;
  currentCode          = '';
  cursorLine           = 1;
  cursorColumn         = 1;
  activeTestCase       = 0;
  activeView: 'problem' | 'editor' = 'problem';

  // ── Test cases ────────────────────────────────────────────────────────────
  testCases = [
    { id: 1, label: 'Case 1', nums: '[2,7,11,15]', target: '9',  expected: '[0,1]' },
    { id: 2, label: 'Case 2', nums: '[3,2,4]',     target: '6',  expected: '[1,2]' },
    { id: 3, label: 'Case 3', nums: '[3,3]',        target: '6',  expected: '[0,1]' },
  ];

  // ── Run state ─────────────────────────────────────────────────────────────
  runPhase: RunPhase = 'idle';
  runResult: RunCodeResponse | null = null;
  runError:  ServiceError    | null = null;

  get currentActualOutput(): string {
    if (!this.runResult?.testResults?.length) return '';
    return this.runResult.testResults[this.activeTestCase]?.actualOutput ?? '';
  }

  get currentConsoleOutput(): string {
    if (!this.runResult) return '';
    return this.runResult.stderr || this.runResult.stdout || '';
  }

  // ── Submit state ──────────────────────────────────────────────────────────
  submitPhase: SubmitPhase = 'idle';
  submitResult: SubmissionDetailResponse | null = null;
  submitError:  ServiceError             | null = null;
  activeBottomTab: 'testcases' | 'result' = 'testcases';

  get showResultTab():  boolean { return this.submitPhase !== 'idle' || this.runPhase !== 'idle'; }
  get isAccepted():     boolean { return this.submitResult?.status === 'Accepted'; }
  get verdictLabel():   string  { return this.submitResult?.status ?? ''; }

  get passRatio(): string {
    if (!this.submitResult) return '';
    return `${this.submitResult.passedTestCases} / ${this.submitResult.totalTestCases}`;
  }

  get executionTime(): string {
    return this.submitResult?.executionTimeMs != null
      ? `${this.submitResult.executionTimeMs} ms` : '—';
  }

  get memoryUsed(): string {
    return this.submitResult?.memoryUsedKb != null
      ? `${(this.submitResult.memoryUsedKb / 1024).toFixed(1)} MB` : '—';
  }

  // ── AI Hint state ─────────────────────────────────────────────────────────
  hintHistory   = signal<HintResponse[]>([]);
  nextHintLevel = signal<number>(1);
  isLoadingHint = signal<boolean>(false);
  hintError     = signal<ServiceError | null>(null);

  private get previousHintTexts(): string[] {
    return this.hintHistory().map(h => h.hintText);
  }

  get hintButtonDisabled(): boolean {
    return this.isLoadingHint() || this.nextHintLevel() > 3;
  }

  get hintButtonLabel(): string {
    if (this.isLoadingHint()) return 'Loading…';
    if (this.nextHintLevel() > 3) return 'No more hints';
    return this.nextHintLevel() === 1 ? 'AI Hint' : `Hint ${this.nextHintLevel()}/3`;
  }

  // ── Private drag state ────────────────────────────────────────────────────
  private isDragging = false;
  private readonly MIN_PANEL_WIDTH = 20;
  private readonly MAX_PANEL_WIDTH = 80;
  private originalStarterCode = '';

  constructor() {
    const python = this.languages.find(l => l.value === 'python')!;
    this.currentCode         = python.starterCode;
    this.originalStarterCode = python.starterCode;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Services verified — both SubmissionService and HintService return data correctly.
  }

  // ── Toolbar: Run ──────────────────────────────────────────────────────────

  onRun(): void {
    this.runPhase  = 'running';
    this.runResult = null;
    this.runError  = null;
    if (!this.isBottomPanelOpen) this.isBottomPanelOpen = true;
    this.activeBottomTab = 'testcases';

    // TODO: derive problemId from ActivatedRoute once multi-problem support lands
    this.submissionSvc.run('00000000-0000-0000-0000-000000000005', this.currentCode, this.selectedLanguage)
      .subscribe({
        next:  result => { this.runResult = result; this.runPhase = 'done'; },
        error: (err: ServiceError) => { this.runError = err; this.runPhase = 'error'; },
      });
  }

  // ── Toolbar: Submit ───────────────────────────────────────────────────────

  onSubmit(): void {
    this.submitPhase  = 'submitting';
    this.submitResult = null;
    this.submitError  = null;
    if (!this.isBottomPanelOpen) this.isBottomPanelOpen = true;
    this.activeBottomTab = 'result';

    this.submissionSvc.submit('00000000-0000-0000-0000-000000000005', this.currentCode, this.selectedLanguage)
      .subscribe({
        next: result => {
          this.submitResult = result;
          this.submitPhase  = 'done';
          if (result.status === 'Accepted') this.isSolved = true;
          // Show submission history
          this.setActiveTab('submissions');
        },
        error: (err: ServiceError) => {
          this.submitError = err;
          this.submitPhase = 'error';
        },
      });
  }

  // ── Toolbar: Copy ─────────────────────────────────────────────────────────

  onCopy(): void {
    navigator.clipboard.writeText(this.currentCode).catch(() => {/* clipboard unavailable */});
  }

  // ── Toolbar: AI Hint ──────────────────────────────────────────────────────

  onAiHint(): void {
    const level = this.nextHintLevel();
    if (level > 3 || this.isLoadingHint()) return;

    this.isLoadingHint.set(true);
    this.hintError.set(null);

    this.hintSvc.getHint({
      problemId:            '00000000-0000-0000-0000-000000000005',
      studentCode:          this.currentCode,
      hintLevel:            level as 1 | 2 | 3,
      previousHints:        this.previousHintTexts,
      lastSubmissionStatus: this.submitResult?.status ?? undefined,
    }).subscribe({
      next: hint => {
        this.hintHistory.update(h => [...h, hint]);
        this.nextHintLevel.update(l => hint.hasMoreHints ? Math.min(l + 1, 3) : 4);
        this.isLoadingHint.set(false);
        this.setActiveTab('codify');
      },
      error: (err: ServiceError) => {
        this.hintError.set(err);
        this.isLoadingHint.set(false);
      },
    });
  }

  onSettings(): void {}

  // ── Tab switching ─────────────────────────────────────────────────────────
  setActiveTab(tab: ActiveTab): void { this.activeTab = tab; }
  setBottomTab(tab: 'testcases' | 'result'): void { this.activeBottomTab = tab; }

  // ── Problem actions ───────────────────────────────────────────────────────
  toggleSolved():     void { this.isSolved = !this.isSolved; }
  onTopicsClick():    void {}
  onCompaniesClick(): void {}
  onHintClick():      void { this.onAiHint(); }

  // ── Editor actions ────────────────────────────────────────────────────────

  onLanguageChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    const newLanguage = target.value;
    const hasModifiedCode = this.currentCode !== this.originalStarterCode;

    if (hasModifiedCode && !confirm('Switching language will reset your code. Continue?')) {
      target.value = this.selectedLanguage;
      return;
    }

    this.selectedLanguage = newLanguage;
    const lang = this.languages.find(l => l.value === newLanguage);
    if (lang) {
      this.currentCode         = lang.starterCode;
      this.originalStarterCode = lang.starterCode;
    }
  }

  toggleAutocomplete(): void { this.isAutocompleteEnabled = !this.isAutocompleteEnabled; }
  toggleFullscreen():   void { this.isEditorFullscreen    = !this.isEditorFullscreen;    }

  onEditorInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.currentCode = target.value;
    this.updateCursorPosition(target);
  }

  private updateCursorPosition(textarea: HTMLTextAreaElement): void {
    const text  = textarea.value.substring(0, textarea.selectionStart);
    const lines = text.split('\n');
    this.cursorLine   = lines.length;
    this.cursorColumn = lines[lines.length - 1].length + 1;
  }

  // ── Bottom panel ──────────────────────────────────────────────────────────
  toggleBottomPanel():            void { this.isBottomPanelOpen = !this.isBottomPanelOpen; }
  setActiveTestCase(i: number):   void { this.activeTestCase = i; }

  addTestCase(): void {
    const newId = this.testCases.length + 1;
    this.testCases.push({ id: newId, label: `Case ${newId}`, nums: '[]', target: '0', expected: '[]' });
    this.activeTestCase = this.testCases.length - 1;
  }

  get currentTestCase() { return this.testCases[this.activeTestCase]; }

  // ── Mobile ────────────────────────────────────────────────────────────────
  setActiveView(view: 'problem' | 'editor'): void { this.activeView = view; }

  // ── Panel toggles ─────────────────────────────────────────────────────────
  toggleLeftPanel():  void { this.isLeftPanelVisible  = !this.isLeftPanelVisible;  }
  toggleRightPanel(): void { this.isRightPanelVisible = !this.isRightPanelVisible; }

  // ── Resizer drag ──────────────────────────────────────────────────────────

  onResizerMouseDown(event: MouseEvent): void {
    event.preventDefault();
    this.isDragging = true;
    document.body.style.cursor     = 'col-resize';
    document.body.style.userSelect = 'none';
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent): void {
    if (!this.isDragging) return;
    const container = document.querySelector('.split-container') as HTMLElement;
    if (!container) return;
    const rect = container.getBoundingClientRect();
    const pct  = ((event.clientX - rect.left) / rect.width) * 100;
    this.splitPosition = Math.max(this.MIN_PANEL_WIDTH, Math.min(this.MAX_PANEL_WIDTH, pct));
  }

  @HostListener('document:mouseup')
  onMouseUp(): void {
    if (this.isDragging) {
      this.isDragging = false;
      document.body.style.cursor     = '';
      document.body.style.userSelect = '';
    }
  }
}
