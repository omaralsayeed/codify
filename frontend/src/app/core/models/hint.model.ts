/**
 * AI Hint models.
 *
 * Two distinct shapes live here:
 *
 * 1. `Hint`         — the original UI bubble model used by HintBubbleComponent
 *                     and the hero decorative preview. Unchanged.
 *
 * 2. `HintRequest`  — mirrors Codify.Application.DTOs.AI.HintRequest exactly.
 *    `HintResponse` — mirrors Codify.Application.DTOs.AI.HintResponse exactly.
 *    Used by HintService to talk to POST /api/ai/hints.
 */

import { SubmissionStatus } from './submission.model';

// ── UI bubble model (original — do not remove) ────────────────────────────────
export interface Hint {
  stepIndex:  number;
  totalSteps: number;
  text:       string;
  problemId:  string;
}

// ── Backend API types ─────────────────────────────────────────────────────────

export type HintLevel = 1 | 2 | 3;

export interface HintRequest {
  problemId:            string;           // GUID string
  studentCode:          string;
  hintLevel:            HintLevel;        // 1–3; backend enforces MinHintLevel/MaxHintLevel
  previousHints:        string[];         // hintText values from prior calls in this session
  attemptCount?:        number;
  lastSubmissionStatus?: SubmissionStatus;
}

export interface HintResponse {
  hintText:         string;
  hintLevel:        number;
  followUpQuestion?: string;
  hasMoreHints:     boolean;
}
