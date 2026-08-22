# Sprint Status — Bug Fix Batch 1

> **Date:** August 22, 2026  
> **Sprint:** Bug Fix Batch 1  
> **Tracked Issues:** 5

---

## Summary Table

| # | Issue | Layer | Status |
|---|-------|-------|--------|
| 1 | Runtime & memory not shown after submit | Frontend | ✅ Done — bindings were already correct, verified clean |
| 2 | Submission history missing in submissions tab | Frontend | ✅ Done — API wired, table rendered |
| 3 | Settings icon does nothing | Frontend | ✅ Done — modal + PATCH call + role guard |
| 4 | Filter by topic broken (exact match) | Backend | 🔴 Pending backend fix |
| 5 | Instructor submit gives 403 / submit button visible | Frontend + Backend | ✅ Frontend done — button hidden for non-students. 🔴 Backend fix still needed |

---

## What Was Done (Frontend — All Complete)

---

### ✅ Issue 1 — Runtime & Memory After Submit

**Verdict:** Already implemented correctly. No code change needed.

The `executionTime` and `memoryUsed` getters exist in the component and the HTML binds them in two places:
- The result banner chips (pass ratio · runtime · memory)
- The result panel stats row (Test Cases / Runtime / Memory)

```typescript
get executionTime(): string {
  return this.submitResult?.executionTimeMs != null
    ? `${this.submitResult.executionTimeMs} ms` : '—';
}
get memoryUsed(): string {
  return this.submitResult?.memoryUsedKb != null
    ? `${(this.submitResult.memoryUsedKb / 1024).toFixed(1)} MB` : '—';
}
```

These render the real backend values automatically. The `!= null` guard means `0` shows as `0 ms` / `0.0 MB` (correct), and a missing field falls back to `—`.

---

### ✅ Issue 2 — Submission History Tab

**Files changed:** `submission.model.ts`, `submission.service.ts`, `problem-page.component.ts`, `problem-page.component.html`

**What was added:**

1. `SubmissionSummaryResponse` interface in `submission.model.ts`

2. `getSubmissionsByProblem(problemId)` in `SubmissionService` — calls `GET /api/problems/{id}/submissions`

3. State in the component:
   - `submissionHistory: SubmissionSummaryResponse[]`
   - `isSubmissionHistoryLoading`, `submissionHistoryError`, `historyLoaded` (lazy-load guard)

4. `loadSubmissionHistory()` — fetches and populates state

5. `setActiveTab('submissions')` now triggers the fetch on first visit

6. After a successful submit, `historyLoaded` is reset so the submissions tab always shows the fresh entry

7. Full table UI: Status badge · Language · Runtime · Memory · Submitted At — with loading / error / empty states

---

### ✅ Issue 3 — Settings Icon

**Files changed:** `problem-page.component.ts`, `problem-page.component.html`, `problem-page.component.scss`

**What was added:**

- `onSettings()` — pre-populates `settingsForm`, opens modal (returns early for non-instructor/admin)
- `closeSettingsModal()` — closes on button click or backdrop click
- `saveSettings()` — calls `PATCH /api/problems/{id}` via `problemService.update()`, updates component state on success, shows toast on error
- `canEditProblem` getter — `true` only for `instructor` / `admin` roles
- Settings icon now wrapped in `@if (canEditProblem)` — students never see it
- Edit modal with Title, Difficulty (dropdown), and Description (textarea) fields
- Full SCSS for modal: backdrop, panel, header, body, footer, inputs

---

### ✅ Issue 5 — Instructor Submit Button (Frontend half)

**Files changed:** `problem-page.component.ts`, `problem-page.component.html`

**What was added:**

- `canSubmit` getter — returns `true` only when `role === 'student'`
- All 3 Submit button instances wrapped in `@if (canSubmit)`:
  - Toolbar Submit button + validation error message
  - Bottom panel "Submit Solution" button
  - Mobile floating action bar Submit button

Instructors and admins see the problem page normally, can run code and use AI hints, but the submit buttons are completely absent from the DOM.

**Backend fix still needed:** `[Authorize(Roles = "Student,Instructor")]` in `SubmissionsController.cs` — so instructors can submit if they ever call the endpoint directly (e.g. via API tools or future roles expansion).

---

## What Backend Still Needs to Do

---

### � Issue 4 — Filter by Topic (Backend)

**File:** `src/Codify.Infrastructure/Repositories/ProblemRepository.cs`

**Change:**
```csharp
// ❌ Current — exact match
query = query.Where(p => p.ProblemTags.Any(pt => pt.ConceptTag.Name == filter.Tag));

// ✅ Fix — case-insensitive contains via SQL LIKE
query = query.Where(p => p.ProblemTags.Any(
    pt => EF.Functions.Like(pt.ConceptTag.Name, $"%{filter.Tag}%")));
```

Effort: XS (1 line). No frontend change needed — the frontend already sends the correct `tag` query param.

---

### � Issue 5 — Instructor Submit 403 (Backend)

**File:** `src/Codify.API/Controllers/SubmissionsController.cs`

**Change:**
```csharp
// ❌ Current
[Authorize(Roles = "Student")]

// ✅ Fix
[Authorize(Roles = "Student,Instructor")]
```

Effort: XS (1 line). Frontend already hides the button for instructors — this backend change covers direct API calls and future role flexibility.

---

## Sprint Tree

```
Sprint: Bug Fix Batch 1
│
├── 🎨 FRONTEND ── ALL DONE ✅
│   │
│   ├── F1  Runtime & memory display            ✅  Already wired — no change needed
│   ├── F2  Submission history tab              ✅  API + table + lazy load + post-submit refresh
│   ├── F3  Settings icon                       ✅  Edit modal + PATCH + role guard
│   └── F5  Hide submit for instructors         ✅  canSubmit getter, 3 button sites guarded
│
└── 🔧 BACKEND ── 2 lines left
    │
    ├── B4  Tag filter: == → EF.Functions.Like   🔴  1 line in ProblemRepository.cs
    └── B5  Instructor submit: add role          🔴  1 line in SubmissionsController.cs
```

---

## Checklist

- [x] F1 — Runtime & memory confirmed wired
- [x] F2 — Submissions tab fetches real history, refreshes after each submit
- [x] F3 — Settings icon opens edit modal, saves via PATCH, hidden for students
- [x] F5 — Submit button absent for instructors/admins (all 3 button sites)
- [ ] B4 — `ProblemRepository.cs`: `EF.Functions.Like` for tag filter
- [ ] B5 — `SubmissionsController.cs`: add `Instructor` to submit role
