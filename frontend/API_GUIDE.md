# Codify — Backend API Guide

> Generated from a full frontend codebase scan.  
> Angular version: **21.2.0** | Backend stack: **ASP.NET Core** | Base URL: `http://localhost:5237/api`

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
   - [4.7 Progress / Dashboard](#47-progress--dashboard)
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
| `/` | public | Home |
| `/auth/login` | guestGuard | LoginComponent |
| `/auth/register` | guestGuard | RegisterComponent |
| `/auth/forgot-password` | guestGuard | ForgotPasswordComponent |
| `/problems` | authGuard | ProblemListComponent |
| `/problems/:id` | authGuard | ProblemPageComponent |
| `/dashboard` | authGuard | StudentDashboardComponent |

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

### 4.7 Progress / Dashboard

| Method | Endpoint | Status |
|---|---|---|
| GET | `/api/progress/student` | ⚠️ Mocked |
| GET | `/api/progress/class` | ⚠️ Mocked |

#### GET `/api/progress/student`

Returns the logged-in student's progress stats.

**Response `200`:**
```json
{
  "data": {
    "problemsSolved": 47,
    "avgScore": 68,
    "streak": 12,
    "hintsUsedToday": 3,
    "hintsLimit": 5,
    "topicMastery": [
      { "topic": "Arrays", "percentage": 85 },
      { "topic": "Recursion", "percentage": 72 },
      { "topic": "Dyn. Programming", "percentage": 54 },
      { "topic": "Graphs", "percentage": 38 },
      { "topic": "Greedy", "percentage": 61 }
    ]
  }
}
```

#### GET `/api/progress/class`

Returns class-wide stats. Accessible by instructors only.

**Response `200`:**
```json
{
  "data": {
    "activeStudents": 28,
    "enrolledStudents": 32,
    "classAvgScore": 63,
    "integrityFlags": 3,
    "assignedProblems": 14,
    "topicMastery": [
      { "topic": "Arrays", "percentage": 78 },
      { "topic": "Recursion", "percentage": 65 },
      { "topic": "Dyn. Programming", "percentage": 48 },
      { "topic": "Graphs", "percentage": 41 },
      { "topic": "Sorting", "percentage": 72 }
    ]
  }
}
```

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

### Progress
```typescript
interface StudentProgress {
  problemsSolved: number;
  avgScore: number;
  streak: number;
  hintsUsedToday: number;
  hintsLimit: number;
  topicMastery: { topic: string; percentage: number; }[];
}

interface ClassProgress {
  activeStudents: number;
  enrolledStudents: number;
  classAvgScore: number;
  integrityFlags: number;
  assignedProblems: number;
  topicMastery: { topic: string; percentage: number; }[];
}
```

---

## 6. Current Implementation Status

| Feature | Endpoint | Status | Notes |
|---|---|---|---|
| Login | `POST /api/auth/login` | ⚠️ Mocked | In-memory mock users |
| Register | `POST /api/auth/register` | ⚠️ Mocked | No persistence |
| Forgot Password | `POST /api/auth/forgot-password` | ⚠️ Mocked | Form exists, no action |
| Problem List | `GET /api/problems` | ⚠️ Mocked | Hardcoded 9 problems |
| Problem Detail | `GET /api/problems/:id` | ⚠️ Not started | Page hardcoded to Two Sum |
| Recommended | `GET /api/problems/recommended` | ⚠️ Mocked | First 3 of hardcoded list |
| Run Code | `POST /api/execution/run` | ⚠️ Mocked | Real call commented out, ready to enable |
| Submit | `POST /api/submissions` | ⚠️ Mocked | Real call commented out, ready to enable |
| Poll Submission | `GET /api/submissions/:id` | ⚠️ Mocked | Polling logic fully built |
| AI Hints | `POST /api/ai/hints` | ⚠️ Mocked | Real call commented out, ready to enable |
| AI Feedback | `GET /api/submissions/:id/feedback` | ❌ Missing | UI complete, endpoint doesn't exist |
| Student Progress | `GET /api/progress/student` | ⚠️ Mocked | No HTTP call wired |
| Class Progress | `GET /api/progress/class` | ⚠️ Mocked | No HTTP call wired |

**Legend:**
- ✅ Live — real HTTP call working
- ⚠️ Mocked — frontend ready, backend needed
- ❌ Missing — backend endpoint doesn't exist yet, UI exists

---

## 7. Priority Order for Backend Delivery

Ordered by what unblocks the most frontend functionality:

1. **Auth (Login + Register)** — blocks everything behind `authGuard`
2. **`POST /api/execution/run`** — highest-traffic endpoint, core product experience
3. **`POST /api/submissions` + `GET /api/submissions/:id`** — complete the judge loop
4. **`GET /api/problems` + `GET /api/problems/:id`** — unblock the problem list and page
5. **`POST /api/ai/hints`** — AI hint panel is fully built and waiting
6. **`GET /api/submissions/:id/feedback`** — feedback UI is complete but endpoint missing
7. **`GET /api/progress/student`** — needed for student dashboard
8. **`POST /api/auth/forgot-password`** — lower priority
9. **`GET /api/progress/class`** — instructor dashboard, not yet designed

---

## 8. Frontend Notes & Gotchas

### Token storage
- JWT stored in `localStorage['codify_token']`
- User object in `localStorage['codify_user']` (no password field)
- Auth header: `Authorization: Bearer <token>`
- No refresh token mechanism exists yet — implement standard JWT expiry

### HttpInterceptor (TODO)
Currently each service builds the `Authorization` header manually. Once real auth is wired, we'll add an `HttpInterceptor` to handle this globally. The backend doesn't need to change anything for this.

### Submission polling interval
The frontend polls `GET /api/submissions/:id` every **1500ms** using `timer(0, 1500)`. Keep response time for this endpoint under 500ms.

### Problem ID hardcoding
The problem page currently uses a hardcoded problem ID: `00000000-0000-0000-0000-000000000005`. Once `GET /api/problems/:id` is live, the component will read the ID from the route param `problems/:id`.

### Language support matrix

| Language | Run | Submit | Judge |
|---|---|---|---|
| Python | ✅ (wired, mocked) | ✅ (wired, mocked) | Needed |
| C# | ✅ (wired, mocked) | ✅ (wired, mocked) | Needed |
| JavaScript | frontend mock only | frontend mock only | Not planned |
| Java | frontend mock only | frontend mock only | Not planned |
| C++ | frontend mock only | frontend mock only | Not planned |

### Enabling real API calls
All three services (`SubmissionService`, `HintService`) have the real HTTP calls written and commented out right next to the mock line. To switch any endpoint live:
1. Uncomment the `http` block
2. Delete the mock return line directly below it

### CORS
Backend needs to allow requests from `http://localhost:4200` (Angular dev server default).

### `avatarInitials` generation
Frontend auto-generates initials from full name (first + last word initials). The backend doesn't need to compute this — the frontend sends the final value on register if needed, or the backend can derive it using the same logic.
