# Codify — Frontend ↔ Backend Integration Journey

> **Project:** Codify — Coding Challenge Platform  
> **Stack:** Angular 21 (frontend) · ASP.NET Core (backend)  
> **Base URL:** `http://localhost:5237/api`  
> **Started:** August 11, 2026  
> **Author:** Kiro (AI dev agent)

---

## Overview

This document is the living record of every step taken to wire the Angular frontend to the real ASP.NET Core backend. It lists every file touched, every decision made, every workaround applied, and what still needs to happen next.

The project arrived with a fully mocked frontend — every service returned hardcoded data, every login worked with an in-memory array. The goal of this integration phase was to replace the mocks with real HTTP calls for **Login, Register, Problems List, and Problem Detail**, while intentionally leaving everything else mocked until the backend is ready for it.

---

## Reference Documents

| File | Purpose |
|---|---|
| `API_GUIDE.md` | Original frontend-authored API spec sent to the backend team |
| `FRONTEND_INTEGRATION_GUIDE.md` | Backend team's response — what they actually built, field differences, enum changes |
| `CODEBASE_SCAN.md` | Pre-integration audit — all issues found in the codebase before any changes |

---

## Pre-Integration State (What We Started With)

Before any changes were made, the frontend was **100% mocked**:

| Feature | State |
|---|---|
| Login | In-memory user array — no HTTP calls |
| Register | Pushed to same in-memory array — no HTTP calls |
| Problem List | Hardcoded array of 9 problems |
| Problem Detail | Hardcoded "Two Sum" data, no routing by ID |
| Code Run | Mocked — returns fake test results |
| Code Submit | Mocked — returns fake verdict after delay |
| AI Hints | Mocked — returns hardcoded hint text |
| AI Feedback | Mocked — returns hardcoded feedback items |
| Student Progress | Mocked — hardcoded stats |
| Instructor Features | Mocked — hardcoded students, flags, contests |

---

## Key Discoveries from the Integration Guide

Before writing a single line of code, we read `FRONTEND_INTEGRATION_GUIDE.md` carefully. The backend team flagged two critical global changes from what the `API_GUIDE.md` had assumed:

### Discovery 1 — Enums Are Numbers, Not Strings

The original API guide expected:
```json
{ "difficulty": "easy", "role": "student" }
```

The backend actually returns:
```json
{ "difficulty": 0, "role": 0 }
```

This affected every single service that touches difficulty, role, or language. Required a shared mapping layer before touching any service.

### Discovery 2 — User Object Fields Renamed

| What frontend expected | What backend returns |
|---|---|
| `user.id` | `user.userId` |
| `user.name` | `user.fullName` |
| `user.role = "student"` | `user.role = 0` |
| `user.avatarInitials` | Not returned — generate on frontend |
| `user.streak` | Not returned — default to 0 |

### Discovery 3 — Register Returns No Token

The original guide assumed register would return a JWT. The backend returns `201` with just `{ userId, email, role }`. You have to call login immediately after to get a token.

### Discovery 4 — Tags Replace Topic

Problems list used a single `topic` string. Backend returns a `tags` array. Frontend `topic` and `topicLabel` both need to be derived from `tags`:
- `topic` = `tags[0]` lowercased, spaces replaced with `-`
- `topicLabel` = `tags.join(' · ')`

### Discovery 5 — Filter Param Renamed

Original guide used `?topic=arrays`. Backend uses `?tag=Arrays`.

---

## What We Built — Sprint 1 (August 11, 2026)

### Step 1 — Enum Mapper Utility

**Decision:** Rather than scatter inline `=== 0 ? 'easy' : ...` checks everywhere, create a single utility file all services import from. Keeps the mapping logic in one place and easy to update.

**File created:** `src/app/core/utils/enum-mappers.ts` ✨ NEW

```
src/
  app/
    core/
      utils/
        enum-mappers.ts   ← NEW
```

**What it exports:**

| Function | Direction | Mapping |
|---|---|---|
| `mapDifficulty(n)` | backend → frontend | `0→'easy'`, `1→'medium'`, `2→'hard'` |
| `difficultyToNumber(s)` | frontend → backend | `'easy'→0`, `'medium'→1`, `'hard'→2` |
| `mapRole(n)` | backend → frontend | `0→'student'`, `1→'instructor'` |
| `roleToNumber(s)` | frontend → backend | `'student'→0`, `'instructor'→1` |
| `mapLanguage(n)` | backend → frontend | `0→'Python'`, `1→'CSharp'` |
| `languageToNumber(s)` | frontend → backend | `'Python'→0`, `'CSharp'→1` |

