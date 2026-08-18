# Codify — Final Integration Check

> **Date:** August 17, 2026
> **Branch:** admin-instructor (frontend) | admin-integrations (backend)
> **Purpose:** Shared handoff document between frontend and backend teams
> **Frontend status:** See per-feature status below
> **Backend status:** See "What Backend Still Needs to Build" section

---

## Table of Contents

1. [Endpoints — Complete Status](#1-endpoints--complete-status)
2. [What Is Fully Done End-to-End](#2-what-is-fully-done-end-to-end)
3. [What Backend Still Needs to Build](#3-what-backend-still-needs-to-build)
4. [Warnings & Known Issues](#4-warnings--known-issues)
5. [Mocked Features — Frontend Ready, Backend Pending](#5-mocked-features--frontend-ready-backend-pending)
6. [Field Contracts & Enum Mappings](#6-field-contracts--enum-mappings)
7. [Infrastructure Requirements](#7-infrastructure-requirements)
8. [Quick Test Credentials](#8-quick-test-credentials)

---

## 1. Endpoints — Complete Status

### AUTH

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| POST | `/api/auth/login` | ✅ Wired | ✅ Live | Role 2 = admin; maps to 'admin' on frontend |
| POST | `/api/auth/register` | ✅ Wired | ✅ Live | Instructor → pending; student → auto-login |
| PUT | `/api/auth/avatar` | ✅ Wired | ❌ Not built | Frontend fires & forgets; falls back to localStorage |
| PUT | `/api/auth/profile` | ✅ Wired | ❌ Not built | Graceful 404/501 fallback — local update still sticks |
| POST | `/api/auth/forgot-password` | ⚠️ Form exists, mocked | ❌ Not built | Do not wire yet |

---

### PROBLEMS (Public/Student)

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| GET | `/api/problems` | ✅ Wired | ✅ Live | Filters: difficulty (number), tag, search, page, pageSize |
| GET | `/api/problems/:id` | ✅ Wired | ✅ Live | Used by problem-page and admin edit form |
| GET | `/api/problems/recommended` | ⚠️ Returns first 3 from getAll | ❌ Not built | No dedicated endpoint exists |

---

### ADMIN

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| GET | `/api/admin/stats` | ✅ Wired | ✅ Live | All 10 stats fields consumed |
| GET | `/api/admin/users` | ✅ Wired | ✅ Live | Filters: search, role, status, sortBy, sortDir, page, pageSize |
| GET | `/api/admin/users/:id` | ✅ Wired | ✅ Live | Includes avgScore, streak, recentSubmissions |
| PATCH | `/api/admin/users/:id/status` | ✅ Wired | ✅ Live | activate / set-pending; 403 on admin accounts |
| GET | `/api/admin/problems` | ✅ Wired | ✅ Live | Returns inactive problems too; includes isActive, createdAt |
| POST | `/api/problems` | ✅ Wired | ✅ Live | Admin-only; tags as string names; validates min 50-char statement |
| PATCH | `/api/problems/:id` | ✅ Wired | ✅ Live | Partial update; replaces sampleTestCases and tags entirely if provided |
| PATCH | `/api/problems/:id/status` | ✅ Wired | ✅ Live | Toggles isActive |
| DELETE | `/api/problems/:id` | ✅ Wired | ✅ Live | Soft delete; no undelete endpoint |

---

### ANALYTICS / STUDENT PROGRESS

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| GET | `/api/analytics/profile` | ✅ Wired | ✅ Live | Used by profile, progress, and student dashboard |
| GET | `/api/analytics/profile/:username` | ✅ Wired | ✅ Live | Public profile — no auth required |
| GET | `/api/analytics/overview` | ✅ Wired | ✅ Live | Instructor overview stats |
| GET | `/api/analytics/integrity-flags` | ✅ Wired | ✅ Live | AI-detected plagiarism flags |
| GET | `/api/analytics/profile/:id` | ✅ Wired | ✅ Live | Instructor student detail |

---

### CODE EXECUTION & SUBMISSIONS

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| POST | `/api/execution/run` | ⚠️ Wired for Python/C# only | ✅ Live (needs Judge0) | JavaScript, Java, C++ are mocked on frontend |
| POST | `/api/submissions` | ⚠️ Wired for Python/C# only | ✅ Live (needs Judge0) | Returns 202, frontend polls every 1.5s |
| GET | `/api/submissions/:id` | ⚠️ Wired for Python/C# only | ✅ Live (needs Judge0) | Polls until status leaves Pending/Running |
| GET | `/api/submissions/:id/feedback` | ⚠️ Wired, mocked response | ❌ Not built | AI feedback endpoint does not exist yet |

---

### AI HINTS

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| POST | `/api/ai/hints` | ✅ Wired | ⚠️ Live (needs OpenAI key) | Max 3 levels; previousHints[] passed to avoid repetition |

---

### INSTRUCTOR FEATURES

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| GET | `/api/analytics/overview` | ✅ Wired | ✅ Live | Maps students[], dailyActivity[], topicPerformance[] |
| GET | `/api/analytics/integrity-flags` | ✅ Wired | ✅ Live | Confidence ≥ 0.8 = high, ≥ 0.5 = medium, < 0.5 = low |
| GET | `/api/analytics/profile/:id` | ✅ Wired | ✅ Live | Single student detail with topicStats, recentAccepted |

---

### CONTESTS

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| GET | `/api/contests` | ⚠️ HTTP call written, falls back to mock | ❌ Not built | ContestService calls endpoint; graceful fallback |
| GET | `/api/contests/:id` | ⚠️ HTTP call written, falls back to mock | ❌ Not built | |
| POST | `/api/contests` | ⚠️ HTTP call written, falls back to mock | ❌ Not built | |
| GET | `/api/contests/my-contests` | ⚠️ HTTP call written, falls back to mock | ❌ Not built | Student enrolled contests view |
| GET | `/api/contests/:id/results` | ⚠️ HTTP call written, falls back to mock | ❌ Not built | Leaderboard sorted by rank |

---

### AVATAR / PROFILE PERSISTENCE

| Method | Endpoint | Frontend | Backend | Notes |
|--------|----------|----------|---------|-------|
| PUT | `/api/auth/avatar` | ✅ Wired (fire-and-forget) | ❌ Not built | Must add `AvatarUrl` column to Users table |
| PUT | `/api/auth/profile` | ✅ Wired | ❌ Not built | Optimistic local update; backend is optional |

---

## 2. What Is Fully Done End-to-End

These features work completely with real data from the database. No mocks involved.

### ✅ Authentication
- Login with JWT (role 0/1/2 mapped to student/instructor/admin)
- Register as student (auto-login after 201)
- Register as instructor (redirects to `/auth/pending-approval` — pending state)
- Session restore from localStorage on page refresh
- Admin login redirects to `/admin/overview`
- `ACCOUNT_PENDING` error code handled — shows correct message to pending instructors

### ✅ Problem Browsing
- Problem list loads from database with real difficulty/tags
- Difficulty displayed as `Easy/Medium/Hard` (not 0/1/2)
- Tags joined as `Arrays · Hash Map` for `topicLabel`
- Filter by difficulty (sent as number), tag (sent as string), search
- Problem detail page loads by route param ID
- Dynamic title, description, examples, constraints from backend
- Hardcoded starter code (backend does not provide it yet)

### ✅ Admin Panel (All 9 Endpoints)
- Overview dashboard: totalUsers, totalProblems, pendingInstructors, submissionsToday
- Users list: paginated, sortable, filterable by role/status/search
- User detail: full profile with avgScore, streak, recentSubmissions
- User status toggle: activate / set-pending (403 blocked for admin accounts)
- Problems list: includes inactive, sortable, filterable
- Problem create: POST with validation (min 3-char title, min 50-char statement, ≥1 tag)
- Problem edit: PATCH (partial update; sampleTestCases/tags fully replaced if provided)
- Problem status toggle: isActive flip
- Problem delete: soft delete (removed from all GET responses permanently)

### ✅ Analytics / Student Progress
- Student dashboard and profile: totalSolved, successRate, streak, topicStats, activityGrid
- Instructor overview: totalStudentsReached, integrityFlagsCount, dailyActivity, topicPerformance
- Integrity flags: confidence mapped to severity (high/medium/low)
- Instructor student detail: topicStats, recentAccepted, streak

### ✅ Avatar Upload (Cloudinary — Client-Side Only)
- Photo uploads to Cloudinary during registration
- URL stored in `localStorage['codify_avatar_<userId>']`
- Restored on every login using userId from API
- Shown in navbar and profile page
- `PUT /api/auth/avatar` called fire-and-forget (saves to DB when backend builds it)

---

## 3. What Backend Still Needs to Build

These are the remaining backend items the frontend is waiting on. Ordered by priority.

### 🔴 HIGH PRIORITY

#### 3.1 — Judge0 Configuration (Code Execution & Submissions)
**Affects:** Run code, Submit code, Poll for verdict
**Endpoints:**
- `POST /api/execution/run`
- `POST /api/submissions`
- `GET /api/submissions/:id`

**Status on frontend:** HTTP calls written and wired for Python and C#. The backend endpoints exist but need Judge0 running. Once Judge0 is configured and these endpoints respond correctly, the frontend works automatically.

**Frontend contract:**
- `run` → returns `{ stdout, stderr, executionTimeMs, status, testResults[] }`
- `submit` → returns `202` with `{ submissionId, status: "Pending" }`, then poll
- `GET submissions/:id` → returns full `SubmissionDetailResponse` with final status
- Poll stops when status leaves `Pending` or `Running`
- Extra status from backend not in original spec: `MemoryLimitExceeded` — already handled

---

#### 3.2 — AI Hints (OpenAI Key)
**Affects:** Hint button on the problem page
**Endpoint:** `POST /api/ai/hints`

**Status on frontend:** Service is fully wired. Just needs OpenAI API key configured on the backend.

**Frontend contract:**
```json
Request: { problemId, studentCode, hintLevel (1-3), previousHints[], attemptCount, lastSubmissionStatus }
Response: { hintText, hintLevel, followUpQuestion?, hasMoreHints }
```

---

#### 3.3 — Avatar URL in Login Response
**Affects:** Profile photo persistence across devices
**Endpoint:** `POST /api/auth/login` (modify response)

**What to add to login response:**
```json
{
  "data": {
    "token": "...",
    "user": {
      "userId": "...",
      "fullName": "...",
      "role": 0,
      "avatarUrl": "https://res.cloudinary.com/..."  ← ADD THIS
    }
  }
}
```

**What to add to the DB:**
```sql
ALTER TABLE Users ADD AvatarUrl NVARCHAR(500) NULL;
```

**New endpoint needed:** `PUT /api/auth/avatar`
```json
Request body: { "avatarUrl": "https://res.cloudinary.com/..." }
Auth: JWT Bearer required
Response: 200 OK (body ignored)
Validation: non-empty string, max 500 chars, must start with https://res.cloudinary.com/
```

---

### 🟡 MEDIUM PRIORITY

#### 3.4 — Contests (All Endpoints)
**Affects:** Instructor contest management, student contests view
**Endpoints needed:**
- `GET /api/contests` — all contests for authenticated instructor
- `GET /api/contests/:id` — single contest
- `POST /api/contests` — create (title, description, problemIds[], assignedStudentIds[], startAt, endAt)
- `GET /api/contests/:id/results` — leaderboard sorted by rank
- `GET /api/contests/my-contests` — student's enrolled contests (live, upcoming, past)

**Status on frontend:** ContestService HTTP calls are written. Falls back gracefully to in-memory mock if endpoints return 404. Once endpoints exist, frontend works automatically.

**Contest status enum:**
```
0 or "Draft"    → 'draft'
1 or "Upcoming" → 'upcoming'
2 or "Live"     → 'live'
3 or "Ended"    → 'ended'
```

**Ranking rules for results:** Primary: score descending. Secondary: problemsSolved descending. Tertiary: finishedAt ascending (earlier finish wins).

---

#### 3.5 — AI Submission Feedback
**Affects:** Code feedback panel after submission
**Endpoint:** `GET /api/submissions/:id/feedback`

**Frontend contract:**
```json
{
  "data": {
    "submissionId": "uuid",
    "overallScore": 72,
    "summary": "Your solution works but has room for improvement.",
    "feedbackItems": [
      {
        "id": "f1",
        "type": "quality | optimization | anomaly",
        "title": "...",
        "description": "...",
        "lineStart": null,
        "lineEnd": null,
        "severity": "low | medium | high"
      }
    ]
  }
}
```

**Status on frontend:** Service is wired. Currently falls back to mock data since endpoint returns error. Just needs endpoint to exist.

---

#### 3.6 — Profile Update Persistence
**Affects:** Edit Profile page (headline, bio, organization, social links)
**Endpoint:** `PUT /api/auth/profile`

**Frontend contract:**
```json
Request body: { fullName, headline, bio, organization, social: { linkedin, github, twitter } }
Auth: JWT Bearer
Response: 200 OK
```

**Status on frontend:** Service makes the call. Graceful fallback — local update sticks even if endpoint returns 404/501.

---

### 🟢 LOWER PRIORITY

#### 3.7 — Forgot Password
**Affects:** `/auth/forgot-password` page
**Endpoint:** `POST /api/auth/forgot-password`
```json
Request: { "email": "user@example.com" }
Response 200: confirmation message
```
**Status on frontend:** Form exists, mocked. Do not wire until endpoint is built.

---

#### 3.8 — Recommended Problems
**Affects:** Home page preview and student dashboard recommendations
**Endpoint:** `GET /api/problems/recommended`

**Status on frontend:** Currently calls `getAll()` and takes first 3. Not a real recommendation engine. Backend can build a personalized recommendation endpoint when ready.

---

#### 3.9 — `GET /api/progress/student` and `GET /api/progress/class`
**Note:** These original spec endpoints are no longer needed. The analytics endpoints (`/api/analytics/profile` and `/api/analytics/overview`) replaced them entirely and are already live. The `ProgressService` is still mocked with hardcoded data for the instructor class-view charts — this should be replaced with `/api/analytics/overview` data when the instructor wiring is complete.

---

## 4. Warnings & Known Issues

These are real problems in the codebase that both teams should be aware of.

---

### ⚠️ WARNING 1 — `ProblemService.update()` Uses PUT Instead of PATCH

**File:** `src/app/core/services/problem.service.ts`
**Line:** `update(id, payload)` method uses `this.http.put(...)`

**Problem:** The backend changed this endpoint from `PUT` to `PATCH` (documented in `ADMIN_PAGE_SPEC.md`). `AdminService.updateProblem()` correctly uses `PATCH`. But `ProblemService.update()` still uses `PUT`. The admin problem form uses `AdminService`, so this is not currently breaking anything. However, if any other part of the app calls `ProblemService.update()` directly, it will get a 404 or 405.

**Fix:** Change `this.http.put(...)` to `this.http.patch(...)` in `ProblemService.update()`.

---

### ⚠️ WARNING 2 — `ProgressService` Has a Hardcoded Date

**File:** `src/app/core/services/progress.service.ts`
**Line:** `const today = new Date('2026-07-23T00:00:00Z');`

**Problem:** The instructor class activity trend chart always shows data anchored to July 23, 2026. Today is August 17, 2026 — chart labels are 25 days in the past.

**Fix:** Change to `const today = new Date();`

**Note:** `ProgressService` is fully mocked and not connected to any backend endpoint. The instructor overview component (`instructor-overview.component.ts`) fetches real data from `InstructorService.getOverview$()` and does NOT use `ProgressService`. The stale date only affects a direct consumer of `ProgressService.getClassActivityTrend()` — check if anything still calls that method.

---

### ⚠️ WARNING 3 — `instructorGuard` Double-Redirect Chain

**File:** `src/app/core/guards/instructor.guard.ts`

**Problem:** When a non-instructor hits `/instructor/**`, the guard redirects to `/dashboard`. But `/dashboard` is itself a redirect — it resolves to `/profile/:username`. This means two redirects happen instead of one.

**Fix:** Change `router.navigate(['/dashboard'])` to `router.navigate(['/'])`.

---

### ⚠️ WARNING 4 — `DailyActivity` Interface Name Collision

**Files:**
- `src/app/core/models/analytics.model.ts` → `DailyActivity { date, submitted: boolean }`
- `src/app/core/models/progress.model.ts` → `DailyActivity { date, dayLabel, submissions: number }`

**Problem:** Two interfaces with the same name and different shapes. Currently no runtime bug because imports are explicit, but a wrong import would cause silent type errors.

**Fix:** Rename one of them — e.g., `StreakDay` in `analytics.model.ts` and `ClassActivityDay` in `progress.model.ts`.

---

### ⚠️ WARNING 5 — Topic Slug `as any` Cast

**File:** `src/app/core/services/problem.service.ts`
**Lines:** Both `mapProblemSummary()` and `mapProblemDetail()`

**Problem:** Backend tags are free-form strings. The frontend `Topic` type is a strict union (`'arrays' | 'graphs' | ...`). Tags like `"Binary Search Tree"` become slug `binary-search-tree` which doesn't match any union value, so topic filter pills won't highlight for such problems.

**Fix:** Either widen `Topic` to `string`, or build a normalization map that translates backend tags to the correct union values. Low urgency until instructor features are wired.

---

### ⚠️ WARNING 6 — `solvedCount` is Always 0 in Problem List

**File:** `src/app/core/services/problem.service.ts` → `mapProblemSummary()`

**Problem:** The `GET /api/problems` list response does not include `acceptedSubmissionsCount`. The frontend hardcodes `solvedCount: 0` for all problems in the list view. The detail view shows the real count.

**Fix options:**
- Backend adds `acceptedSubmissionsCount` to the list response (recommended)
- OR frontend hides the Solves column from the problem list until detail is loaded

---

### ⚠️ WARNING 7 — Sync Fallback Methods Will Grow Stale

**File:** `src/app/core/services/problem.service.ts`
**Methods:** `getAllSync()`, `searchSync()`, `getRecommendedSync()`

**Problem:** These return a hardcoded mock array of 9 problems. Three instructor components use them:
- `instructor-contest-create.component.ts` → `searchSync()`
- `instructor-contest-detail.component.ts` → `getAllSync()`
- `src/app/features/home/components/student-dashboard-preview/student-dashboard-preview.component.ts` → `getRecommendedSync()`

As the real database grows, these mocked components will show outdated, wrong problem data. These methods must be removed when the instructor endpoints are wired.

---

### ⚠️ WARNING 8 — Avatar Persistence is Device-Specific Until Backend Builds the Endpoint

**Context:** Profile photos upload to Cloudinary. The URL is stored in `localStorage['codify_avatar_<userId>']` as a fallback. The `PUT /api/auth/avatar` endpoint is not built yet.

**Impact:** A user who uploads a photo on device A will see their photo on device A only. On device B, they see only their initials. Once the backend builds `PUT /api/auth/avatar` and adds `avatarUrl` to the login response, the photo will appear on all devices automatically — no frontend change needed.

---

### ℹ️ INFO — Sass Deprecation Warnings (Non-Breaking)

**Files:** auth scss files + `problem-page.component.scss`

`@import` and `darken()` are deprecated in Dart Sass 3.0. App builds and runs fine. These will become errors in a future Sass version. Low urgency.

---

## 5. Mocked Features — Frontend Ready, Backend Pending

These features have complete, working UI with mock data. They are waiting on backend endpoints only. No frontend changes needed when backend delivers these.

| Feature | Frontend Status | Waiting On |
|---------|-----------------|------------|
| Run code (Python/C#) | ✅ Wired, needs Judge0 | Judge0 config |
| Submit code (Python/C#) | ✅ Wired, needs Judge0 | Judge0 config |
| AI Hints (all levels) | ✅ Wired, needs OpenAI | OpenAI key config |
| AI Submission Feedback | ✅ Wired, currently mocked | Backend to build endpoint |
| Contest list (instructor) | HTTP calls written, graceful fallback | Backend to build endpoints |
| Contest create | HTTP calls written, graceful fallback | Backend to build endpoint |
| Contest detail + leaderboard | HTTP calls written, graceful fallback | Backend to build endpoint |
| Student contests view | HTTP calls written, graceful fallback | Backend to build endpoint |
| Avatar cross-device | Wired fire-and-forget | Backend to build PUT /api/auth/avatar |
| Profile update persistence | Wired, graceful fallback | Backend to build PUT /api/auth/profile |
| Forgot password | Form exists, mocked | Backend to build POST /api/auth/forgot-password |

---

## 6. Field Contracts & Enum Mappings

### Enums (Backend integer → Frontend string)

| Field | 0 | 1 | 2 |
|-------|---|---|---|
| `role` | `student` | `instructor` | `admin` |
| `difficulty` | `easy` | `medium` | `hard` |
| `language` | `Python` | `CSharp` | — |

**Contest status (string or number):**

| Value | Frontend |
|-------|----------|
| `0` or `"Draft"` | `draft` |
| `1` or `"Upcoming"` | `upcoming` |
| `2` or `"Live"` | `live` |
| `3` or `"Ended"` | `ended` |

**Submission status (string — never a number):**
`Pending` | `Running` | `Accepted` | `WrongAnswer` | `RuntimeError` | `TimeLimitExceeded` | `CompileError` | `MemoryLimitExceeded`

Poll stops when status is anything other than `Pending` or `Running`.

---

### Response Envelope

All responses use `{ success: boolean, data: {...} }` for admin endpoints. Non-admin endpoints use `{ data: {...} }`. The `AdminService` maps `r.data`; `ProblemService` and `AuthService` also map `r.data`.

**Error shape (admin endpoints):**
```json
{ "success": false, "errorCode": "NOT_FOUND", "message": "Human-readable description" }
```

**Error codes the frontend handles:**
- `NOT_FOUND` (404)
- `FORBIDDEN` (403) — e.g., trying to modify an admin account
- `CONFLICT` (409) — e.g., duplicate problem title
- `VALIDATION_ERROR` (400)
- `ACCOUNT_PENDING` (403) — login attempt by a pending instructor

---

### Key Field Name Differences (Backend → Frontend Mapping)

| Backend field | Frontend field | Where applied |
|---|---|---|
| `userId` | `id` | Login response, admin users |
| `fullName` | `name` | Login response, admin users |
| `statement` | `description` | Problem detail |
| `sampleTestCases[].expectedOutput` | `examples[].output` | Problem detail |
| `acceptedSubmissionsCount` | `solvedCount` | Problem detail |
| `constraints` (string) | `constraints` (array) | Problem detail — split on `\n` |
| `tags` (array) | `topic` (slug) + `topicLabel` (joined) | All problem responses |

---

## 7. Infrastructure Requirements

These external services must be running for the following features to work:

| Service | Feature | Credentials |
|---------|---------|-------------|
| **Judge0** | Run code, Submit code, Poll verdict | Must be running locally or configured |
| **OpenAI API** | AI Hints (`POST /api/ai/hints`) | `OPENAI_API_KEY` env variable |
| **Cloudinary** | Profile photo upload | Cloud name: `mg7dsqv2`, Upload preset: `MS_codify-imgs` |

---

## 8. Quick Test Credentials

### Admin
```
Email:    admin@codify.com
Password: Admin@123456
Role:     admin (role 2) — auto-seeded on backend startup
```

### Student (seeded)
```
Email:    student@codify.com
Password: 123456
```

### Instructor (seeded)
```
Email:    instructor@codify.com
Password: 123456
```

### Register a new account
Any email/password works. Password minimum: 8 characters.
- Student → auto-login → redirect to `/problems`
- Instructor → redirect to `/auth/pending-approval`

---

### Base URLs
```
Frontend:  http://localhost:4200
Backend:   http://localhost:5237
Swagger:   http://localhost:5237/swagger
```

---

*Frontend authored by: Kiro*
*Last updated: August 17, 2026*
