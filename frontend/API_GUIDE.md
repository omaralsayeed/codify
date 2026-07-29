# Codify — Backend API Guide

> Generated from a full frontend codebase scan.  
> Angular version: **21.2.0** | Backend stack: **ASP.NET Core** | Base URL: `http://localhost:5237/api`

---

## Summary

**Total Endpoints:** 20  
**Real Endpoints:** 0  
**Mocked Endpoints:** 20  

**Feature Areas Fully Mocked (Awaiting Backend):**
- Authentication (3 endpoints)
- Problems (3 endpoints)
- Code Execution & Submissions (3 endpoints)
- AI Hints (1 endpoint)
- AI Feedback (1 endpoint — endpoint does not exist yet)
- Analytics & Progress (7 endpoints)
- Public Profile (1 endpoint)

**Deprecated (Frontend No Longer Uses):**
- `GET /api/progress/student` — replaced by `GET /api/analytics/progress`
- `GET /api/progress/class` — not yet used by any component

---

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Global Conventions](#2-global-conventions)
3. [Authentication](#3-authentication)
4. [Endpoints Required](#4-endpoints-required)
   - [4.1 Auth](#41-auth)
   - [4.2 Problems](#42-problems)
   - [4.3 Code Execution (Run)](#43-code-execution-run)
   - [4.4 Submissions](#44-submissions)
   - [4.5 AI Hints](#45-ai-hints)
   - [4.6 AI Feedback](#46-ai-feedback)
   - [4.7 Analytics](#47-analytics)
   - [4.8 Public Profile](#48-public-profile)
5. [Data Models (TypeScript → C# DTO mapping)](#5-data-models)
6. [Current Implementation Status](#6-current-implementation-status)
7. [Priority Order for Backend Delivery](#7-priority-order-for-backend-delivery)
8. [Frontend Notes & Gotchas](#8-frontend-notes--gotchas)

---

## 1. Project Overview

**Codify** is a coding-challenge learning platform. Two roles exist: **student** and **instructor**.

Key features:
- Browse a problem list (with topic and difficulty filters)
- Solve problems in an in-browser code editor (Python, C#, JavaScript, Java, C++)
- Run code against sample test cases, submit for full judging
- Get progressive AI hints (up to 3 levels per problem)
- View AI code-quality feedback after submission
- Student dashboard (progress, streaks, topic mastery)
- Instructor dashboard (class overview, integrity flags) — *planned*

Frontend routing summary:

| Route | Guard | Component |
|---|---|---|
| `/` | public | HomeComponent |
| `/auth/login` | guestGuard | LoginComponent |
| `/auth/register` | guestGuard | RegisterComponent |
| `/auth/forgot-password` | guestGuard | ForgotPasswordComponent |
| `/problems` | authGuard | ProblemListComponent |
| `/problems/:id` | authGuard | ProblemPageComponent |
| `/progress` | authGuard | StudentProgressComponent |
| `/profile/:username` | public | ProfileComponent |
| `/dashboard` | — | Redirects to `/profile/:username` (logged-in user) |

---

## 2. Global Conventions

### Base URL
```
http://localhost:5237/api
```

### Response Envelope
**Every response must be wrapped in a `data` envelope:**
```json
{
  "data": { ... }
}
```
The Angular `HttpClient` unwraps this via `.pipe(map(r => r.data))` in every service.

### JSON Casing
- **Backend (C#):** PascalCase property names (default ASP.NET Core serializer)
- **Frontend (Angular):** camelCase property names (auto-converted by `HttpClient`)

Example: C# `SubmissionId` → Angular receives it as `submissionId`.

### HTTP Status Codes Expected by Frontend

| Status | Meaning |
|---|---|
| `200 OK` | Standard successful response |
| `201 Created` | Resource created |
| `202 Accepted` | Submission accepted, processing async |
| `400 Bad Request` | Validation failure |
| `401 Unauthorized` | Missing or invalid token |
| `403 Forbidden` | Authenticated but not authorized |
| `404 Not Found` | Resource doesn't exist |
| `5xx` | Server error |

### Error Response Shape
The frontend `ServiceError` interface expects:
```json
{
  "message": "Human-readable error message"
}
```

---

## 3. Authentication

### Current Status: ⚠️ FULLY MOCKED

The `AuthService` is 100% in-memory. No HTTP calls are made. All auth must be implemented from scratch.

### How the frontend expects auth to work

1. User POSTs credentials → backend returns a JWT
2. JWT is stored in `localStorage` under key `codify_token`
3. User object (without password) is stored in `localStorage` under key `codify_user`
4. All subsequent API requests include the header:
   ```
   Authorization: Bearer <jwt_token>
   ```
5. The `authGuard` checks `AuthService.isLoggedIn()` (signal-based, checks for stored user)

> **Note:** Auth header is currently added manually in each service via a `headers()` helper method. The team plans to migrate this to an `HttpInterceptor` once real auth is wired.

### 3.1 Login

```
POST /api/auth/login
```

**Request body:**
```json
{
  "email": "student@codify.com",
  "password": "123456"
}
```

**Response `200`:**
```json
{
  "data": {
    "token": "eyJhbGci...",
    "user": {
      "id": "uuid",
      "name": "Test Student",
      "email": "student@codify.com",
      "role": "student",
      "avatarInitials": "TS",
      "streak": 12
    }
  }
}
```

**Response `401`:**
```json
{ "message": "Invalid email or password" }
```

### 3.2 Register

```
POST /api/auth/register
```

**Request body:**
```json
{
  "fullName": "Jane Doe",
  "email": "jane@example.com",
  "password": "securepass",
  "role": "student",
  "organization": "Cairo University",
  "phoneNumber": "01012345678",
  "country": "Egypt",
  "city": "Cairo"
}
```

> `organization`, `phoneNumber`, `country`, `city` are collected on the register form. Backend should store them even if not all are required.

**Response `201`:**
```json
{
  "data": {
    "token": "eyJhbGci...",
    "user": {
      "id": "uuid",
      "name": "Jane Doe",
      "email": "jane@example.com",
      "role": "student",
      "avatarInitials": "JD",
      "streak": 0
    }
  }
}
```

### 3.3 Forgot Password

```
POST /api/auth/forgot-password
```

**Request body:**
```json
{ "email": "jane@example.com" }
```

**Response `200`:** Confirmation that reset email was sent (frontend shows a success message, doesn't need token).

---

## 4. Endpoints Required

---

### 4.1 Auth

| Method | Endpoint | Status |
|---|---|---|
| POST | `/api/auth/login` | ⚠️ Mocked |
| POST | `/api/auth/register` | ⚠️ Mocked |
| POST | `/api/auth/forgot-password` | ⚠️ Mocked |

---

### 4.2 Problems

| Method | Endpoint | Status |
|---|---|---|
| GET | `/api/problems` | ⚠️ Mocked (hardcoded array) |
| GET | `/api/problems/:id` | ⚠️ Not implemented |
| GET | `/api/problems/recommended` | ⚠️ Mocked (first 3 items) |

#### GET `/api/problems`

Returns the full list of problems. Supports filtering via query params (frontend filters client-side currently, but these should move to the backend).

**Query params (future):** `?topic=arrays&difficulty=easy`

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Two Sum",
      "difficulty": "easy",
      "topic": "arrays",
      "topicLabel": "Arrays · Hash Map",
      "solvedCount": 36045
    }
  ]
}
```

**Difficulty values (enum):** `easy` | `medium` | `hard`

**Topic values (enum):**
`dynamic-programming` | `graphs` | `recursion` | `greedy` | `arrays` | `sorting` | `binary-search` | `trees`

#### GET `/api/problems/:id`

Returns a single problem with full details (description, constraints, examples, starter code per language).

**Response `200`:**
```json
{
  "data": {
    "id": "uuid",
    "title": "Two Sum",
    "difficulty": "easy",
    "topic": "arrays",
    "topicLabel": "Arrays · Hash Map",
    "solvedCount": 36045,
    "description": "Given an array of integers nums...",
    "constraints": ["2 <= nums.length <= 10^4", ...],
    "examples": [
      { "input": "nums = [2,7,11,15], target = 9", "output": "[0,1]", "explanation": "..." }
    ],
    "starterCode": {
      "python": "def twoSum(nums, target):\n    pass",
      "csharp": "public int[] TwoSum(int[] nums, int target) { }",
      "javascript": "var twoSum = function(nums, target) { };",
      "java": "public int[] twoSum(int[] nums, int target) { }",
      "cpp": "vector<int> twoSum(vector<int>& nums, int target) { }"
    }
  }
}
```

> **Note:** The current problem page is **hardcoded to "Two Sum"**. Once this endpoint exists, the component should load the problem by `params['id']` from the route.

#### GET `/api/problems/recommended`

Returns a short list (3 items) of recommended problems for the current user's dashboard.

---

### 4.3 Code Execution (Run)

| Method | Endpoint | Status |
|---|---|---|
| POST | `/api/execution/run` | ⚠️ Mocked (Python & C# wired, others mock) |

#### POST `/api/execution/run`

Runs code against the problem's **sample test cases only**. Does **not** create a submission record. Safe to call on every "Run" button click.

**Request body:**
```json
{
  "problemId": "00000000-0000-0000-0000-000000000005",
  "code": "def twoSum(nums, target):\n    seen = {}\n    for i, n in enumerate(nums):\n        if target - n in seen:\n            return [seen[target - n], i]\n        seen[n] = i",
  "language": "Python"
}
```

**Language enum values:** `Python` | `CSharp`
> JavaScript, Java, C++ are mocked on the frontend — no backend judge support needed for those yet.

**Response `200`:**
```json
{
  "data": {
    "stdout": "[0, 1]\n[1, 2]\n[0, 1]",
    "stderr": "",
    "executionTimeMs": 42,
    "status": "Accepted",
    "testResults": [
      {
        "input": "nums=[2,7,11,15], target=9",
        "expectedOutput": "[0,1]",
        "actualOutput": "[0,1]",
        "passed": true
      },
      {
        "input": "nums=[3,2,4], target=6",
        "expectedOutput": "[1,2]",
        "actualOutput": "[1,2]",
        "passed": true
      },
      {
        "input": "nums=[3,3], target=6",
        "expectedOutput": "[0,1]",
        "actualOutput": "[0,1]",
        "passed": true
      }
    ]
  }
}
```

---

### 4.4 Submissions

| Method | Endpoint | Status |
|---|---|---|
| POST | `/api/submissions` | ⚠️ Mocked |
| GET | `/api/submissions/:id` | ⚠️ Mocked |

#### POST `/api/submissions`

Submits code for full judging against all test cases. Returns `202 Accepted` immediately with a `Pending` submission. Frontend polls `GET /api/submissions/:id` every **1500ms** until status is no longer `Pending` or `Running`.

**Request body:**
```json
{
  "problemId": "00000000-0000-0000-0000-000000000005",
  "code": "def twoSum(nums, target):\n    ...",
  "language": "Python"
}
```

**Response `202`:**
```json
{
  "data": {
    "submissionId": "uuid",
    "problemId": "uuid",
    "userId": "uuid",
    "code": "...",
    "language": "Python",
    "status": "Pending",
    "submittedAt": "2025-01-01T12:00:00Z",
    "executionTimeMs": null,
    "memoryUsedKb": null,
    "passedTestCases": 0,
    "totalTestCases": 32,
    "score": null,
    "result": null,
    "aiFeedback": []
  }
}
```

#### GET `/api/submissions/:id`

Returns the current state of a submission. Called repeatedly by the frontend until `status` exits `Pending`/`Running`.

**Response `200` (final, accepted):**
```json
{
  "data": {
    "submissionId": "uuid",
    "problemId": "uuid",
    "userId": "uuid",
    "code": "...",
    "language": "Python",
    "status": "Accepted",
    "submittedAt": "2025-01-01T12:00:00Z",
    "executionTimeMs": 38,
    "memoryUsedKb": 14200,
    "passedTestCases": 32,
    "totalTestCases": 32,
    "score": 100,
    "result": {
      "passedTestCount": 32,
      "failedTestCount": 0,
      "totalTestCount": 32,
      "errorMessage": null,
      "outputSummary": "All test cases passed."
    },
    "aiFeedback": []
  }
}
```

**Status enum values:**

| Value | Meaning |
|---|---|
| `Pending` | Queued, not started |
| `Running` | Currently executing |
| `Accepted` | All test cases passed |
| `WrongAnswer` | Output doesn't match expected |
| `RuntimeError` | Code threw an exception |
| `TimeLimitExceeded` | Execution exceeded time limit |
| `CompileError` | Code failed to compile |

> `Pending` and `Running` = keep polling. All others = stop polling, show result.

---

### 4.5 AI Hints

| Method | Endpoint | Status |
|---|---|---|
| POST | `/api/ai/hints` | ⚠️ Mocked |

#### POST `/api/ai/hints`

Returns the next progressive hint for the student. Up to **3 levels** per problem per session. The frontend passes all previously received hint texts in `previousHints[]` so the AI doesn't repeat itself.

**Request body:**
```json
{
  "problemId": "uuid",
  "studentCode": "def twoSum(nums, target):\n    pass",
  "hintLevel": 1,
  "previousHints": [],
  "attemptCount": 2,
  "lastSubmissionStatus": "WrongAnswer"
}
```

| Field | Type | Required | Notes |
|---|---|---|---|
| `problemId` | string (GUID) | ✅ | |
| `studentCode` | string | ✅ | Current code in the editor |
| `hintLevel` | 1 \| 2 \| 3 | ✅ | Increments per call |
| `previousHints` | string[] | ✅ | `hintText` from prior responses |
| `attemptCount` | number | optional | Number of submissions made |
| `lastSubmissionStatus` | SubmissionStatus | optional | Last verdict |

**Response `200`:**
```json
{
  "data": {
    "hintText": "Think about what data structure lets you look up a value in O(1)...",
    "hintLevel": 1,
    "followUpQuestion": "What would you store as the key and value?",
    "hasMoreHints": true
  }
}
```

| Field | Notes |
|---|---|
| `hintText` | Narrative hint shown to student |
| `hintLevel` | Echo of requested level |
| `followUpQuestion` | Optional follow-up prompt. `null` / omit on level 3 |
| `hasMoreHints` | `false` when at max level (3) or no more hints |

> **Hint usage is tracked.** The `ProgressService` exposes `hintsUsedToday` and `hintsLimit` (currently mocked as 3/5). The backend should enforce a daily hint budget per student.

---

### 4.6 AI Feedback

| Method | Endpoint | Status |
|---|---|---|
| GET | `/api/submissions/:id/feedback` | ❌ Does not exist yet |

This endpoint is **not yet implemented in the backend**. The frontend has a full UI for it (filter pills, severity badges, score counter animation) but falls back to a hardcoded mock.

#### GET `/api/submissions/:id/feedback`

Returns AI-generated code quality feedback for a completed submission.

**Response `200`:**
```json
{
  "data": {
    "submissionId": "uuid",
    "overallScore": 72,
    "summary": "Your solution works but has room for efficiency improvements.",
    "feedbackItems": [
      {
        "id": "f1",
        "type": "quality",
        "title": "Good variable naming",
        "description": "Variable names are clear and follow conventions.",
        "lineStart": null,
        "lineEnd": null,
        "severity": "low"
      },
      {
        "id": "f2",
        "type": "optimization",
        "title": "Nested loop detected",
        "description": "The nested loop results in O(n²) time complexity. Consider a hash map.",
        "lineStart": 3,
        "lineEnd": 6,
        "severity": "high"
      },
      {
        "id": "f3",
        "type": "anomaly",
        "title": "Unused variable",
        "description": "Variable 'temp' is declared but never used.",
        "lineStart": 2,
        "lineEnd": 2,
        "severity": "medium"
      }
    ]
  }
}
```

**`type` enum:** `quality` | `optimization` | `anomaly`

**`severity` enum:** `low` | `medium` | `high`

| Field | Notes |
|---|---|
| `overallScore` | 0–100 integer. Drives the animated score counter in the UI |
| `summary` | One-sentence overall assessment |
| `lineStart` / `lineEnd` | Line numbers in student code. `null` = general feedback |

---

### 4.7 Analytics

> All endpoints in this section are called by `AnalyticsService`. All are currently mocked with `delay(1200)` to simulate network latency. The HTTP calls are written and commented out in the service — only the backend endpoints are missing.

| Method | Endpoint | Status | Caller |
|---|---|---|---|
| GET | `/api/analytics/progress` | ⚠️ Mocked | `StudentProgressComponent` |
| GET | `/api/analytics/dashboard` | ⚠️ Mocked | legacy (kept for compatibility) |
| GET | `/api/analytics/summary` | ⚠️ Mocked | legacy (kept for compatibility) |
| GET | `/api/analytics/topics` | ⚠️ Mocked | legacy (kept for compatibility) |
| GET | `/api/analytics/activity` | ⚠️ Mocked | legacy (kept for compatibility) |
| GET | `/api/analytics/scores` | ⚠️ Mocked | legacy (kept for compatibility) |
| GET | `/api/analytics/recommendations` | ⚠️ Mocked | legacy (kept for compatibility) |

---

#### GET `/api/analytics/progress`

The primary endpoint for the **Student Progress page** (`/progress`). Returns the full `StudentAnalytics` payload in one call — summary stats, topic performance with AI insights, difficulty breakdown, success rate history, recent submissions, recommendations, and hint usage.

**Auth:** Required (`Authorization: Bearer <token>`)

**Response `200`:**
```json
{
  "data": {
    "summary": {
      "studentName": "Mohamed",
      "totalAttempted": 47,
      "totalSolved": 31,
      "successRate": 66,
      "streak": {
        "currentStreak": 7,
        "longestStreak": 15,
        "lastSevenDays": [
          { "date": "2026-07-23", "submitted": true },
          { "date": "2026-07-24", "submitted": true },
          { "date": "2026-07-25", "submitted": false },
          { "date": "2026-07-26", "submitted": true },
          { "date": "2026-07-27", "submitted": true },
          { "date": "2026-07-28", "submitted": true },
          { "date": "2026-07-29", "submitted": true }
        ]
      }
    },
    "topics": [
      {
        "topicId": "t1",
        "topicName": "Arrays",
        "attempted": 12,
        "solved": 10,
        "strengthScore": 83,
        "strength": "strong",
        "aiInsight": null
      },
      {
        "topicId": "t5",
        "topicName": "Dynamic Programming",
        "attempted": 7,
        "solved": 2,
        "strengthScore": 28,
        "strength": "weak",
        "aiInsight": "You tend to miss the memoization step — try identifying overlapping subproblems first."
      }
    ],
    "difficultyBreakdown": { "easy": 18, "medium": 10, "hard": 3 },
    "successRateHistory": [
      { "label": "Mon", "successRate": 50, "solved": 2 },
      { "label": "Tue", "successRate": 66, "solved": 4 }
    ],
    "recentSubmissions": [
      {
        "submissionId": "uuid",
        "problemId": "uuid",
        "problemTitle": "Two Sum",
        "difficulty": "Easy",
        "status": "Accepted",
        "language": "Python",
        "submittedAt": "2026-07-29T10:30:00Z"
      }
    ],
    "recommendations": [
      {
        "problemId": "r1",
        "title": "Coin Change",
        "difficulty": "Medium",
        "topic": "Dynamic Programming",
        "reason": "Based on your weakness in Dynamic Programming"
      }
    ],
    "hintUsage": {
      "totalHintsUsed": 14,
      "averageHintsPerProblem": 0.9,
      "solvedWithZeroHints": 18,
      "solvedUsingAllHints": 3
    }
  }
}
```

**Key field notes:**

| Field | Type | Notes |
|---|---|---|
| `summary.streak.lastSevenDays` | `DailyActivity[7]` | Always exactly 7 items, oldest first. Drives the 7-dot streak row in the UI. |
| `topics[].strength` | `'strong' \| 'average' \| 'weak'` | `strengthScore` ≥ 75 = strong, 40–74 = average, < 40 = weak |
| `topics[].aiInsight` | `string \| null` | Only provided for weak topics. Displayed in the "Focus Areas" cards. |
| `successRateHistory[].label` | `string` | Day-of-week label, e.g. `"Mon"`. Drives the success rate chart. |
| `recentSubmissions[].status` | `string` | `"Accepted"` \| `"Wrong Answer"` \| `"Runtime Error"` \| `"Time Limit Exceeded"` |
| `recentSubmissions[].difficulty` | `string` | Title-cased: `"Easy"` \| `"Medium"` \| `"Hard"` |

---

#### GET `/api/analytics/dashboard`

Returns the full legacy dashboard payload for a student in a single call. Currently not consumed by any active component but the service method remains for compatibility.

**Auth:** Required

**Response `200`:**
```json
{
  "data": {
    "summary": {
      "problemsSolved": 47,
      "avgScore": 68,
      "streak": 12,
      "totalAttempts": 83,
      "acceptanceRate": 57,
      "hintsUsedToday": 3,
      "hintsLimit": 5
    },
    "topicStats": [
      { "topic": "Arrays", "percentage": 85, "trend": "up" },
      { "topic": "Graphs", "percentage": 38, "trend": "down" }
    ],
    "weeklyActivity": [
      { "date": "2026-07-01", "solved": 3, "attempted": 4 }
    ],
    "scoreHistory": [
      { "date": "2026-06-29", "score": 61 }
    ],
    "recommendations": [
      {
        "id": "uuid",
        "title": "Number of Islands",
        "difficulty": "hard",
        "topic": "graphs",
        "topicLabel": "Graphs · BFS",
        "reason": "Weak area: Graphs (38%)",
        "estimatedMinutes": 35
      }
    ]
  }
}
```

---

#### GET `/api/analytics/summary`

Returns only the headline summary stats. Subset of the `/analytics/dashboard` response.

**Auth:** Required

**Response `200`:**
```json
{
  "data": {
    "problemsSolved": 47,
    "avgScore": 68,
    "streak": 12,
    "totalAttempts": 83,
    "acceptanceRate": 57,
    "hintsUsedToday": 3,
    "hintsLimit": 5
  }
}
```

---

#### GET `/api/analytics/topics`

Returns topic mastery stats with trend indicators.

**Auth:** Required

**Response `200`:**
```json
{
  "data": [
    { "topic": "Arrays",           "percentage": 85, "trend": "up"   },
    { "topic": "Recursion",        "percentage": 72, "trend": "up"   },
    { "topic": "Dyn. Programming", "percentage": 54, "trend": "flat" },
    { "topic": "Graphs",           "percentage": 38, "trend": "down" }
  ]
}
```

**`trend` enum:** `up` | `down` | `flat`

---

#### GET `/api/analytics/activity`

Returns daily activity for the last **28 days** (4 weeks).

**Auth:** Required

**Response `200`:**
```json
{
  "data": [
    { "date": "2026-07-01", "solved": 3, "attempted": 4 },
    { "date": "2026-07-02", "solved": 0, "attempted": 1 }
  ]
}
```

Array length: always **28 items**, oldest first.

---

#### GET `/api/analytics/scores`

Returns score history for the rolling **30-day** trend chart.

**Auth:** Required

**Response `200`:**
```json
{
  "data": [
    { "date": "2026-06-29", "score": 61 },
    { "date": "2026-06-30", "score": 68 }
  ]
}
```

Array length: always **30 items**, oldest first. `score` is 0–100.

---

#### GET `/api/analytics/recommendations`

Returns personalized problem recommendations ordered by priority (weakest topics first).

**Auth:** Required

**Response `200`:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Number of Islands",
      "difficulty": "hard",
      "topic": "graphs",
      "topicLabel": "Graphs · BFS",
      "reason": "Weak area: Graphs (38%)",
      "estimatedMinutes": 35
    }
  ]
}
```

---

### 4.8 Public Profile

| Method | Endpoint | Status | Caller |
|---|---|---|---|
| GET | `/api/profile/:username` | ⚠️ Mocked | `ProfileComponent` |

#### GET `/api/profile/:username`

Returns a public profile for any user identified by their username slug. **No authentication required** — this endpoint is intentionally public.

Called by `ProfileComponent` at route `/profile/:username`. The `:username` parameter is a URL-safe lowercase slug (e.g., `test_student` from name "Test Student").

**Auth:** None

**Response `200`:**
```json
{
  "data": {
    "user": {
      "username": "test_student",
      "name": "Test Student",
      "avatarInitials": "TS",
      "role": "student",
      "joinedAt": "2023-01-15T00:00:00.000Z",
      "headline": "SWE | 3 yrs exp · Open to opportunities",
      "bio": "Passionate about algorithms and clean code.",
      "social": {
        "linkedin": "https://linkedin.com/in/test-student",
        "github":   "https://github.com/test-student",
        "twitter":  "https://twitter.com/test_student"
      }
    },
    "totalSolved": 31,
    "totalAttempted": 47,
    "successRate": 66,
    "streak": {
      "currentStreak": 12,
      "longestStreak": 21,
      "totalActiveDays": 89,
      "totalSubmissionsLastYear": 143
    },
    "difficultyBreakdown": { "easy": 18, "medium": 10, "hard": 3 },
    "difficultyTotals":    { "easy": 955, "medium": 1813, "hard": 843 },
    "languageStats": [
      { "language": "Python",     "solved": 22 },
      { "language": "C#",         "solved": 7  },
      { "language": "JavaScript", "solved": 2  }
    ],
    "topicStats": [
      {
        "topicId": "t1",
        "topicName": "Arrays",
        "attempted": 12,
        "solved": 10,
        "strengthScore": 83,
        "strength": "strong",
        "aiInsight": null
      }
    ],
    "activityGrid": [
      { "date": "2025-07-29", "count": 0 },
      { "date": "2025-07-30", "count": 2 }
    ],
    "recentAccepted": [
      {
        "submissionId": "uuid",
        "problemId": "uuid",
        "problemTitle": "Two Sum",
        "difficulty": "Easy",
        "status": "Accepted",
        "language": "Python",
        "submittedAt": "2026-07-28T10:30:00Z"
      }
    ]
  }
}
```

**Key field notes:**

| Field | Type | Notes |
|---|---|---|
| `user.username` | `string` | URL-safe slug matching the `:username` route param |
| `user.headline` | `string?` | Optional one-liner shown under avatar |
| `user.bio` | `string?` | Optional short bio |
| `user.social.*` | `string?` | Full URLs; all fields optional |
| `difficultyTotals` | `object` | Platform-wide totals per difficulty, used to render the difficulty progress bars (e.g. "18 / 955 Easy") |
| `activityGrid` | `ActivityDay[365]` | Exactly **365 items**, oldest first. Drives the GitHub-style activity heatmap. `count = 0` means inactive. |
| `recentAccepted` | `RecentSubmission[]` | Last **10 Accepted-only** submissions. Non-accepted verdicts are excluded. |
| `topicStats[].strength` | `'strong' \| 'average' \| 'weak'` | Same calculation as the progress page |

> **Note:** The `/dashboard` route now redirects to `/profile/:username` for the logged-in user. The profile page doubles as the personal dashboard.

---

## 5. Data Models

Full TypeScript interface → C# DTO mapping for every model the frontend uses.

### User
```typescript
interface User {
  id: string;               // GUID
  name: string;
  email: string;
  role: 'student' | 'instructor';
  avatarInitials: string;   // e.g. "JD" — auto-generated from full name
  streak?: number;          // students only
  username?: string;        // URL-safe slug, e.g. "test_student"
  joinedAt?: string;        // ISO date string — populated on profile load
}
```

### Problem
```typescript
type Difficulty = 'easy' | 'medium' | 'hard';
type Topic = 'dynamic-programming' | 'graphs' | 'recursion' | 'greedy'
           | 'arrays' | 'sorting' | 'binary-search' | 'trees';

interface Problem {
  id: string;
  title: string;
  difficulty: Difficulty;
  topic: Topic;
  topicLabel: string;   // human-readable, e.g. "Arrays · Hash Map"
  solvedCount?: number;
}
```

### Submission (Request & Response)
```typescript
// POST body
interface CreateSubmissionRequest {
  problemId: string;
  code: string;
  language: 'Python' | 'CSharp';
}

// GET response (also returned immediately by POST as 202)
interface SubmissionDetailResponse {
  submissionId: string;
  problemId: string;
  userId: string;
  code: string;
  language: string;
  status: 'Pending' | 'Running' | 'Accepted' | 'WrongAnswer'
        | 'RuntimeError' | 'TimeLimitExceeded' | 'CompileError';
  submittedAt: string;           // ISO-8601
  executionTimeMs?: number;
  memoryUsedKb?: number;
  passedTestCases: number;
  totalTestCases: number;
  score?: number;
  result?: {
    passedTestCount: number;
    failedTestCount: number;
    totalTestCount: number;
    errorMessage?: string;
    outputSummary?: string;
  };
  aiFeedback: { type: string; message: string; }[];
}
```

### Run (Request & Response)
```typescript
interface RunCodeRequest {
  problemId: string;
  code: string;
  language: 'Python' | 'CSharp';
}

interface RunCodeResponse {
  stdout: string;
  stderr: string;
  executionTimeMs: number;
  status: string;
  testResults: {
    input: string;
    expectedOutput: string;
    actualOutput: string;
    passed: boolean;
  }[];
}
```

### Hint (Request & Response)
```typescript
interface HintRequest {
  problemId: string;
  studentCode: string;
  hintLevel: 1 | 2 | 3;
  previousHints: string[];
  attemptCount?: number;
  lastSubmissionStatus?: string;
}

interface HintResponse {
  hintText: string;
  hintLevel: number;
  followUpQuestion?: string;
  hasMoreHints: boolean;
}
```

### AI Feedback
```typescript
interface SubmissionFeedback {
  submissionId: string;
  overallScore: number;         // 0–100
  summary: string;
  feedbackItems: {
    id: string;
    type: 'quality' | 'optimization' | 'anomaly';
    title: string;
    description: string;
    lineStart: number | null;
    lineEnd: number | null;
    severity: 'low' | 'medium' | 'high';
  }[];
}
```

### Analytics — Student Progress (`GET /api/analytics/progress`)
```typescript
// Streak & activity
interface DailyActivity {
  date: string;        // 'YYYY-MM-DD'
  submitted: boolean;  // true if at least one submission that day
}

interface StreakData {
  currentStreak: number;          // consecutive days with ≥1 submission
  longestStreak: number;          // all-time best
  lastSevenDays: DailyActivity[]; // always 7 items, oldest first
}

// Hero summary
interface ProgressSummary {
  studentName: string;
  totalAttempted: number;
  totalSolved: number;
  successRate: number;   // 0–100
  streak: StreakData;
}

// Topic performance
type TopicStrength = 'strong' | 'average' | 'weak';

interface TopicPerformance {
  topicId: string;
  topicName: string;
  attempted: number;
  solved: number;
  strengthScore: number;     // 0–100 — thresholds: ≥75 strong, 40–74 average, <40 weak
  strength: TopicStrength;
  aiInsight: string | null;  // only populated for weak topics
}

// Difficulty distribution
interface DifficultyBreakdown {
  easy: number;
  medium: number;
  hard: number;
}

// Success rate chart data
interface SuccessRateDataPoint {
  label: string;        // e.g. 'Mon', 'Tue'
  successRate: number;  // 0–100
  solved: number;
}

// Recent submissions (progress page)
interface RecentSubmission {
  submissionId: string;
  problemId: string;
  problemTitle: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';   // title-cased
  status: 'Accepted' | 'Wrong Answer' | 'Runtime Error' | 'Time Limit Exceeded';
  language: string;
  submittedAt: string;  // ISO-8601
}

// Recommended problems (progress page)
interface ProgressRecommendedProblem {
  problemId: string;
  title: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  topic: string;
  reason: string;   // e.g. "Based on your weakness in Dynamic Programming"
}

// Hint usage stats
interface HintUsageStats {
  totalHintsUsed: number;
  averageHintsPerProblem: number;
  solvedWithZeroHints: number;
  solvedUsingAllHints: number;
}

// Full response shape
interface StudentAnalytics {
  summary: ProgressSummary;
  topics: TopicPerformance[];
  difficultyBreakdown: DifficultyBreakdown;
  successRateHistory: SuccessRateDataPoint[];
  recentSubmissions: RecentSubmission[];
  recommendations: ProgressRecommendedProblem[];
  hintUsage: HintUsageStats;
}
```

### Analytics — Dashboard / Legacy (`GET /api/analytics/dashboard` and sub-endpoints)
```typescript
interface TopicStat {
  topic: string;
  percentage: number;   // 0–100
  trend: 'up' | 'down' | 'flat';
}

interface DashboardSummary {
  problemsSolved: number;
  avgScore: number;         // 0–100
  streak: number;           // consecutive days
  totalAttempts: number;
  acceptanceRate: number;   // 0–100
  hintsUsedToday: number;
  hintsLimit: number;
}

interface WeeklyActivity {
  date: string;       // 'YYYY-MM-DD'
  solved: number;
  attempted: number;
}

interface ScorePoint {
  date: string;       // 'YYYY-MM-DD'
  score: number;      // 0–100
}

interface RecommendedProblem {
  id: string;
  title: string;
  difficulty: 'easy' | 'medium' | 'hard';
  topic: string;
  topicLabel: string;
  reason: string;
  estimatedMinutes: number;
}

interface StudentDashboardData {
  summary: DashboardSummary;
  topicStats: TopicStat[];
  weeklyActivity: WeeklyActivity[];   // 28 items
  scoreHistory: ScorePoint[];         // 30 items
  recommendations: RecommendedProblem[];
}
```

### Public Profile (`GET /api/profile/:username`)
```typescript
interface LanguageStat {
  language: string;   // e.g. 'Python', 'C#', 'JavaScript'
  solved: number;
}

interface ActivityDay {
  date: string;    // 'YYYY-MM-DD'
  count: number;   // 0 = inactive, 1+ = number of submissions that day
}

interface DifficultyTotals {
  easy: number;    // total Easy problems available on the platform
  medium: number;
  hard: number;
}

interface PublicProfileData {
  user: {
    username: string;          // URL-safe slug, e.g. "test_student"
    name: string;
    avatarInitials: string;
    role: 'student' | 'instructor';
    joinedAt: string;          // ISO date string
    headline?: string;         // optional one-liner, e.g. "SWE | 3 yrs exp"
    bio?: string;              // optional short summary
    social?: {
      linkedin?: string;       // full URL
      github?: string;         // full URL
      twitter?: string;        // full URL
    };
  };
  totalSolved: number;
  totalAttempted: number;
  successRate: number;         // 0–100
  streak: {
    currentStreak: number;
    longestStreak: number;
    totalActiveDays: number;
    totalSubmissionsLastYear: number;
  };
  difficultyBreakdown: DifficultyBreakdown;
  difficultyTotals: DifficultyTotals;    // platform-wide totals, used for progress bars
  languageStats: LanguageStat[];
  topicStats: TopicPerformance[];        // same shape as progress page
  activityGrid: ActivityDay[];           // exactly 365 items, oldest first
  recentAccepted: RecentSubmission[];    // last 10 Accepted-only submissions
}
```

---

## 6. Current Implementation Status

| Feature | Endpoint | Status | Notes |
|---|---|---|---|
| Login | `POST /api/auth/login` | ⚠️ Mocked | In-memory mock users (student@codify.com / instructor@codify.com) |
| Register | `POST /api/auth/register` | ⚠️ Mocked | Creates user in-memory, no persistence |
| Forgot Password | `POST /api/auth/forgot-password` | ⚠️ Mocked | Form shows success state, no HTTP call |
| Problem List | `GET /api/problems` | ⚠️ Mocked | Hardcoded 9-problem array in service |
| Problem Detail | `GET /api/problems/:id` | ⚠️ Mocked | Page hardcoded to Two Sum; route param not yet used |
| Recommended | `GET /api/problems/recommended` | ⚠️ Mocked | Returns first 3 of hardcoded list |
| Run Code | `POST /api/execution/run` | ⚠️ Mocked | Real call commented out, ready to enable |
| Submit | `POST /api/submissions` | ⚠️ Mocked | Real call commented out, ready to enable |
| Poll Submission | `GET /api/submissions/:id` | ⚠️ Mocked | Polling logic fully built (1500ms interval) |
| AI Hints | `POST /api/ai/hints` | ⚠️ Mocked | Real call commented out, ready to enable |
| AI Feedback | `GET /api/submissions/:id/feedback` | ❌ Missing | Full UI complete; backend endpoint does not exist yet |
| Student Analytics | `GET /api/analytics/progress` | ⚠️ Mocked | Used by StudentProgressComponent; no HTTP wired |
| Dashboard | `GET /api/analytics/dashboard` | ⚠️ Mocked | Legacy method; no active component consumes it |
| Summary | `GET /api/analytics/summary` | ⚠️ Mocked | Legacy method; no active component consumes it |
| Topic Stats | `GET /api/analytics/topics` | ⚠️ Mocked | Legacy method; no active component consumes it |
| Activity | `GET /api/analytics/activity` | ⚠️ Mocked | Legacy method; no active component consumes it |
| Score History | `GET /api/analytics/scores` | ⚠️ Mocked | Legacy method; no active component consumes it |
| Recommendations | `GET /api/analytics/recommendations` | ⚠️ Mocked | Legacy method; no active component consumes it |
| Public Profile | `GET /api/profile/:username` | ⚠️ Mocked | Used by ProfileComponent; no HTTP wired |
| ~~Student Progress~~ | ~~`GET /api/progress/student`~~ | 🚫 Deprecated | Replaced by `GET /api/analytics/progress` |
| ~~Class Progress~~ | ~~`GET /api/progress/class`~~ | 🚫 Deprecated | No component uses this; instructor dashboard not yet designed |

**Legend:**
- ✅ Live — real HTTP call working
- ⚠️ Mocked — frontend ready, backend needed
- ❌ Missing — backend endpoint does not exist yet, UI is complete
- 🚫 Deprecated — no longer called by any frontend component

---

## 7. Priority Order for Backend Delivery

Ordered by what unblocks the most frontend functionality:

1. **Auth (Login + Register)** — blocks everything behind `authGuard`
2. **`POST /api/execution/run`** — highest-traffic endpoint, core product experience
3. **`POST /api/submissions` + `GET /api/submissions/:id`** — completes the judge loop; polling logic already built
4. **`GET /api/problems` + `GET /api/problems/:id`** — unblocks the full problem list and per-problem pages
5. **`POST /api/ai/hints`** — AI hint panel fully built and waiting; real call commented out and ready
6. **`GET /api/submissions/:id/feedback`** — feedback UI complete but endpoint does not exist yet; highest-priority new build
7. **`GET /api/analytics/progress`** — unlocks the entire Student Progress page (`/progress`)
8. **`GET /api/profile/:username`** — unlocks the Public Profile page and the `/dashboard` redirect
9. **`POST /api/auth/forgot-password`** — lower priority, form already shows success state
10. **`GET /api/analytics/dashboard` and sub-endpoints** — legacy; back-fill after the progress endpoint is live
11. **`GET /api/problems/recommended`** — no active UI component currently consumes this

---

## 8. Frontend Notes & Gotchas

### Token storage
- JWT stored in `localStorage['codify_token']`
- User object in `localStorage['codify_user']` (no password field)
- Auth header: `Authorization: Bearer <token>`
- No refresh token mechanism exists yet — implement standard JWT expiry

### HttpInterceptor (TODO)
Currently each service builds the `Authorization` header manually via a `headers()` helper. Once real auth is wired, we'll add an `HttpInterceptor` to handle this globally. The backend doesn't need to change anything for this.

### Submission polling interval
The frontend polls `GET /api/submissions/:id` every **1500ms** using `timer(0, 1500)`. Keep response time for this endpoint under 500ms.

### Problem ID hardcoding
The problem page currently uses a hardcoded problem ID: `00000000-0000-0000-0000-000000000005`. Once `GET /api/problems/:id` is live, the component will read the ID from the route param `problems/:id`.

### `/dashboard` redirect
`/dashboard` no longer maps to a component — it redirects to `/profile/:username` using the logged-in user's name slug. The profile page is the new combined dashboard + profile surface. `StudentDashboardComponent` has been removed.

### Username slug format
The slug is derived from the user's full name: lowercase, spaces replaced with underscores (e.g. "Test Student" → `test_student`). The same logic runs in `app.routes.ts` (redirect) and should be used server-side when looking up a profile by slug.

### Activity heatmap grid
`GET /api/profile/:username` must return exactly **365 `ActivityDay` items**, one per calendar day, oldest first, ending on today. The profile component filters by year client-side — the full year of data is always expected in the response.

### `successRateHistory` time range
The progress component has a time-range toggle (`7d` | `30d` | `3m`). The current mock always returns 7 data points with day-of-week labels. When the real endpoint ships, the backend should accept an optional `?range=7d|30d|3m` query param and return the appropriate slice. The component calls `getStudentAnalytics()` without query params for now, so this can be a v2 addition.

### Language support matrix

| Language | Run | Submit | Judge |
|---|---|---|---|
| Python | ✅ (wired, mocked) | ✅ (wired, mocked) | Needed |
| C# | ✅ (wired, mocked) | ✅ (wired, mocked) | Needed |
| JavaScript | frontend mock only | frontend mock only | Not planned |
| Java | frontend mock only | frontend mock only | Not planned |
| C++ | frontend mock only | frontend mock only | Not planned |

### Enabling real API calls
`SubmissionService`, `HintService`, and `AnalyticsService` all have the real HTTP calls written and commented out directly next to the mock line. To switch any endpoint live:
1. Uncomment the `http.get` / `http.post` block in the service
2. Delete the mock return line directly below it

### CORS
Backend needs to allow requests from `http://localhost:4200` (Angular dev server default).

### `avatarInitials` generation
Frontend auto-generates initials from the full name (first letter of first word + first letter of last word). The backend doesn't need to compute this — the frontend derives and stores the value.