---

### Step 2 — Auth Service

**File modified:** `src/app/core/services/auth.service.ts`

**What changed:**

Before this step, `auth.service.ts` was entirely self-contained — it had its own mock users array, returned `of(...)` observables with fake data, and never imported `HttpClient`. 

After:
- Imported `HttpClient` via `inject()`
- Removed the mock users array entirely
- `login()` replaced with real `POST /api/auth/login` call
- `register()` replaced with real `POST /api/auth/register` call, then auto-chains to `login()` using `switchMap()`
- `logout()`, `restoreSession()`, `isLoggedIn`, `currentUser` signal — **all unchanged**

**Why `switchMap` for register?**

The backend returns `201` on register with no token. We need to log the user in immediately after. `switchMap` lets us chain `register → login` as a single observable stream so the component consuming it still only sees one `subscribe()` call and gets a `User` back from both paths.

**What the register payload sends (and deliberately omits):**

```typescript
// Sent
{ fullName, email, password, role: 0 | 1 }

// NOT sent (form collects them, backend rejects them for now)
// organization, phoneNumber, country, city
```

**Error handling:** `catchError` wraps both calls. HTTP 401, 409, 400 all surface as `{ success: false, error: message }` so login/register components don't need to change.

**Interface types added inside the file** (not exported — only needed internally):

```typescript
interface LoginApiResponse {
  token: string;
  expiresAt: string;
  user: { userId: string; fullName: string; role: number; }
}

interface RegisterApiResponse {
  userId: string;
  email: string;
  role: number;
}
```

---

### Step 3 — Problem Service

**File modified:** `src/app/core/services/problem.service.ts`

This was the biggest rewrite of the batch. The old service was 30 lines with a hardcoded array. The new one is a proper HTTP service.

**What changed:**

| Old | New |
|---|---|
| `getAll(): Problem[]` | `getAll(filters?): Observable<Problem[]>` |
| `getRecommended(): Problem[]` | `getRecommended(): Observable<Problem[]>` |
| `search(q): Problem[]` | `search(q): Observable<Problem[]>` |
| No `getById()` | `getById(id): Observable<any>` |

**Filters supported by `getAll()`:**

```typescript
getAll(filters?: {
  difficulty?: 'easy' | 'medium' | 'hard';  // sent as number
  tag?: string;                              // note: 'tag' not 'topic'
  search?: string;
  page?: number;
  pageSize?: number;
})
```

**Field mapping in `mapProblemSummary()`:**

| Backend field | Frontend field | Transform |
|---|---|---|
| `difficulty: 0` | `difficulty: 'easy'` | `mapDifficulty()` |
| `tags: ['Arrays', 'Hash Map']` | `topic: 'arrays'` | `tags[0].toLowerCase().replace(/\s+/g, '-')` |
| `tags: ['Arrays', 'Hash Map']` | `topicLabel: 'Arrays · Hash Map'` | `tags.join(' · ')` |
| *(not in list response)* | `solvedCount: 0` | hardcoded 0 for now |

**Field mapping in `mapProblemDetail()`:**

| Backend field | Frontend field | Transform |
|---|---|---|
| `statement` | `description` | direct rename |
| `constraints: "a\nb\nc"` | `constraints: ['a','b','c']` | `.split('\n')` |
| `sampleTestCases[].expectedOutput` | `examples[].output` | rename |
| `acceptedSubmissionsCount` | `solvedCount` | direct map |
| *(not returned)* | `starterCode` | hardcoded per language |

**The `as any` cast on `topic`:**

The `Problem` model has `topic: Topic` where `Topic` is a strict union type. Backend tags are free-form strings (e.g. "Binary Search Tree", "Hash Map") that don't always map cleanly to our enum. Rather than break the model or widen the type, we cast with `as any`. This is a deliberate short-term trade-off — when the instructor backend is ready and we have proper tag normalization, this can be cleaned up.

**The sync fallback methods (temporary):**

When we changed `getAll()` to return `Observable<Problem[]>`, three other components that were already mocked broke at build time:

- `instructor-contest-create.component.ts` — called `problemSvc.search(q).map(...)`
- `instructor-contest-detail.component.ts` — called `problemSvc.getAll()` synchronously
- `student-dashboard-preview.component.ts` — called `problemSvc.getRecommended()`

