import { Component, OnInit, OnDestroy, inject, HostListener, signal, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { Subject, takeUntil } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { SubmissionService } from '../../core/services/submission.service';
import { HintService } from '../../core/services/hint.service';
import {
  RunCodeResponse,
  SubmissionDetailResponse,
  ServiceError,
  SubmissionFeedback,
  FeedbackType,
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
  level:        number;
  explanation:  string;  // the hintText displayed to the user
  appliedAt:    Date;
  codeSnapshot: string;  // code state BEFORE this hint was applied — stored for future undo UI
}

@Component({
  selector: 'app-problem-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './problem-page.component.html',
  styleUrl: './problem-page.component.scss',
})
export class ProblemPageComponent implements OnInit, OnDestroy {
  protected readonly auth          = inject(AuthService);
  private  readonly submissionSvc  = inject(SubmissionService);
  private  readonly hintSvc        = inject(HintService);
  private  readonly elRef          = inject(ElementRef);
  private  readonly router         = inject(Router);

  /** Direct reference to the code textarea for imperative value sync after hint apply */
  @ViewChild('editorTextarea') private editorTextareaRef?: ElementRef<HTMLTextAreaElement>;

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
  submissionId:    string | null = null;   // stored for AI feedback fetch
  activeBottomTab: 'testcases' | 'result' | 'feedback' = 'testcases';

  // ── AI Feedback state (Chunk 3) ───────────────────────────────────────────
  submissionFeedback:   SubmissionFeedback | null = null;
  isFeedbackLoading:    boolean = false;
  feedbackError:        string | null = null;
  activeFeedbackFilter: FeedbackType | 'all' = 'all';

  // ── AI Feedback animation state (Chunk 6) ─────────────────────────────────
  // scoreAnimationPlayed: true after the count-up fires once per submission.
  // displayedScore: the value shown in the template (ticks from 0 → real score).
  scoreAnimationPlayed: boolean = false;
  displayedScore:       number  = 0;
  private scoreCounterId: ReturnType<typeof setInterval> | null = null;

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

  // ── AI Feedback derived getters (Chunk 5 & 6) ────────────────────────────

  /**
   * Items after applying activeFeedbackFilter.
   * Returns the full list when filter is 'all', otherwise filters by type.
   */
  get filteredFeedbackItems() {
    if (!this.submissionFeedback) return [];
    if (this.activeFeedbackFilter === 'all') return this.submissionFeedback.feedbackItems;
    return this.submissionFeedback.feedbackItems.filter(
      item => item.type === this.activeFeedbackFilter,
    );
  }

  /** Count per type — used by filter pill labels. */
  feedbackCountOf(type: FeedbackType): number {
    return this.submissionFeedback?.feedbackItems.filter(i => i.type === type).length ?? 0;
  }

  /** Number of high-severity items — drives the warning strip. */
  get highSeverityCount(): number {
    return this.submissionFeedback?.feedbackItems.filter(i => i.severity === 'high').length ?? 0;
  }

  /** True when overallScore === 100 — adds the ✨ decoration. */
  get hasPerfectScore(): boolean {
    return (this.submissionFeedback?.overallScore ?? 0) === 100;
  }

  /**
   * CSS class for the score number based on the 0–49 / 50–74 / 75–100 scale.
   * Uses displayedScore so the color updates as the counter ticks up.
   */
  get scoreColorClass(): string {
    if (this.displayedScore >= 75) return 'score--high';
    if (this.displayedScore >= 50) return 'score--mid';
    return 'score--low';
  }

  /**
   * True when the loaded feedback has zero items (API returned an empty array).
   * Shows a "your code looks clean!" message instead of the filter + list.
   */
  get feedbackIsEmpty(): boolean {
    return !!this.submissionFeedback && this.submissionFeedback.feedbackItems.length === 0;
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

  /**
   * Toolbar button label — four states per spec:
   *   • "AI Hint"           — no hints used yet
   *   • "Thinking…"         — loading
   *   • "AI Hint · 2/3"     — hints in progress (counter shows spend)
   *   • "No more hints"     — exhausted
   */
  get hintButtonLabel(): string {
    if (this.isHintLoading) return 'Thinking…';
    if (!this.canRequestMoreHints && this.hintLevel > 0) return 'No more hints';
    if (this.hintLevel === 0) return 'AI Hint';
    return `AI Hint · ${this.hintLevel}/${this.totalHintsAvailable || 3}`;
  }

  // ── Hint overlay card state (Chunk 8) ─────────────────────────────────────
  showHintOverlay:         boolean = false;
  hintOverlayExplanation:  string  = '';
  hintOverlayLevel:        number  = 0;
  hintOverlayProgress:     number  = 100; // 100→0 over 6s, drives the countdown bar
  private hintOverlayTimerId:   ReturnType<typeof setTimeout>  | null = null;
  private hintOverlayIntervalId: ReturnType<typeof setInterval> | null = null;

  // ── Editor highlight state (Chunk 8) ─────────────────────────────────────
  editorHighlighting: boolean = false;  // drives .editor-textarea--highlight class

  private get previousHintTexts(): string[] {
    return this.hintHistory.map(h => h.explanation);
  }

  // ── Private drag state ────────────────────────────────────────────────────
  private isDragging = false;
  private readonly MIN_PANEL_WIDTH = 20;
  private readonly MAX_PANEL_WIDTH = 80;

  // ── Bottom panel resize state ─────────────────────────────────────────────
  testPanelHeight = 240;
  private readonly MIN_PANEL_HEIGHT = 120;
  private readonly MAX_PANEL_HEIGHT = 600;
  private isBottomDragging = false;
  private originalStarterCode = '';

  /** Completes on ngOnDestroy — all HTTP subscriptions use takeUntil(destroy$) */
  private readonly destroy$ = new Subject<void>();

  // ── Validation & toast state (Chunk 9) ───────────────────────────────────
  submitValidationError: string | null = null;  // inline message near Submit button
  toastMessage:          string | null = null;  // brief dismissible toast
  private toastTimerId:  ReturnType<typeof setTimeout> | null = null;

  // ── title attribute getters for accessibility ────────────────────────────
  get submitBtnTitle(): string {
    if (this.isSubmitting)            return 'Submitting your solution…';
    if (this.submitValidationError)   return this.submitValidationError;
    return 'Submit your solution';
  }

  get hintBtnTitle(): string {
    if (this.isHintLoading)           return 'Fetching hint…';
    if (!this.canRequestMoreHints)    return 'No more hints available';
    return `Request hint ${this.nextHintLevel} of ${this.totalHintsAvailable || 3}`;
  }

  constructor() {
    const python = this.languages.find(l => l.value === 'python')!;
    this.currentCode         = python.starterCode;
    this.originalStarterCode = python.starterCode;
  }

  // ── Lifecycle ─────────────────────────────────────────────────────────────

  ngOnInit(): void {
    // Services verified — both SubmissionService and HintService return data correctly.
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.clearHintOverlayTimers();
    this.clearScoreCounter();
    if (this.toastTimerId !== null) { clearTimeout(this.toastTimerId); }
  }

  // ── Toast helper ─────────────────────────────────────────────────────────

  showToast(message: string, durationMs = 3500): void {
    if (this.toastTimerId !== null) { clearTimeout(this.toastTimerId); }
    this.toastMessage  = message;
    this.toastTimerId  = setTimeout(() => {
      this.toastMessage = null;
      this.toastTimerId = null;
    }, durationMs);
  }

  dismissToast(): void {
    this.toastMessage = null;
    if (this.toastTimerId !== null) { clearTimeout(this.toastTimerId); this.toastTimerId = null; }
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
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next:  result => { this.runResult = result; this.runPhase = 'done'; },
        error: (err: ServiceError) => { this.runError = err; this.runPhase = 'error'; },
      });
  }

  // ── Toolbar: Submit ───────────────────────────────────────────────────────

  onSubmit(): void {
    // ── Edge case: empty editor ───────────────────────────────────────────
    if (!this.currentCode.trim()) {
      this.submitValidationError = 'Please write some code before submitting.';
      // Auto-clear after 4 s so it doesn't linger
      setTimeout(() => { this.submitValidationError = null; }, 4000);
      return;
    }
    this.submitValidationError = null;

    // ── Guard: already submitting (button disabled, but belt-and-suspenders) ─
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
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          this.submitResult    = result;
          this.submissionId    = result.submissionId;
          this.isSubmitting    = false;
          this.submitPhase     = 'done';
          this.submissionError = null;

          // ── Unexpected / unknown status guard ─────────────────────────
          const knownStatuses = ['Accepted','WrongAnswer','RuntimeError','TimeLimitExceeded','CompileError'];
          if (!knownStatuses.includes(result.status)) {
            this.submissionError = 'Something went wrong. Try again.';
            this.submitPhase     = 'error';
            this.isSubmitting    = false;
            return;
          }

          this.submitFlash = result.status === 'Accepted' ? 'accepted' : 'rejected';
          setTimeout(() => { this.submitFlash = null; }, 700);

          if (result.status === 'Accepted') {
            this.isSolved = true;
            this.triggerAcceptedCelebration();
          }
          this.setActiveTab('submissions');

          // Fetch AI feedback in the background — parallel to the user reading
          // the result banner. By the time they click the Feedback tab it will
          // likely already be ready.
          this.fetchFeedback(result.submissionId);
        },
        error: (err: ServiceError) => {
          this.submitError     = err;
          this.submissionError = err.message ?? 'Something went wrong. Try again.';
          this.isSubmitting    = false;
          this.submitPhase     = 'error';
        },
      });
  }

  // ── Toolbar: Copy ─────────────────────────────────────────────────────────

  onCopy(): void {
    navigator.clipboard.writeText(this.currentCode).catch(() => {/* clipboard unavailable */});
  }

  // ── AI Feedback fetch (Chunk 3) ───────────────────────────────────────────

  /**
   * Fetches AI feedback for the just-completed submission.
   * Triggered automatically from onSubmit() — never requires a user click.
   * Runs in parallel with the user reading the result banner so the data
   * is likely ready by the time they switch to the Feedback tab.
   *
   * takeUntil(destroy$) cancels the in-flight request on component destroy,
   * matching the same pattern used by run() and submit() above.
   */
  fetchFeedback(submissionId: string): void {
    this.isFeedbackLoading    = true;
    this.feedbackError        = null;
    this.submissionFeedback   = null;
    // Reset animation state so each new submission gets a fresh count-up
    this.scoreAnimationPlayed = false;
    this.displayedScore       = 0;
    this.clearScoreCounter();

    this.submissionSvc.getSubmissionFeedback(submissionId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: feedback => {
          this.submissionFeedback = feedback;
          this.isFeedbackLoading  = false;

          // If the user is already on the Feedback tab when data arrives,
          // start the count-up immediately; otherwise it starts on first tab switch.
          if (this.activeBottomTab === 'feedback' && !this.scoreAnimationPlayed) {
            this.startScoreCountUp(feedback.overallScore);
          }

          console.log('[FeedbackService ✓] score:', feedback.overallScore,
                      '| items:', feedback.feedbackItems.length);
        },
        error: () => {
          this.feedbackError     = 'Could not load feedback. Try again.';
          this.isFeedbackLoading = false;
          console.error('[FeedbackService ✗] failed to load feedback for', submissionId);
        },
      });
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
      attemptCount:          this.hintLevel,
      lastSubmissionStatus:  this.submitResult?.status ?? undefined,
    }).pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (hint: HintResponse) => {
        // ── Update plain state (spec contract) ────────────────────────────
        this.hintLevel           = hint.hintLevel;
        this.totalHintsAvailable = 3;                // backend max; hasMoreHints drives canAskMore
        this.canRequestMoreHints = hint.hasMoreHints && hint.hintLevel < 3;
        this.lastHintExplanation = hint.hintText;

        const historyItem: HintHistoryItem = {
          level:        hint.hintLevel,
          explanation:  hint.hintText,
          appliedAt:    new Date(),
          codeSnapshot: this.currentCode,  // snapshot BEFORE applyHintToCode mutates it
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
   * Applies code changes from a hint response directly into the editor.
   *
   * Current backend shape: narrative text only — no codeChanges array.
   * When the backend starts returning diffs, pass them as the optional second argument
   * or extend HintResponse with a codeChanges field.
   *
   * Algorithm (reverse-order application):
   *   1. Split currentCode into a lines array.
   *   2. Sort changes from highest lineStart to lowest (bottom → top).
   *      This preserves all line numbers for earlier changes after each splice.
   *   3. For each change, replace lines[lineStart-1..lineEnd-1] with newCode lines.
   *   4. Join back to a string and write to currentCode AND the textarea DOM node.
   *   5. Store the first change's explanation as lastHintExplanation.
   *   6. Trigger the hint-applied animation (placeholder — Chunk 8 will style it).
   *
   * @param hint        Backend HintResponse (always present).
   * @param codeChanges Optional diff array from HintApiResponse.codeChanges.
   *                    Pass this when the backend or a mock supplies line-level diffs.
   */
  applyHintToCode(hint: HintResponse, codeChanges?: HintCodeChange[]): void {
    const changes = codeChanges ?? [];

    if (changes.length === 0) {
      // Narrative-only hint — nothing to splice into the editor.
      // Per spec: if codeChanges is explicitly empty (not just absent), show a brief toast.
      // In practice the backend never sends an empty array — guard for robustness.
      if (codeChanges !== undefined) {
        this.showToast('No code changes for this hint level.');
      }
      console.log(
        '[applyHintToCode] level', hint.hintLevel,
        '— narrative hint only, no code changes applied.',
        '\nHint text:', hint.hintText,
      );
      return;
    }

    // ── Step 1: Split into lines ──────────────────────────────────────────────
    const lines = this.currentCode.split('\n');

    // ── Step 2: Sort highest lineStart first (bottom → top) ──────────────────
    // Sorting descending means later lines are processed first, so each splice
    // does not shift the indices that subsequent (earlier) changes depend on.
    const sorted = [...changes].sort((a, b) => b.lineStart - a.lineStart);

    // ── Step 3: Apply each change ─────────────────────────────────────────────
    for (const change of sorted) {
      // Clamp to valid range — guard against malformed API data
      const start = Math.max(1, change.lineStart);
      const end   = Math.min(lines.length, change.lineEnd);

      // Number of original lines to remove (inclusive range)
      const deleteCount = end - start + 1;

      // newCode may itself be multi-line
      const replacementLines = change.newCode.split('\n');

      lines.splice(start - 1, deleteCount, ...replacementLines);
    }

    // ── Step 4: Commit to component state AND textarea DOM ────────────────────
    const updatedCode = lines.join('\n');
    this.currentCode  = updatedCode;

    // [value]="currentCode" is a one-way binding — Angular won't re-render the
    // textarea's DOM value until the next change-detection cycle that it detects
    // as a real change. Setting the native value directly gives instant feedback.
    if (this.editorTextareaRef?.nativeElement) {
      this.editorTextareaRef.nativeElement.value = updatedCode;
    }

    // ── Step 5: Surface the explanation ──────────────────────────────────────
    // Use the first change's explanation (most contextually relevant).
    this.lastHintExplanation = sorted[sorted.length - 1].explanation; // first in original order

    // ── Step 6: Trigger hint-applied animation (Chunk 8) ─────────────────────
    this.triggerHintAppliedAnimation();

    console.log(
      '[applyHintToCode] applied', changes.length, 'change(s) at level', hint.hintLevel,
      '\nlines affected:', sorted.map(c => `${c.lineStart}-${c.lineEnd}`).join(', '),
    );
  }

  /**
   * Chunk 8 — Hint-applied animation: two effects.
   *
   * 1. Editor textarea green highlight (800ms CSS class toggle).
   *    Applied on any hint, whether narrative or code-diff, so the user
   *    always gets a visual signal that the hint has been received.
   *
   * 2. Shows / updates the hint explanation overlay card.
   *    The card auto-dismisses after 6 s with a countdown progress bar.
   *    If called while the overlay is already visible it updates in-place.
   */
  private triggerHintAppliedAnimation(): void {
    // ── Editor textarea highlight ─────────────────────────────────────────
    if (!this.prefersReducedMotion()) {
      this.editorHighlighting = true;
      setTimeout(() => { this.editorHighlighting = false; }, 800);
    }

    // ── Overlay card (always shown — carries the explanation text) ────────
    this.showHintOverlayCard(this.lastHintExplanation, this.hintLevel);
  }

  /** Opens or refreshes the hint explanation overlay and starts the 6 s timer. */
  showHintOverlayCard(explanation: string, level: number): void {
    // Update content (in-place refresh if already visible)
    this.hintOverlayExplanation = explanation;
    this.hintOverlayLevel       = level;
    this.showHintOverlay        = true;

    // Clear any running timer + interval from the previous hint
    this.clearHintOverlayTimers();

    // Reset progress bar to full
    this.hintOverlayProgress = 100;

    const DURATION_MS    = 6000;
    const TICK_MS        = 100;
    const totalTicks     = DURATION_MS / TICK_MS;
    let   ticksElapsed   = 0;

    // Smooth countdown — decrements progress bar every 100 ms
    this.hintOverlayIntervalId = setInterval(() => {
      ticksElapsed++;
      this.hintOverlayProgress = Math.max(0, 100 - (ticksElapsed / totalTicks) * 100);
    }, TICK_MS);

    // Auto-dismiss after full duration
    this.hintOverlayTimerId = setTimeout(() => {
      this.dismissHintOverlay();
    }, DURATION_MS);
  }

  dismissHintOverlay(): void {
    this.showHintOverlay    = false;
    this.clearHintOverlayTimers();
  }

  private clearHintOverlayTimers(): void {
    if (this.hintOverlayTimerId   !== null) { clearTimeout(this.hintOverlayTimerId);    this.hintOverlayTimerId   = null; }
    if (this.hintOverlayIntervalId !== null) { clearInterval(this.hintOverlayIntervalId); this.hintOverlayIntervalId = null; }
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

    // Check whether the /codify/chat route is registered before navigating.
    // The router.navigate call will succeed silently on unknown routes, so
    // we inspect the config explicitly.
    const chatRouteExists = this.router.config.some(
      r => r.path === 'codify' || r.path?.startsWith('codify/chat'),
    );

    if (chatRouteExists) {
      this.router.navigate(['/codify/chat'], { queryParams });
    } else {
      // Route not registered yet — show a friendly toast instead of silent no-op
      this.showToast('Codify Chat coming soon.');
      console.log('[navigateToCodifyChat] route not yet registered. Params ready:', queryParams);
      this.setActiveTab('codify');   // fall back to the hints tab
    }

    this.dismissHintOverlay();
  }

  onSettings(): void {}

  // ── Tab switching ─────────────────────────────────────────────────────────
  setActiveTab(tab: ActiveTab): void { this.activeTab = tab; }

  setBottomTab(tab: 'testcases' | 'result' | 'feedback'): void {
    this.activeBottomTab = tab;
    // Start score count-up on first switch to the Feedback tab after data arrives
    if (tab === 'feedback'
        && this.submissionFeedback
        && !this.scoreAnimationPlayed) {
      this.startScoreCountUp(this.submissionFeedback.overallScore);
    }
  }

  /**
   * Counts displayedScore from 0 → target over ~800 ms using a setInterval.
   * Fires once per submission (scoreAnimationPlayed guards re-entry).
   * Skipped entirely under prefers-reduced-motion — displayedScore jumps to target.
   */
  private startScoreCountUp(target: number): void {
    if (this.scoreAnimationPlayed) return;
    this.scoreAnimationPlayed = true;

    if (this.prefersReducedMotion() || target === 0) {
      this.displayedScore = target;
      return;
    }

    const DURATION_MS = 800;
    const TICK_MS     = 16;           // ~60 fps
    const totalTicks  = Math.ceil(DURATION_MS / TICK_MS);
    let   tick        = 0;

    this.clearScoreCounter();
    this.displayedScore = 0;

    this.scoreCounterId = setInterval(() => {
      tick++;
      // Ease-out: progress² gives a fast start, gentle finish
      const progress      = tick / totalTicks;
      const eased         = 1 - Math.pow(1 - progress, 2);
      this.displayedScore = Math.min(target, Math.round(eased * target));

      if (tick >= totalTicks) {
        this.displayedScore = target;  // guarantee exact final value
        this.clearScoreCounter();
      }
    }, TICK_MS);
  }

  private clearScoreCounter(): void {
    if (this.scoreCounterId !== null) {
      clearInterval(this.scoreCounterId);
      this.scoreCounterId = null;
    }
  }

  /**
   * Scrolls the code textarea to the given 1-based line number and briefly
   * flashes the editor highlight animation so the user can see where to look.
   *
   * The editor is a plain <textarea> (no Monaco/CodeMirror), so we compute
   * the character offset of the target line by splitting on '\n', then use
   * the browser's native scrollTop API after setting the selection range.
   *
   * If the editor is not visible (e.g., mobile problem-view is active), we
   * switch to the editor view first so the scroll lands on screen.
   */
  jumpToLine(lineNumber: number): void {
    if (!this.editorTextareaRef?.nativeElement) return;

    // On mobile the editor panel may be hidden — switch to it first
    if (this.activeView !== 'editor') {
      this.activeView = 'editor';
    }

    const textarea = this.editorTextareaRef.nativeElement;
    const lines    = textarea.value.split('\n');

    // Clamp to valid range
    const targetLine  = Math.max(1, Math.min(lineNumber, lines.length));
    // Character offset = sum of lengths of all lines before the target + newline chars
    const charOffset  = lines.slice(0, targetLine - 1).reduce((sum, l) => sum + l.length + 1, 0);

    // Move cursor to the line and scroll it into view
    textarea.focus();
    textarea.setSelectionRange(charOffset, charOffset);

    // scrollTop: approximate line height is 20px (matches .editor-textarea line-height)
    const LINE_HEIGHT_PX = 20;
    const visibleLines   = Math.floor(textarea.clientHeight / LINE_HEIGHT_PX);
    textarea.scrollTop   = Math.max(0, (targetLine - Math.floor(visibleLines / 2)) * LINE_HEIGHT_PX);

    // Reuse the existing green highlight animation from the hint system
    if (!this.prefersReducedMotion()) {
      this.editorHighlighting = true;
      setTimeout(() => { this.editorHighlighting = false; }, 800);
    }

    console.log('[jumpToLine] scrolled to line', targetLine);
  }

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

  onBottomResizerMouseDown(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isBottomDragging = true;
    document.body.style.cursor     = 'row-resize';
    document.body.style.userSelect = 'none';
    if (!this.isBottomPanelOpen) {
      this.isBottomPanelOpen = true;
    }
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent): void {
    if (this.isDragging) {
      const container = document.querySelector('.split-container') as HTMLElement;
      if (!container) return;
      const rect = container.getBoundingClientRect();
      const pct  = ((event.clientX - rect.left) / rect.width) * 100;
      this.splitPosition = Math.max(this.MIN_PANEL_WIDTH, Math.min(this.MAX_PANEL_WIDTH, pct));
    }
    if (this.isBottomDragging) {
      const container = document.querySelector('.right-panel-content') as HTMLElement;
      if (!container) return;
      const rect = container.getBoundingClientRect();
      // Distance from cursor up to the bottom of the container = desired panel height
      const newHeight = rect.bottom - event.clientY;
      this.testPanelHeight = Math.max(this.MIN_PANEL_HEIGHT, Math.min(this.MAX_PANEL_HEIGHT, newHeight));
    }
  }

  @HostListener('document:mouseup')
  onMouseUp(): void {
    if (this.isDragging) {
      this.isDragging = false;
      document.body.style.cursor     = '';
      document.body.style.userSelect = '';
    }
    if (this.isBottomDragging) {
      this.isBottomDragging = false;
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
