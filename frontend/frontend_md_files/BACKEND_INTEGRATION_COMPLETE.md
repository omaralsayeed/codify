# Backend Integration Complete — Login, Register, Problems

**Date:** August 11, 2026  
**Scope:** Auth + Problems endpoints (Login, Register, Problem List, Problem Detail)  
**Status:** ✅ Complete — Ready for testing with backend

---

## What Was Done

Successfully wired up the Angular frontend to the real ASP.NET Core backend for:

1. **Login** (`POST /api/auth/login`)
2. **Register** (`POST /api/auth/register`)
3. **Problems List** (`GET /api/problems`)
4. **Problem Detail** (`GET /api/problems/{id}`)

All other features (code execution, submission, AI hints, analytics, instructor features) remain **intentionally mocked** — the backend is not ready for those yet (missing Judge0 config, missing OpenAI API key, missing routes).

---

## Files Modified

### 1. Created Enum Mapper Utility
**File:** `src/app/core/utils/enum-mappers.ts` (NEW)

Maps backend enum integers to frontend string literals:
- **Difficulty:** `0 → 'easy'`, `1 → 'medium'`, `2 → 'hard'`
- **UserRole:** `0 → 'student'`, `1 → 'instructor'`
- **Language:** `0 → 'Python'`, `1 → 'CSharp'`

---

### 2. AuthService — Wired to Backend
**File:** `src/app/core/services/auth.service.ts`

**Changes:**
- ❌ Removed mock user array
- ✅ `login()` now calls `POST /api/auth/login`
  - Maps `userId → id`, `fullName → name`, `role (number) → role (string)`
  - Stores JWT token in `localStorage['codify_token']`
  - Generates `avatarInitials` from full name on frontend