These are all mocked/not-wired features. Rather than do a full async refactor on them (which would mean touching templates, subscriptions, OnDestroy, etc.), we added synchronous stub methods that return the old hardcoded array:

```
getAllSync()           → returns hardcoded mock array
searchSync(q)         → filters hardcoded mock array
getRecommendedSync()  → returns first 3 from mock array
```

These three methods exist only to keep the mocked features compiling. They will be deleted when those features get wired.

---

### Step 4 — Problem List Component

**Files modified:**
- `src/app/features/problem-list/problem-list.component.ts`
- `src/app/features/problem-list/problem-list.component.html`

**What changed in the TS file:**

The component previously called `problemService.getAll()` directly in a computed getter — which worked fine when it returned a synchronous array. Now that `getAll()` returns an Observable, we needed to:

1. Add `OnInit` and subscribe in `ngOnInit()`
2. Store the result in a `allProblems: Problem[]` array
3. Keep the client-side filter getter pointing at `allProblems` instead

Loading and error states were added:
- `isLoading = true` until the HTTP call resolves or errors
- `errorMessage = ''` set on error

**What changed in the HTML:**

Wrapped the entire filter + table block in `@if (!isLoading)`. Added:

```html
@if (isLoading) {
  <div class="loading-state"><p>Loading problems...</p></div>
}
@if (errorMessage) {
  <div class="error-message">{{ errorMessage }}</div>
}
```

Nothing else in the template changed — the `@for` loop, filter dropdowns, difficulty badge component, and `solvedTotal` getter all stayed the same.

---

### Step 5 — Problem Page Component

**Files modified:**
- `src/app/features/problem-page/problem-page.component.ts`
- `src/app/features/problem-page/problem-page.component.html`

This was the most visible change — the problem page had everything hardcoded to "Two Sum". Every piece of content: title, difficulty badge, description, examples, constraints — all static HTML.

**What changed in the TS file:**

Added four new dependencies:
```typescript
private readonly problemSvc = inject(ProblemService);
private readonly route      = inject(ActivatedRoute);
```

Added five new data fields:
```typescript
problemId: string = '';
problemTitle: string = 'Loading...';
problemDifficulty: string = '';
problemDescription: string = '';
problemConstraints: string[] = [];
problemExamples: Array<{input: string; output: string; explanation: string}> = [];
isProblemLoading: boolean = true;
problemLoadError: string | null = null;
```

`ngOnInit()` now reads the route param and calls `loadProblem(id)`:
```typescript
ngOnInit(): void {
  this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
    const id = params.get('id');
    if (id) {
      this.problemId = id;
      this.loadProblem(id);
    }
  });
}
```

Updated three hardcoded problem IDs to use `this.problemId`:
- `onRun()` — was `'00000000-0000-0000-0000-000000000005'`
- `onSubmit()` — was `'00000000-0000-0000-0000-000000000005'`
- `onHintRequested()` — was `'00000000-0000-0000-0000-000000000005'`

**What changed in the HTML:**

Replaced the entire `@if (activeTab === 'description')` block. The old block was ~90 lines of hardcoded HTML including literal "Two Sum" text. The new block:

1. Shows a loading state while `isProblemLoading` is true
2. Shows an error message if `problemLoadError` is set
3. When loaded, renders:
   - `{{ problemTitle }}` instead of `1. Two Sum`
   - `[class]="'badge--' + problemDifficulty"` instead of hardcoded `badge--easy`
   - `{{ problemDescription }}` with `white-space: pre-line` to preserve line breaks
   - `@for (example of problemExamples)` loop instead of 3 hardcoded example blocks
   - `@for (constraint of problemConstraints)` loop instead of hardcoded `<ul>`

Everything outside the description tab (editor, bottom panel, AI feedback, hints, toolbar) was **not touched** — only the left panel description content was dynamic.

---

### Step 6 — Mocked Instructor Components (Compile Fixes)

**Files modified:**
- `src/app/features/instructor/contest-create/instructor-contest-create.component.ts`
- `src/app/features/instructor/contest-detail/instructor-contest-detail.component.ts`
- `src/app/features/home/components/student-dashboard-preview/student-dashboard-preview.component.ts`

These components were already calling the ProblemService synchronously and are entirely mocked features. The changes are one-liners — swapping to the sync fallback methods so the build stays green while the real wiring waits for the backend:

