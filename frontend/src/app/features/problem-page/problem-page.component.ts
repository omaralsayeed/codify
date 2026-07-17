import { Component, OnInit, inject, HostListener, signal, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { SubmissionService } from '../../core/services/submission.service';
import { HintService } from '../../core/services/hint.service';
import {
  RunCodeResponse,
  SubmissionDetailResponse,
  ServiceError,
} from '../../core/models/submission.model';
import {
  HintResponse,
  HintLevel,
} from '../../core/models/hint.model';

// ── Types ─────────────────────────────────────────────────────────────────────

type ActiveTab    = 'description' | 'editorial' | 'solutions' | 'submissions' | 'codify';
type SubmitPhase  = 'idle' | 'submitting' | 'done' | 'error';
type RunPhase     = 'idle' | 'running'    | 'done' | 'error';

// ── Chunk 6: Hint-specific interfaces ────────────────────────────────────────
// HintCodeChange / spec-contract HintResponse kept here for future codeChange
// support; the backend currently returns narrative hints (hintText) only.

/** A single line-range replacement — populated when the backend supports diffs */
export interface HintCodeChange {
  lineStart:   number;
  lineEnd:     number;
  newCode:     string;
  explanation: string;
}

/** Spec-contract shape — superset of backend HintResponse */
export interface HintApiResponse {
  hintLevel:   number;
  totalHints:  number;      // backend: hasMoreHints → derive totalHints as 3
  codeChanges: HintCodeChange[];  // empty until backend returns diffs
  canAskMore:  boolean;
}

/** One entry in the session hint log */
export interface HintHistoryItem {
  level:       number;
  explanation: string;  // the hintText displayed to the user
  appliedAt:   Date;
}

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
  private  readonly elRef          = inject(ElementRef);
  private  readonly router         = inject(Router);

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
  isSubmitting:    boolean = false;
  submitPhase:     SubmitPhase = 'idle';
  submitResult:    SubmissionDetailResponse | null = null;
  submissionError: string | null = null;   // human-readable error for the banner
  submitError:     ServiceError | null = null;  // typed error for internal use
  submissionId:    string | null = null;   // stored for Chunk 7 (feedback panel)
  activeBottomTab: 'testcases' | 'result' = 'testcases';

  // Flash state: 'accepted' | 'rejected' | null — drives the 200ms button flash
  submitFlash: 'accepted' | 'rejected' | null = null;

  // Layer 3: editor glow class toggled on accepted
  editorGlowing = false;

  get showResultBanner(): boolean {
    // Banner shows as soon as submit is triggered (even while judging)
    return this.submitPhase !== 'idle';
  }
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

  // ── AI Hint state (Chunks 2 + 6) ─────────────────────────────────────────
  // Plain fields — spec contract (Chunk 6)
  hintLevel:            number  = 0;     // 0 = no hints used yet; increments on each successful fetch
  totalHintsAvailable:  number  = 0;     // derived from backend hasMoreHints (max 3)
  isHintLoading:        boolean = false;
  canRequestMoreHints:  boolean = true;
  lastHintExplanation:  string  = '';    // hintText of the most recently fetched hint
  hintHistory:          HintHistoryItem[] = [];  // full session log

  // Signal wrappers — used by the existing Codify-tab template (Chunks 2–5)
  // Keep in sync with the plain fields above inside onHintRequested()
  readonly hintHistorySignal = signal<HintResponse[]>([]);
  readonly hintError         = signal<ServiceError | null>(null);

  // Derived getters consumed by the toolbar button (carry-forward from Chunk 2)
  get nextHintLevel(): number { return this.hintLevel + 1; }

  get hintButtonDisabled(): boolean {
    return this.isHintLoading || !this.canRequestMoreHints;
  }

  get hintButtonLabel(): string {
    if (this.isHintLoading) return 'Loading…';
    if (!this.canRequestMoreHints) return 'No more hints';
    return this.hintLevel === 0 ? 'AI Hint' : `Hint ${this.nextHintLevel}/3`;
  }

  private get previousHintTexts(): string[] {
    return this.hintHistory.map(h => h.explanation);
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
    if (this.isSubmitting) return;

    this.isSubmitting    = true;
    this.submitPhase     = 'submitting';
    this.submitResult    = null;
    this.submissionError = null;
    this.submitError     = null;
    this.submissionId    = null;
    this.submitFlash     = null;
    if (!this.isBottomPanelOpen) this.isBottomPanelOpen = true;
    this.activeBottomTab = 'result';

    this.submissionSvc.submit('00000000-0000-0000-0000-000000000005', this.currentCode, this.selectedLanguage)
      .subscribe({
        next: result => {
          this.submitResult    = result;
          this.submissionId    = result.submissionId;   // stored for Chunk 7
          this.isSubmitting    = false;
          this.submitPhase     = 'done';
          this.submissionError = null;

          // 200ms flash on the toolbar Submit button
          this.submitFlash = result.status === 'Accepted' ? 'accepted' : 'rejected';
          setTimeout(() => { this.submitFlash = null; }, 700);

          if (result.status === 'Accepted') {
            this.isSolved = true;
            this.triggerAcceptedCelebration();
          }
          this.setActiveTab('submissions');
        },
        error: (err: ServiceError) => {
          this.submitError     = err;
          this.submissionError = err.message ?? 'Submission failed. Please try again.';
          this.isSubmitting    = false;
          this.submitPhase     = 'error';
        },
      });
  }

  // ── Toolbar: Copy ─────────────────────────────────────────────────────────

  onCopy(): void {
    navigator.clipboard.writeText(this.currentCode).catch(() => {/* clipboard unavailable */});
  }

  // ── Toolbar: AI Hint (public alias used by toolbar button) ───────────────
  onAiHint(): void { this.onHintRequested(); }

  // ── Hint: core handler (Chunk 6) ─────────────────────────────────────────

  /**
   * Requests the next progressive hint from HintService.
   * Guards:  isHintLoading OR !canRequestMoreHints → early return.
   * Updates: all plain state fields + signal wrappers in sync.
   * Side effects: calls applyHintToCode(), navigates to Codify tab.
   */
  onHintRequested(): void {
    if (this.isHintLoading || !this.canRequestMoreHints) return;

    const requestLevel = (this.hintLevel + 1) as HintLevel;

    this.isHintLoading = true;
    this.hintError.set(null);

    this.hintSvc.getHint({
      problemId:             '00000000-0000-0000-0000-000000000005',
      studentCode:           this.currentCode,
      hintLevel:             requestLevel,
      previousHints:         this.previousHintTexts,
      attemptCount:          this.hintLevel,          // how many hints already used
      lastSubmissionStatus:  this.submitResult?.status ?? undefined,
    }).subscribe({
      next: (hint: HintResponse) => {
        // ── Update plain state (spec contract) ────────────────────────────
        this.hintLevel           = hint.hintLevel;
        this.totalHintsAvailable = 3;                // backend max; hasMoreHints drives canAskMore
        this.canRequestMoreHints = hint.hasMoreHints && hint.hintLevel < 3;
        this.lastHintExplanation = hint.hintText;

        const historyItem: HintHistoryItem = {
          level:       hint.hintLevel,
          explanation: hint.hintText,
          appliedAt:   new Date(),
        };
        this.hintHistory = [...this.hintHistory, historyItem];

        // ── Keep signal wrapper in sync (for Codify-tab template) ─────────
        this.hintHistorySignal.update(h => [...h, hint]);

        // ── Apply hint to code if diff data present ────────────────────────
        this.applyHintToCode(hint);

        this.isHintLoading = false;

        // Navigate to Codify tab to surface the hint
        this.setActiveTab('codify');

        console.log('[HintService ✓] level:', hint.hintLevel,
                    '| canAskMore:', this.canRequestMoreHints,
                    '| history length:', this.hintHistory.length);
      },
      error: (err: ServiceError) => {
        this.hintError.set(err);
        this.isHintLoading = false;
        console.error('[HintService ✗]', err);
      },
    });
  }

  /**
   * Applies code changes from a hint response into the editor.
   * The backend currently returns narrative text only (no codeChanges array).
   * This method is ready to accept diffs when the backend supports them.
   *
   * @param hint  The raw HintResponse from the service.
   */
  private applyHintToCode(hint: HintResponse): void {
    // No codeChanges in the current backend response — nothing to apply yet.
    // When the backend adds diff support, map HintResponse.codeChanges here:
    //
    //   if (!hint.codeChanges?.length) return;
    //   const lines = this.currentCode.split('\n');
    //   for (const change of hint.codeChanges) {
    //     lines.splice(change.lineStart - 1,
    //                  change.lineEnd - change.lineStart + 1,
    //                  ...change.newCode.split('\n'));
    //   }
    //   this.currentCode = lines.join('\n');
    //
    // For now, log what would have been applied.
    console.log('[applyHintToCode] hint level', hint.hintLevel,
                '— no code diff in response (narrative hint only)');
  }

  /**
   * Navigates to the Codify AI chat context for the current problem + hint state.
   *
   * There is no dedicated /codify/chat route in app.routes.ts yet.
   * Until that route is registered, this opens the Codify tab on the left panel
   * within the problem page and logs the params that will become query params.
   *
   * When the chat route is added, replace the body with:
   *   this.router.navigate(['/codify/chat'], { queryParams });
   * or for a new tab:
   *   window.open('/codify/chat?' + new URLSearchParams(queryParams).toString(), '_blank');
   */
  navigateToCodifyChat(): void {
    const queryParams = {
      problemId: '00000000-0000-0000-0000-000000000005',
      context:   'hint',
      hintLevel: this.hintLevel,
      code:      encodeURIComponent(this.currentCode),
    };

    console.log('[navigateToCodifyChat] params:', queryParams);

    // TODO: replace with router.navigate once /codify/chat route is registered
    // this.router.navigate(['/codify/chat'], { queryParams });
    this.setActiveTab('codify');
  }

  onSettings(): void {}

  // ── Tab switching ─────────────────────────────────────────────────────────
  setActiveTab(tab: ActiveTab): void { this.activeTab = tab; }
  setBottomTab(tab: 'testcases' | 'result'): void { this.activeBottomTab = tab; }

  // ── Problem actions ───────────────────────────────────────────────────────
  toggleSolved():     void { this.isSolved = !this.isSolved; }
  onTopicsClick():    void {}
  onCompaniesClick(): void {}
  onHintClick(): void { this.onHintRequested(); }

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

  // ── Accepted Celebration (Chunk 5) ────────────────────────────────────────

  private prefersReducedMotion(): boolean {
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  /**
   * Orchestrates all three celebration layers when status === 'Accepted'.
   * Layer 1 (banner pulse) is handled purely in CSS via the --accepted modifier.
   * Layers 2 & 3 are skipped if the user prefers reduced motion.
   */
  private triggerAcceptedCelebration(): void {
    if (this.prefersReducedMotion()) return;

    // Layer 2 — confetti burst from the Submit button
    const submitBtn = this.elRef.nativeElement.querySelector('.toolbar-btn.btn--primary') as HTMLElement | null;
    this.launchConfetti(submitBtn);

    // Layer 3 — editor glow
    this.triggerEditorGlow();
  }

  // ── Layer 2: Confetti ──────────────────────────────────────────────────────

  /** Project palette colours used for particles */
  private readonly CONFETTI_COLORS = [
    '#1D9E75', // $teal
    '#C8A951', // $gold
    '#2E86AB', // $blue
    '#1A2B4A', // $navy
    '#E1F5EE', // $teal-lt
    '#FBF4E3', // $gold-lt
    '#E6F4FB', // $blue-lt
  ];

  private launchConfetti(anchor: HTMLElement | null): void {
    const PARTICLE_COUNT = 30;
    const DURATION_MS    = 1500;

    // Determine burst origin: centre of the Submit button, or viewport centre fallback
    let originX = window.innerWidth  / 2;
    let originY = window.innerHeight / 2;

    if (anchor) {
      const rect = anchor.getBoundingClientRect();
      originX = rect.left + rect.width  / 2;
      originY = rect.top  + rect.height / 2;
    }

    // Container that holds all particles — appended to body, removed after animation
    const container = document.createElement('div');
    container.setAttribute('aria-hidden', 'true');
    container.style.cssText = `
      position: fixed;
      inset: 0;
      pointer-events: none;
      z-index: 9999;
      overflow: hidden;
    `;
    document.body.appendChild(container);

    for (let i = 0; i < PARTICLE_COUNT; i++) {
      const particle = document.createElement('div');

      // Random size: 6–10px
      const size  = 6 + Math.random() * 4;
      // Random spread angle across full 360°
      const angle = Math.random() * Math.PI * 2;
      // Random travel distance: 60–160px
      const dist  = 60 + Math.random() * 100;
      const dx    = Math.cos(angle) * dist;
      const dy    = Math.sin(angle) * dist - 40; // slight upward bias

      const color = this.CONFETTI_COLORS[Math.floor(Math.random() * this.CONFETTI_COLORS.length)];
      // Alternate between squares and circles
      const isCircle = Math.random() > 0.5;
      // Random rotation spin
      const spinDeg  = (Math.random() - 0.5) * 720;
      // Stagger each particle slightly
      const delay    = Math.random() * 200;

      particle.style.cssText = `
        position: fixed;
        left: ${originX}px;
        top:  ${originY}px;
        width:  ${size}px;
        height: ${size}px;
        background: ${color};
        border-radius: ${isCircle ? '50%' : '2px'};
        opacity: 1;
        transform: translate(-50%, -50%);
        animation: confettiFall ${DURATION_MS}ms ease-out ${delay}ms forwards;
        --dx: ${dx}px;
        --dy: ${dy}px;
        --spin: ${spinDeg}deg;
      `;

      container.appendChild(particle);
    }

    // Remove the entire container once the longest animation finishes
    setTimeout(() => {
      container.remove();
    }, DURATION_MS + 250);
  }

  // ── Layer 3: Editor glow ───────────────────────────────────────────────────

  private triggerEditorGlow(): void {
    this.editorGlowing = true;
    // CSS transition handles the 400ms fade; we remove the class after 900ms
    // so the full glow-in + glow-out cycle completes naturally
    setTimeout(() => { this.editorGlowing = false; }, 900);
  }
}
