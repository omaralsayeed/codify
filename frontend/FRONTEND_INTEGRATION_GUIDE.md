# Codify — Frontend Integration Guide

> **From:** Backend Team  
> **To:** Frontend Team  
> **Date:** August 11, 2026  
> **Status:** Phase 1 endpoints are LIVE — ready to wire  
> **Base URL:** `http://localhost:5237/api`  
> **Swagger UI:** `http://localhost:5237/swagger`

---

## Table of Contents

1. [Critical Global Changes — Read First](#1-critical-global-changes--read-first)
2. [Response Envelope](#2-response-envelope)
3. [Authentication](#3-authentication)
   - [3.1 Login](#31-login)
   - [3.2 Register](#32-register)
   - [3.3 Forgot Password](#33-forgot-password)
4. [Problems](#4-problems)
   - [4.1 Get All Problems](#41-get-all-problems)
   - [4.2 Get Problem by ID](#42-get-problem-by-id)
5. [Enum Reference — Map These in Your Services](#5-enum-reference--map-these-in-your-services)
6. [Error Handling](#6-error-handling)
7. [CORS & Auth Header](#7-cors--auth-header)
8. [What Is NOT Ready Yet — Do Not Wire](#8-what-is-not-ready-yet--do-not-wire)
9. [Step-by-Step Checklist for the Frontend Team](#9-step-by-step-checklist-for-the-frontend-team)

---

## 1. Critical Global Changes — Read First

Before touching any service file, there are **two things** that changed from your original `API_GUIDE.md` that affect every single endpoint.

---

### 1.1 — Enums Come Back as Numbers, Not Strings

In your original guide, you expected enums like this:

```json
{ "role": "student", "difficulty": "easy" }
```

The backend returns enums as **integers**. The mapping is:

| Field | 0 | 1 | 2 |
|---|---|---|---|
| `role` | `student` | `instructor` | — |
| `difficulty` | `easy` | `medium` | `hard` |
| `language` | `Python` | `CSharp` | — |

You need to map these in your Angular services. See [Section 5](#5-enum-reference--map-these-in-your-services) for the full mapping helper code.

---

### 1.2 — User Object Fields Are Different

Your `User` interface assumed:

```typescript
{ id, name, role: 'student' | 'instructor', avatarInitials, streak }
```

The backend returns:

```typescript
{ userId, fullName, role: 0 | 1 }
```

You will need to adapt your `AuthService` to map these fields when storing to `localStorage`. Full details in [Section 3.1](#31-login).

---

## 2. Response Envelope

Every response from the backend is wrapped in a `data` key — **this matches what your services already expect**:

```json
{
  "data": { ... }
}
```

Your `.pipe(map(r => r.data))` pattern works correctly. No changes needed here.

Error responses use a `message` key — also matches your `ServiceError` interface:

```json
{
  "message": "Human-readable error description"
}
```

---

## 3. Authentication

### 3.1 Login

```
POST /api/auth/login
```

**Request body — no changes needed:**

```json
{
  "email": "student@codify.com",
  "password": "123456"
}
```

**Response `200` — actual shape from backend:**

```json
{
  "data": {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAt": "2026-08-12T10:00:00Z",
    "user": {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Jane Doe",
      "role": 0
    }
  }
}
```

**Field mapping — what changed vs the original guide:**

| Original guide expected | Backend actually returns | What you need to do |
|---|---|---|
| `data.user.id` | `data.user.userId` | Read from `userId` |
| `data.user.name` | `data.user.fullName` | Read from `fullName` |
| `data.user.role` = `"student"` | `data.user.role` = `0` | Map `0 → "student"`, `1 → "instructor"` |
| `data.user.avatarInitials` | **Not returned** | Generate from `fullName` on the frontend (you already do this) |
| `data.user.streak` | **Not returned** | Set to `0` after login, load from analytics endpoint later |
| No `expiresAt` in guide | `data.expiresAt` returned | Optionally store for token expiry checks |

**What to do in `AuthService.login()`:**

```typescript
login(email: string, password: string): Observable<User> {
  return this.http.post<{ data: LoginApiResponse }>(`${this.baseUrl}/auth/login`, { email, password })
    .pipe(
      map(r => r.data),
      map(resp => {
        const user: User = {
          id: resp.user.userId,
          name: resp.user.fullName,
          email: email,
          role: resp.user.role === 0 ? 'student' : 'instructor',
          avatarInitials: this.getInitials(resp.user.fullName),
          streak: 0
        };
        localStorage.setItem('codify_token', resp.token);
        localStorage.setItem('codify_user', JSON.stringify(user));
        return user;
      })
    );
}
```

**Response `401`:**

```json
{ "message": "Invalid email or password" }
```

---

### 3.2 Register

```
POST /api/auth/register
```

**Request body — CHANGED, read carefully:**

```json
{
  "fullName": "Jane Doe",
  "email": "jane@example.com",
  "password": "securepass123",
  "role": 0
}
```

**What changed:**

- `role` must be sent as a **number**: `0` for student, `1` for instructor
- `password` has a **minimum length of 8 characters** — validation will fail silently if shorter
- **Remove these fields from the register request** — the backend does not accept them yet and will return `400`:
  - `organization`
  - `phoneNumber`
  - `country`
  - `city`

> These fields are collected on the register form but backend support is coming in a later sprint. For now, collect them in the form but **do not send them in the API call**.

**Response `201` — IMPORTANT: NO TOKEN IS RETURNED on register:**

```json
{
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "email": "jane@example.com",
    "role": 0
  }
}
```

**Action required:** After a successful register (`201`), immediately call `POST /api/auth/login` with the same credentials to get the JWT. Do not try to extract a token from the register response.

```typescript
register(request: RegisterFormData): Observable<User> {
  const body = {
    fullName: request.fullName,
    email: request.email,
    password: request.password,
    role: request.role === 'student' ? 0 : 1
    // do NOT include organization, phoneNumber, country, city
  };

  return this.http.post<{ data: RegisterApiResponse }>(`${this.baseUrl}/auth/register`, body)
    .pipe(
      switchMap(() => this.login(request.email, request.password))
    );
}
```

**Response `400` — validation failure example:**

```json
{ "message": "Password must be at least 8 characters." }
```

**Response `409` — email already exists:**

```json
{ "message": "Email is already registered." }
```

---

### 3.3 Forgot Password

```
POST /api/auth/forgot-password
```

**Status: ❌ NOT implemented yet.** Keep the form mocked. Do not wire this endpoint — it does not exist on the backend.

---

## 4. Problems

### 4.1 Get All Problems

```
GET /api/problems
```

**Requires:** `Authorization: Bearer <token>` header.

**Query parameters:**

| Param | Type | Example | Notes |
|---|---|---|---|
| `difficulty` | number | `?difficulty=0` | `0`=Easy, `1`=Medium, `2`=Hard |
| `tag` | string | `?tag=Arrays` | Use `tag`, NOT `topic` |
| `search` | string | `?search=two+sum` | Title search |
| `page` | number | `?page=1` | Default: `1` |
| `pageSize` | number | `?pageSize=20` | Default: `20` |

> The original guide used `?topic=arrays` — **change this to `?tag=Arrays`** in your filter service.

**Response `200` — actual shape:**

```json
{
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Two Sum",
      "difficulty": 0,
      "tags": ["Arrays", "Hash Map"],
      "isActive": true
    }
  ]
}
```

**Field mapping — what changed vs original guide:**

| Original guide expected | Backend actually returns | What you need to do |
|---|---|---|
| `difficulty` = `"easy"` | `difficulty` = `0` | Map number to string (see Section 5) |
| `topic` (single string) | `tags` (array of strings) | Use `tags[0]` as primary topic, `tags.join(' · ')` as `topicLabel` |
| `topicLabel` | Not returned | Build from `tags`: `tags.join(' · ')` |
| `solvedCount` | **Not in list response** | Hide this from the list UI for now |

**Example mapping in your service:**

```typescript
mapProblemSummary(raw: any): Problem {
  return {
    id: raw.id,
    title: raw.title,
    difficulty: this.mapDifficulty(raw.difficulty),   // 0 → 'easy'
    topic: raw.tags?.[0]?.toLowerCase().replace(' ', '-') ?? '',
    topicLabel: raw.tags?.join(' · ') ?? '',
    solvedCount: 0  // not available in list, set to 0
  };
}
```

---

### 4.2 Get Problem by ID

```
GET /api/problems/{id}
```

**Requires:** `Authorization: Bearer <token>` header.

**Important:** The current hardcoded problem ID `00000000-0000-0000-0000-000000000005` **will not work** — the database has real seeded UUIDs. Open Swagger at `http://localhost:5237/swagger`, call `GET /api/problems`, and copy a real `id` from the response to use during development. Once the problems list is wired, the component reads the ID from the route param automatically.

**Response `200` — actual shape:**

```json
{
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Two Sum",
    "slug": "two-sum",
    "statement": "Given an array of integers nums and an integer target...",
    "difficulty": 0,
    "constraints": "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9",
    "languageSupport": ["Python", "CSharp"],
    "tags": ["Arrays", "Hash Map"],
    "sampleTestCases": [
      {
        "input": "nums = [2,7,11,15], target = 9",
        "expectedOutput": "[0,1]"
      }
    ],
    "isActive": true,
    "isPublic": true,
    "timeLimitMs": 2000,
    "memoryLimitMb": 256,
    "acceptedSubmissionsCount": 36045,
    "totalSubmissionsCount": 48200
  }
}
```

**Field mapping — what changed vs original guide:**

| Original guide expected | Backend actually returns | What you need to do |
|---|---|---|
| `description` | `statement` | Read from `statement` |
| `constraints` as `string[]` | `constraints` as single `string` (newline-separated) | Split on `\n` if you need an array: `constraints.split('\n')` |
| `examples[]` with `input`, `output`, `explanation` | `sampleTestCases[]` with `input`, `expectedOutput` only | Map `expectedOutput → output`, no `explanation` field |
| `starterCode` object per language | **Not returned** | Keep your existing hardcoded starter code per language |
| `topic` / `topicLabel` | `tags[]` array | Same as list: build from `tags` |
| `solvedCount` | `acceptedSubmissionsCount` | Read from `acceptedSubmissionsCount` |

**Example mapping in your service:**

```typescript
mapProblemDetail(raw: any): ProblemDetail {
  return {
    id: raw.id,
    title: raw.title,
    difficulty: this.mapDifficulty(raw.difficulty),
    topic: raw.tags?.[0]?.toLowerCase().replace(' ', '-') ?? '',
    topicLabel: raw.tags?.join(' · ') ?? '',
    solvedCount: raw.acceptedSubmissionsCount,
    description: raw.statement,                           // ← renamed
    constraints: raw.constraints?.split('\n') ?? [],      // ← split string to array
    examples: raw.sampleTestCases?.map((tc: any) => ({
      input: tc.input,
      output: tc.expectedOutput,                          // ← renamed
      explanation: ''                                     // ← not available yet
    })) ?? [],
    starterCode: this.getHardcodedStarterCode(raw.id),   // ← keep your existing logic
  };
}
```

**Response `404`:**

```json
{ "message": "Problem not found." }
```

---

## 5. Enum Reference — Map These in Your Services

Add these helper methods once in a shared service or utility file and reuse everywhere.

### Difficulty

```typescript
// Backend sends: 0 | 1 | 2
mapDifficulty(value: number): 'easy' | 'medium' | 'hard' {
  const map: Record<number, 'easy' | 'medium' | 'hard'> = {
    0: 'easy',
    1: 'medium',
    2: 'hard'
  };
  return map[value] ?? 'easy';
}

// When sending difficulty as a filter param:
difficultyToNumber(value: 'easy' | 'medium' | 'hard'): number {
  return { easy: 0, medium: 1, hard: 2 }[value];
}
```

### UserRole

```typescript
// Backend sends: 0 | 1
mapRole(value: number): 'student' | 'instructor' {
  return value === 0 ? 'student' : 'instructor';
}

// When sending role in register:
roleToNumber(value: 'student' | 'instructor'): number {
  return value === 'student' ? 0 : 1;
}
```

### SubmissionLanguage

```typescript
// Backend sends: 0 | 1
// Backend expects in requests: 0 | 1
mapLanguage(value: number): 'Python' | 'CSharp' {
  return value === 0 ? 'Python' : 'CSharp';
}

languageToNumber(value: 'Python' | 'CSharp'): number {
  return value === 'Python' ? 0 : 1;
}
```

### SubmissionStatus (for future use)

```typescript
// Backend sends these as strings (not numbers):
type SubmissionStatus =
  | 'Pending'
  | 'Running'
  | 'Accepted'
  | 'WrongAnswer'
  | 'RuntimeError'
  | 'TimeLimitExceeded'
  | 'CompileError'
  | 'MemoryLimitExceeded';   // ← extra status vs the original guide

// Keep polling when: status === 'Pending' || status === 'Running'
// Stop polling for all other values
```

---

## 6. Error Handling

All error responses follow this shape — your existing `ServiceError` interface works without changes:

```json
{ "message": "Human-readable error description" }
```

| HTTP Status | When it happens |
|---|---|
| `400` | Validation failed (missing field, password too short, etc.) |
| `401` | Wrong credentials or missing/expired JWT |
| `403` | Authenticated but wrong role (e.g. student hitting instructor-only route) |
| `404` | Resource does not exist |
| `409` | Conflict — e.g. email already registered |
| `429` | Rate limit hit |
| `500` | Server error — show a generic "Something went wrong" message |

---

## 7. CORS & Auth Header

**CORS is configured** — the backend allows all requests from `http://localhost:4200`. No changes needed on your side.

**Auth header format** — same as the guide:

```
Authorization: Bearer <jwt_token>
```

Read the token from `localStorage['codify_token']` as before.

**JWT expiry:** Tokens expire after **24 hours** in development. The `expiresAt` field is included in the login response if you want to proactively check expiry. There is no refresh token — if the token expires, redirect to login.

---

## 8. What Is NOT Ready Yet — Do Not Wire

The following endpoints exist on the backend but require external services (Judge0, OpenAI) to be running, or are not yet built. Keep your mocks for all of these.

| Endpoint | Reason Not Ready |
|---|---|
| `POST /api/execution/run` | Needs Judge0 running locally |
| `POST /api/submissions` | Needs Judge0 running locally |
| `GET /api/submissions/:id` | Needs Judge0 running locally |
| `POST /api/ai/hints` | Needs OpenAI API key configured |
| `GET /api/submissions/:id/feedback` | Needs OpenAI API key configured |
| `GET /api/progress/student` | Not built yet (Sprint 2) |
| `GET /api/progress/class` | Not built yet (Sprint 2) |
| `POST /api/auth/forgot-password` | Not built yet |
| `GET /api/problems/recommended` | Not built yet |
| All `/api/instructor/*` endpoints | Not built yet (Sprint 4) |
| All `/api/contests/*` endpoints | Not built yet (Sprint 4) |

> We will send a new update guide when each batch of endpoints is ready. Do not try to call these — they will return `404` or `500`.

---

## 9. Step-by-Step Checklist for the Frontend Team

Work through these in order. Each step builds on the previous one.

---

### Step 1 — Add Enum Mappers

Create a utility file `src/app/core/utils/enum-mappers.ts` (or add to your existing utils) with the mapping functions from [Section 5](#5-enum-reference--map-these-in-your-services).

---

### Step 2 — Update `AuthService` — Login

1. Remove the in-memory mock users array
2. Replace the mock `login()` with a real HTTP call to `POST /api/auth/login`
3. Map the response fields:
   - `userId` → store as `id`
   - `fullName` → store as `name`
   - `role` (number) → map to `'student'` or `'instructor'`
   - Generate `avatarInitials` from `fullName` on the frontend
   - Set `streak: 0` for now
4. Store token in `localStorage['codify_token']`
5. Store mapped user object in `localStorage['codify_user']`
6. Test: log in with a seeded user from Swagger, verify `localStorage` is set

---

### Step 3 — Update `AuthService` — Register

1. Replace the mock `register()` with a real HTTP call to `POST /api/auth/register`
2. Send only: `{ fullName, email, password, role }` — drop the extra fields
3. Send `role` as a number (`0` or `1`)
4. After successful `201`, call `login()` with the same credentials
5. Test: register a new user, verify they are redirected and logged in

---

### Step 4 — Update `ProblemsService` — Problem List

1. Replace the hardcoded problems array with a real HTTP call to `GET /api/problems`
2. Add the `Authorization` header (you already have the `headers()` helper)
3. Map the response using `mapProblemSummary()` from [Section 4.1](#41-get-all-problems)
4. Update the filter call: change `?topic=` to `?tag=` and send difficulty as a number
5. Test: open the problem list page, verify problems load from the database

---

### Step 5 — Update `ProblemsService` — Problem Detail

1. Replace the hardcoded "Two Sum" data with a real HTTP call to `GET /api/problems/{id}`
2. Map the response using `mapProblemDetail()` from [Section 4.2](#42-get-problem-by-id)
3. Remove the hardcoded problem ID check — read the ID from the route param directly
4. Get a real problem ID from Swagger (`GET /api/problems` → copy any `id`)
5. Test: navigate to `/problems/{real-id}`, verify the problem loads correctly

---

### Step 6 — Smoke Test the Full Flow

After all steps above, test this complete flow end to end:

1. Register a new student account
2. Verify redirect to `/problems`
3. Verify the problem list loads with real data from the DB
4. Click a problem, verify the detail page loads with correct title, description, examples
5. Verify the difficulty badge displays correctly (not `0`, `1`, `2`)
6. Verify the topic label displays correctly (not a raw array)
7. Try logging out and back in — verify the session restores correctly

---

### Step 7 — Verify Error Cases

1. Try logging in with wrong password → should show `"Invalid email or password"`
2. Try registering with a 7-character password → should show validation error
3. Try registering with an already-used email → should show conflict error
4. Manually clear the token from `localStorage` and try to access `/problems` → should redirect to login

---

*For any questions or if a response shape doesn't match what's documented here, ping the backend team with the exact request you sent and the response you got — we'll investigate immediately.*