- ✅ `register()` now calls `POST /api/auth/register`
  - Sends only: `{ fullName, email, password, role: 0|1 }`
  - Does NOT send `organization`, `phoneNumber`, `country`, `city` (backend doesn't accept them yet per integration guide)
  - Automatically calls `login()` after successful registration (backend returns no token on register)

**Backward compatibility:** Session restore, logout, and signal-based auth state unchanged.

---

### 3. ProblemService — Wired to Backend
**File:** `src/app/core/services/problem.service.ts`

**Changes:**
- ✅ `getAll()` now returns `Observable<Problem[]>` from `GET /api/problems`
  - Supports query filters: `difficulty`, `tag`, `search`, `page`, `pageSize`
  - Maps backend `difficulty` (number) → frontend (string)
  - Maps backend `tags` (array) → frontend `topic` (slug) + `topicLabel` (joined)
- ✅ `getById(id)` now returns `Observable<any>` from `GET /api/problems/{id}`
  - Maps `statement → description`, `constraints` (string) → array, `sampleTestCases → examples`
  - Starter code still hardcoded (backend doesn't provide it yet)
- ✅ Added JWT `Authorization` header via `headers()` helper
- ⚠️ Added synchronous fallback methods for **mocked instructor features**:
  - `getAllSync()`, `searchSync()`, `getRecommendedSync()`
  - These use a hardcoded mock array so instructor components compile without async refactoring
  - **Remove these once instructor endpoints are ready**

---

### 4. ProblemListComponent — Loads from Backend
**File:** `src/app/features/problem-list/problem-list.component.ts`

**Changes:**
- ✅ Implements `OnInit`, calls `loadProblems()` on mount
- ✅ Stores problems in `allProblems: Problem[]` after async fetch
- ✅ Client-side filtering still works (filter by topic/difficulty in memory)
- ✅ Added loading state (`isLoading`) and error state (`errorMessage`)

**File:** `src/app/features/problem-list/problem-list.component.html`

**Changes:**
- ✅ Shows loading spinner while fetching
- ✅ Shows error message if fetch fails
- ✅ Wraps table in `@if (!isLoading)` block

---

### 5. ProblemPageComponent — Loads Dynamic Problem Data
**File:** `src/app/features/problem-page/problem-page.component.ts`

**Changes:**
- ✅ Injected `ActivatedRoute` and `ProblemService`
- ✅ Reads `problemId` from route params in `ngOnInit()`
- ✅ Calls `loadProblem(id)` to fetch problem details from backend
- ✅ Populates dynamic fields: `problemTitle`, `problemDifficulty`, `problemDescription`, `problemConstraints`, `problemExamples`
- ✅ Updates `onRun()`, `onSubmit()`, `onHintRequested()` to use `this.problemId` instead of hardcoded ID
- ✅ Added loading/error states

**File:** `src/app/features/problem-page/problem-page.component.html`

**Changes:**
- ❌ Removed hardcoded "Two Sum" title, description, examples, constraints
- ✅ Replaced with dynamic bindings: `{{ problemTitle }}`, `{{ problemDescription }}`, `@for (example of problemExamples)`, etc.
- ✅ Added loading state: `@if (isProblemLoading) { ... }`
- ✅ Added error state: `@if (problemLoadError) { ... }`

---

### 6. Instructor Components — Kept Mocked (Sync Fallbacks)
**Files:**
- `src/app/features/instructor/contest-create/instructor-contest-create.component.ts`
- `src/app/features/instructor/contest-detail/instructor-contest-detail.component.ts`
- `src/app/features/home/components/student-dashboard-preview/student-dashboard-preview.component.ts`

**Changes:**
- Changed calls from `problemSvc.search()` → `problemSvc.searchSync()`
- Changed calls from `problemSvc.getAll()` → `problemSvc.getAllSync()`
- Changed calls from `problemSvc.getRecommended()` → `problemSvc.getRecommendedSync()`

**Reason:** These components are part of mocked features (contests, instructor dashboard). They don't need async refactoring yet — synchronous stubs keep them compiling until backend support is added.

---

## Verification

✅ **TypeScript compilation:** `npx tsc --noEmit` — zero errors  
✅ **Angular build:** `npx ng build --configuration development` — success (only Sass deprecation warnings, pre-existing)  

---

## What's Still Mocked

As documented in `FRONTEND_INTEGRATION_GUIDE.md`, these features are **intentionally not wired** — backend is not ready:

| Feature | Status | Reason Not Ready |
|---|---|---|
| Run Code | ⚠️ Mocked | Needs Judge0 running locally |
| Submit Code | ⚠️ Mocked | Needs Judge0 running locally |
| AI Hints | ⚠️ Mocked | Needs OpenAI API key configured |
| AI Feedback | ⚠️ Mocked | Needs OpenAI API key configured |
| Student Progress | ⚠️ Mocked | Not built yet (Sprint 2) |
| Class Progress | ⚠️ Mocked | Not built yet (Sprint 2) |
| Forgot Password | ⚠️ Mocked | Not built yet |
| Recommended Problems | ⚠️ Mocked | Not built yet |
| All Instructor Features | ⚠️ Mocked | Not built yet (Sprint 4) |
| All Contest Features | ⚠️ Mocked | Not built yet (Sprint 4) |

---

## Testing Checklist

Before testing with the live backend, ensure:

1. **Backend is running:** `http://localhost:5237` is accessible
2. **CORS is configured:** Backend allows requests from `http://localhost:4200`
3. **Database is seeded:** At least one problem exists in the DB (get a real UUID from Swagger)
4. **Test credentials exist:** Either:
   - Use seeded users from backend (`student@codify.com` / `instructor@codify.com`)
   - OR register a new account (will auto-login after registration)

### End-to-End Test Flow

1. **Register a new student:**
   - Navigate to `/auth/register`
   - Fill form (min 8-char password required)
   - Submit → should redirect to `/problems` after successful login

2. **Login with existing credentials:**
   - Navigate to `/auth/login`
   - Use `student@codify.com` / `123456` (or your registered account)
   - Submit → should redirect to `/problems`

3. **Browse problems list:**
   - Navigate to `/problems`
   - Should see real problems from the database
   - Try filtering by difficulty/topic (client-side for now)

4. **View problem detail:**
   - Click any problem in the list
   - Should load that specific problem's title, description, examples, constraints
   - Difficulty badge should display correctly (not `0`, `1`, `2`)
   - Topic label should show tags joined with ` · `

5. **Verify error cases:**
   - Try logging in with wrong password → should show "Invalid email or password"
   - Try registering with 7-char password → should show validation error
   - Try registering with duplicate email → should show conflict error
   - Clear `localStorage['codify_token']` manually → accessing `/problems` should redirect to `/auth/login`

---

## Known Issues / Limitations

1. **Starter code is still hardcoded** — Backend doesn't provide per-language starter templates yet. All problems show the same "Two Sum" pattern starter code.

2. **`solvedCount` is 0 in problem list** — Backend list response doesn't include `acceptedSubmissionsCount`. Only the detail view shows the real count.

3. **Topic slugs may not match frontend enum exactly** — Backend sends free-form tags (e.g., "Hash Map", "Binary Search Tree"). We map the first tag to a slug with `as any` cast. This may cause badge colors to not apply correctly if the slug doesn't match `Topic` union values.

4. **No pagination UI yet** — Problem list fetches all at once. The service supports `page` and `pageSize` query params, but the component doesn't expose pagination controls yet.

5. **Synchronous fallback methods in ProblemService** — `getAllSync()`, `searchSync()`, `getRecommendedSync()` are temporary hacks for mocked instructor components. Remove these once instructor endpoints are wired.

---

## Next Steps

1. **Test with live backend** — Follow testing checklist above
2. **Get real problem UUIDs from Swagger** — Update any hardcoded IDs in test data
3. **Wire up code execution** once Judge0 is configured (not in this PR scope)
4. **Wire up submissions** once Judge0 is configured (not in this PR scope)
5. **Wire up AI hints** once OpenAI key is configured (not in this PR scope)
6. **Clean up sync fallback methods** once instructor endpoints are built

---

**Integration Guide Reference:** See `FRONTEND_INTEGRATION_GUIDE.md` for full backend API contract and field mappings.