| Component | Before | After |
|---|---|---|
| `contest-create` | `problemSvc.search(q).map(...)` | `problemSvc.searchSync(q).map(...)` |
| `contest-detail` | `problemSvc.getAll()` | `problemSvc.getAllSync()` |
| `dashboard-preview` | `problemSvc.getRecommended()` | `problemSvc.getRecommendedSync()` |

---

## Build Verification

After every significant change, we ran:

```bash
npx tsc --noEmit           # TypeScript type check only
npx ng build --configuration development   # Full Angular build
```

**Final state:**
- ✅ Zero TypeScript errors
- ✅ Zero Angular template errors
- ⚠️ Sass `darken()` deprecation warnings — pre-existing, not introduced by this work (documented in `CODEBASE_SCAN.md` issue #9)

---

## Complete File Change Log

### New Files Created

| File | Purpose |
|---|---|
| `src/app/core/utils/enum-mappers.ts` | Bidirectional enum mapping between backend integers and frontend string literals |
| `INTEGRATION_JOURNEY.md` | This file — the living integration journal |
| `BACKEND_INTEGRATION_COMPLETE.md` | Concise completion summary (auto-generated after sprint) |

### Modified Files

| File | What Changed |
|---|---|
| `src/app/core/services/auth.service.ts` | Replaced mock login/register with real HTTP calls; removed mock user array |
| `src/app/core/services/problem.service.ts` | Replaced hardcoded array with HTTP calls; added field mappers; added sync fallback stubs |
| `src/app/features/problem-list/problem-list.component.ts` | Async `ngOnInit` loading; loading/error states |
| `src/app/features/problem-list/problem-list.component.html` | Added loading/error UI blocks |
| `src/app/features/problem-page/problem-page.component.ts` | Route param reading; `loadProblem()`; dynamic data fields; removed hardcoded problem IDs |
| `src/app/features/problem-page/problem-page.component.html` | Replaced hardcoded "Two Sum" content with dynamic bindings |
| `src/app/features/instructor/contest-create/instructor-contest-create.component.ts` | `search()` → `searchSync()` (compile fix) |
| `src/app/features/instructor/contest-detail/instructor-contest-detail.component.ts` | `getAll()` → `getAllSync()` (compile fix) |
| `src/app/features/home/components/student-dashboard-preview/student-dashboard-preview.component.ts` | `getRecommended()` → `getRecommendedSync()` (compile fix) |

### Untouched Files (Intentionally Mocked)

| File | Why Not Touched |
|---|---|
| `src/app/core/services/submission.service.ts` | Needs Judge0 — not ready |
| `src/app/core/services/hint.service.ts` | Needs OpenAI key — not ready |
| `src/app/core/services/analytics.service.ts` | Sprint 2 — not built yet |
| `src/app/core/services/progress.service.ts` | Sprint 2 — not built yet |
| `src/app/core/services/instructor.service.ts` | Sprint 4 — not built yet |
| `src/app/core/services/contest.service.ts` | Sprint 4 — not built yet |
| `src/app/features/auth/forgot-password/` | Endpoint not built yet |
| `src/app/features/problem-page/problem-page.component.ts` (run/submit/hint calls) | Still mocked — only problem ID was updated |
| All instructor feature components | Sprint 4 — all mocked |
| All profile/progress components | Sprint 2 — all mocked |

---

## Current Integration Status

| Endpoint | Method | Status | Component |
|---|---|---|---|
| `POST /api/auth/login` | HTTP | ✅ Live | `auth.service.ts` |
| `POST /api/auth/register` | HTTP | ✅ Live | `auth.service.ts` |
| `GET /api/problems` | HTTP | ✅ Live | `problem.service.ts` |
| `GET /api/problems/{id}` | HTTP | ✅ Live | `problem.service.ts` |
| `POST /api/auth/forgot-password` | — | ❌ Mocked | Backend not built |
| `POST /api/execution/run` | — | ❌ Mocked | Needs Judge0 |
| `POST /api/submissions` | — | ❌ Mocked | Needs Judge0 |
| `GET /api/submissions/{id}` | — | ❌ Mocked | Needs Judge0 |
| `POST /api/ai/hints` | — | ❌ Mocked | Needs OpenAI |
| `GET /api/submissions/{id}/feedback` | — | ❌ Mocked | Needs OpenAI |
| `GET /api/progress/student` | — | ❌ Mocked | Sprint 2 |
| `GET /api/progress/class` | — | ❌ Mocked | Sprint 2 |
| `GET /api/problems/recommended` | — | ❌ Mocked | Backend not built |
| All `/api/instructor/*` | — | ❌ Mocked | Sprint 4 |
| All `/api/contests/*` | — | ❌ Mocked | Sprint 4 |

---

## Known Limitations After This Sprint

**1. Starter code is still hardcoded**  
Backend doesn't provide per-language starter templates. Every problem shows the same generic starter code regardless of what problem it is. Will be fixed when the backend adds a `starterCode` field to the problem detail response.

**2. `solvedCount` is `0` in the problem list**  
The list endpoint `GET /api/problems` doesn't return `acceptedSubmissionsCount`. That field only exists on the detail endpoint. The Solves column in the table will show 0 for all problems until either: (a) the list endpoint adds the count, or (b) we batch-fetch details (expensive).

**3. Topic badge mismatch risk**  
The `Problem.topic` field is a strict TypeScript union. Backend tags are free-form. We cast `as any` to keep it compiling. If a tag like `"Binary Search Tree"` comes in, the slug `binary-search-tree` won't match the `trees` or `binary-search` values in the union, so topic filter pills won't highlight correctly.

**4. No pagination UI**  
`GET /api/problems` supports `page` and `pageSize` query params. The service passes them through if provided, but the ProblemListComponent has no pagination controls — it fetches everything in one call.

**5. Sync fallback methods will grow stale**  
`getAllSync()`, `searchSync()`, `getRecommendedSync()` in `problem.service.ts` return the old hardcoded mock array. As the real database grows, instructor features using these methods will show out-of-date problem data. This is acceptable while those features are mocked, but becomes a bug once they're wired.

---

## Decisions Log

| Decision | Reasoning |
|---|---|
| Sync fallback methods instead of async refactor for instructor | Instructor features are 100% mocked, refactoring them to async now adds scope with no user-visible benefit |
| `as any` cast on `topic` field | The Topic union is frontend-defined; widening it to `string` would break filter logic elsewhere; cast keeps the build green without changing model semantics |
| `register → switchMap → login` chaining | Backend by design returns no token on register; chaining keeps the register component's `subscribe()` call simple and unchanged |
| Keep `avatarInitials` generation on frontend | Backend integration guide explicitly states it's not returned; frontend already had `generateAvatarInitials()` working correctly |
| Don't touch submission/hint service despite hardcoded IDs | The run/submit/hint calls were using hardcoded `00000000-0000-0000-0000-000000000005`; we updated them to `this.problemId` without touching the mock logic — this was the right minimal change |

---

## Next Sprint — When Backend is Ready

### Sprint 2 (Student Progress, Dashboard)
When `GET /api/progress/student` is ready:
- Update `src/app/core/services/progress.service.ts`
- Update `src/app/features/student-progress/student-progress.component.ts`
- Update `src/app/features/profile/profile.component.ts`
- Fix the hardcoded date in `progress.service.ts` (CODEBASE_SCAN.md issue #4)

### Sprint 3 (Code Execution + Submissions + AI)
When Judge0 is configured and OpenAI key is set:
- Update `src/app/core/services/submission.service.ts`
- Update `src/app/core/services/hint.service.ts`
- The run/submit/hint calls in `problem-page.component.ts` already use `this.problemId` — just need the service to make real HTTP calls

### Sprint 4 (Instructor Features + Contests)
- Update `src/app/core/services/instructor.service.ts`
- Update `src/app/core/services/contest.service.ts`
- **Delete** sync fallback methods from `problem.service.ts`
- Async-refactor the three instructor components that currently use `getAllSync()` / `searchSync()`

---

## Testing Checklist for Sprint 1

Start backend at `http://localhost:5237`, then:

- [ ] Register a new student (`/auth/register`) — password must be 8+ chars
- [ ] Verify auto-redirect to `/problems` after registration
- [ ] Log out and log back in (`/auth/login`) — verify session restores
- [ ] Browse `/problems` — confirm real problems load from DB
- [ ] Filter by difficulty — verify filter works client-side
- [ ] Click a problem — verify correct title/description/examples load
- [ ] Verify difficulty badge shows `Easy`/`Medium`/`Hard`, not `0`/`1`/`2`
- [ ] Try wrong password at login — verify `"Invalid email or password"` shown
- [ ] Try 7-char password at register — verify `"Password must be at least 8 characters."` shown
- [ ] Try duplicate email at register — verify conflict error shown
- [ ] Delete `codify_token` from localStorage, navigate to `/problems` — verify redirect to `/auth/login`
