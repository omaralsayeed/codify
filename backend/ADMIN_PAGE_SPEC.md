# Codify — Admin Panel: Frontend & Backend Handoff

> **Last Updated:** August 16, 2026
> **Branch:** `admin-integrations` (backend) | `admin-page` (frontend)
> **Frontend Status:** ✅ Fully built — all pages working with mock data
> **Backend Status:** ✅ ALL 9 endpoints complete — ready for frontend integration
> **Updated by:** Backend team — August 16, 2026

---

## 🟢 Backend Is Done — Ready to Wire Up

All 9 endpoints described in this document have been built, reviewed, and pushed to the `admin-integrations` branch. The frontend can now replace mock data with real HTTP calls one endpoint at a time.

### Endpoint Status

| # | Method | Endpoint | Status | Notes |
|---|---|---|---|---|
| 1 | GET | `/api/admin/stats` | ✅ Live | |
| 2 | GET | `/api/admin/users` | ✅ Live | |
| 3 | GET | `/api/admin/users/:id` | ✅ Live | |
| 4 | PATCH | `/api/admin/users/:id/status` | ✅ Live | |
| 5 | GET | `/api/admin/problems` | ✅ Live | |
| 6 | POST | `/api/problems` | ✅ Live | Request shape changed — see notes below |
| 7 | PATCH | `/api/problems/:id` | ✅ Live | Was PUT, now PATCH |
| 8 | PATCH | `/api/problems/:id/status` | ✅ Live | |
| 9 | DELETE | `/api/problems/:id` | ✅ Live | |

---

## Answers to Frontend Team's Questions

> These were open questions at the bottom of the original spec. All answered.

**1. `status` field on Users table** — ✅ Already exists. Added during the instructor approval flow sprint. Values: `"active"` / `"pending"`. Returned as a lowercase string in all admin responses.

**2. `initials` field** — Backend computes and returns it. Algorithm: first character of each of the first two words in `FullName`, uppercased. e.g. `"Karim Ahmed"` → `"KA"`, `"Dr. Hana Saad"` → `"DH"`. Frontend does not need to derive it.

**3. `lastActiveAt` field** — Backed by `LastLoginAt` on the User entity (updated every time the user logs in). This is `null` if the user has never logged in after registration. Returned as `lastActiveAt` in all admin user responses.

**4. `avgScore` for students** — Average of `Score` across **accepted submissions only** (0–100 scale). `null` for instructors. `null` for students with zero accepted submissions.

**5. Problem `createdAt`** — ✅ Already stored. `Problem.CreatedAt` is set at creation and never updated. Returned in all admin problem responses.

**6. Admin seeding** — Admin accounts must be seeded directly in the database. The register form intentionally excludes the Admin role (role = 2). Use a SQL seed script or EF data seeder. The backend team will provide a seed script separately.

---

## Important Changes from Original Spec

### POST /api/problems — Request shape changed

The original spec listed `tags` as string names. Our `CreateProblemRequest` now fully matches:

```json
{
  "title": "Two Sum",
  "difficulty": 0,
  "tags": ["Arrays", "Hash Map"],
  "statement": "Given an array of integers...",
  "constraints": "2 <= nums.length <= 10^4",
  "sampleTestCases": [
    { "input": "nums = [2,7,11,15], target = 9", "expectedOutput": "[0,1]" }
  ],
  "isActive": true,
  "timeLimitMs": 2000,
  "memoryLimitMb": 256
}
```

- `tags` — array of **string names** (not GUIDs). Backend resolves or creates tags automatically.
- `sampleTestCases` — uses `input` / `expectedOutput` keys (not `inputData` / `expectedOutput`).
- `isActive` — optional, defaults to `true`.

### PATCH /api/problems/:id — Was PUT

Changed from `PUT` to `PATCH`. Same partial-update semantics, same request body shape as the spec. Only send fields you want to change.

### Response envelope

All responses include a `success` boolean alongside `data`:

```json
{
  "success": true,
  "data": { ... }
}
```

Error responses:
```json
{
  "success": false,
  "errorCode": "NOT_FOUND",
  "message": "Human-readable description"
}
```

Error codes you'll encounter from admin endpoints:
| Code | HTTP | When |
|---|---|---|
| `NOT_FOUND` | 404 | User or problem not found |
| `FORBIDDEN` | 403 | Trying to modify an admin account |
| `CONFLICT` | 409 | Problem title already exists |
| `VALIDATION_ERROR` | 400 | Missing required fields, invalid values |
| `ACCOUNT_PENDING` | 403 | Login attempt by a pending user |

---

## The Story So Far

The frontend team built a complete admin panel on the `admin-page` branch — fully functional with mock data. The backend team has now delivered all endpoints on the `admin-integrations` branch. The frontend can replace mock data with real HTTP calls.

---

## What the Admin Panel Is

The admin panel is a **completely separate full-screen control system**. When a user with `role = admin` (role number = **2**) logs in, they are automatically redirected to `/admin/overview` and see a dedicated dark-sidebar control panel. The regular navbar and footer are hidden entirely.

### What Admins Can Do
1. **Overview dashboard** — platform stats at a glance
2. **User management** — view all students and instructors, activate or set-pending any of them
3. **User detail** — deep dive into a single user's profile, stats, recent submissions
4. **Problem list** — see all problems including inactive ones, toggle active/inactive
5. **Problem create/edit** — full form to add new problems or edit existing ones

### What Admins Cannot Do (by design)
- See or manage other admin accounts (admins are excluded from the users list)
- The admin panel has no access to student-facing features

---

## Role System

| Role | Backend number | Frontend string |
|---|---|---|
| Student | `0` | `'student'` |
| Instructor | `1` | `'instructor'` |
| **Admin** | **`2`** | **`'admin'`** |

`POST /api/auth/login` returns `role: 2` for admin users. The frontend `AuthService` already maps this and redirects admins to `/admin/overview` automatically.

All `/api/admin/*` endpoints require `[Authorize(Roles = "Admin")]`. Non-admins get `403`.

---

## Endpoint Reference

---

### 1. GET /api/admin/stats ✅

Returns platform-wide statistics for the overview dashboard.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request:** No params.

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "totalUsers": 124,
    "totalStudents": 118,
    "totalInstructors": 6,
    "activeInstructors": 3,
    "pendingInstructors": 3,
    "totalProblems": 32,
    "totalSubmissions": 4820,
    "newUsersToday": 5,
    "newUsersThisWeek": 18,
    "submissionsToday": 87
  }
}
```

---

### 2. GET /api/admin/users ✅

Paginated, filterable list of all users. Admins are always excluded.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Query params:**

| Param | Type | Default | Notes |
|---|---|---|---|
| `search` | string | — | Case-insensitive contains on name OR email |
| `role` | `student` \| `instructor` | — | Omit = return both |
| `status` | `active` \| `pending` | — | Omit = return both |
| `sortBy` | `name` \| `registeredAt` \| `lastActiveAt` | `registeredAt` | |
| `sortDir` | `asc` \| `desc` | `desc` | |
| `page` | number | `1` | |
| `pageSize` | number | `20` | |

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "users": [
      {
        "id": "uuid",
        "name": "Karim Ahmed",
        "initials": "KA",
        "email": "karim@example.com",
        "role": 0,
        "status": "active",
        "registeredAt": "2026-06-01T10:00:00Z",
        "lastActiveAt": "2026-08-13T14:22:00Z",
        "problemsSolved": 38,
        "organization": null
      },
      {
        "id": "uuid",
        "name": "Dr. Hana Saad",
        "initials": "HS",
        "email": "hana@university.edu",
        "role": 1,
        "status": "active",
        "registeredAt": "2026-05-15T09:00:00Z",
        "lastActiveAt": "2026-08-13T08:00:00Z",
        "problemsSolved": null,
        "organization": "Cairo University"
      }
    ],
    "total": 124,
    "page": 1,
    "pageSize": 20
  }
}
```

**Field notes:**
- `initials` — backend-computed, no need to derive on frontend.
- `status` — `"active"` or `"pending"` (lowercase string).
- `problemsSolved` — students only. `null` for instructors.
- `organization` — instructors only. `null` for students.
- `lastActiveAt` — maps to last login timestamp. `null` if never logged in.

---

### 3. GET /api/admin/users/:id ✅

Full detail for a single user.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "name": "Karim Ahmed",
    "initials": "KA",
    "email": "karim@example.com",
    "role": 0,
    "status": "active",
    "registeredAt": "2026-06-01T10:00:00Z",
    "lastActiveAt": "2026-08-13T14:22:00Z",
    "organization": null,
    "problemsSolved": 38,
    "avgScore": 92.5,
    "streak": 14,
    "totalSubmissions": 61,
    "recentSubmissions": [
      {
        "problemTitle": "Two Sum",
        "status": "Accepted",
        "submittedAt": "2026-08-13T10:45:00Z"
      },
      {
        "problemTitle": "Valid Parentheses",
        "status": "WrongAnswer",
        "submittedAt": "2026-08-12T16:20:00Z"
      }
    ]
  }
}
```

**Field notes:**
- `avgScore` — average of accepted submissions' scores (0–100). `null` for instructors.
- `streak` — consecutive days with at least one accepted submission. `null` for instructors.
- `totalSubmissions` — real total count (not capped). `0` for instructors.
- `recentSubmissions` — last 5, newest first. Empty array `[]` for instructors.
- `recentSubmissions[].status` — `"Accepted"`, `"WrongAnswer"`, `"RuntimeError"`, `"TimeLimitExceeded"`, `"CompileError"`, `"MemoryLimitExceeded"`.

**Response `404`:** User not found or is an admin.

---

### 4. PATCH /api/admin/users/:id/status ✅

Activates or sets a user to pending.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body:**
```json
{ "status": "active" }
```

**Response `200`:** Full updated user object (same shape as endpoint #3).

**Response `400`:** Invalid status value.

**Response `403`:** Target user is an admin.

**Response `404`:** User not found.

**Notes:**
- Setting an instructor from `"pending"` → `"active"` is the approval action.
- Setting any user to `"pending"` will block their next login with `errorCode: "ACCOUNT_PENDING"`.

---

### 5. GET /api/admin/problems ✅

All problems including inactive ones.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Query params:**

| Param | Type | Default | Notes |
|---|---|---|---|
| `search` | string | — | Case-insensitive title contains |
| `difficulty` | `0` \| `1` \| `2` | — | Omit = all |
| `tag` | string | — | Filter by tag name. Omit = all |
| `isActive` | `true` \| `false` | — | Omit = all (including inactive) |
| `sortBy` | `title` \| `difficulty` \| `solvedCount` \| `createdAt` | `createdAt` | |
| `sortDir` | `asc` \| `desc` | `desc` | |
| `page` | number | `1` | |
| `pageSize` | number | `20` | |

**Response `200`:**
```json
{
  "success": true,
  "data": {
    "problems": [
      {
        "id": "uuid",
        "title": "Two Sum",
        "difficulty": 0,
        "tags": ["Arrays", "Hash Map"],
        "solvedCount": 36045,
        "totalSubmissions": 48200,
        "isActive": true,
        "createdAt": "2026-04-01T10:00:00Z"
      }
    ],
    "total": 32,
    "page": 1,
    "pageSize": 20
  }
}
```

---

### 6. POST /api/problems ✅

Creates a new problem. Admin only.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body:**
```json
{
  "title": "Two Sum",
  "difficulty": 0,
  "tags": ["Arrays", "Hash Map"],
  "statement": "Given an array of integers nums and an integer target...",
  "constraints": "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9",
  "sampleTestCases": [
    {
      "input": "nums = [2,7,11,15], target = 9",
      "expectedOutput": "[0,1]"
    }
  ],
  "isActive": true,
  "timeLimitMs": 2000,
  "memoryLimitMb": 256
}
```

**Key points:**
- `tags` — string names, NOT GUIDs. Backend resolves existing tags or creates new ones.
- `sampleTestCases` — field names are `input` / `expectedOutput` (camelCase).
- `isActive` — optional, defaults to `true`.

**Validation failures return `400`:**
- `title`: required, min 3 chars
- `difficulty`: required, `0`/`1`/`2` only
- `tags`: at least 1
- `statement`: required, min 50 chars
- `sampleTestCases`: at least 1, each must have non-empty `input` and `expectedOutput`

**Response `201`:** Full problem detail (same shape as `GET /api/problems/:id`).

**Response `409`:** `{ "success": false, "errorCode": "CONFLICT", "message": "A problem with this title already exists." }`

---

### 7. PATCH /api/problems/:id ✅

Partial update. All fields optional — only send what changed.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body** (all optional):
```json
{
  "title": "Updated Title",
  "difficulty": 1,
  "tags": ["Arrays"],
  "statement": "Updated statement...",
  "constraints": "Updated constraints...",
  "sampleTestCases": [
    { "input": "...", "expectedOutput": "..." }
  ],
  "isActive": false,
  "timeLimitMs": 3000,
  "memoryLimitMb": 512
}
```

**Notes:**
- `sampleTestCases` — if provided, **replaces all existing sample test cases** entirely.
- `tags` — if provided, replaces all existing tags.
- `isActive` — toggles the problem's visibility to students.

**Response `200`:** Full updated problem detail.

**Response `400`:** Validation failure (e.g. duplicate title in same request).

**Response `404`:** Problem not found.

**Response `409`:** Title already exists on another problem.

---

### 8. PATCH /api/problems/:id/status ✅

Toggles active/inactive. Single-purpose clean action separate from the full PATCH.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body:**
```json
{ "isActive": false }
```

**Response `200`:**
```json
{
  "success": true,
  "data": { "id": "uuid", "isActive": false }
}
```

**Response `404`:** Problem not found.

---

### 9. DELETE /api/problems/:id ✅

Soft-deletes a problem. Record stays in DB, submissions are untouched.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Response `200`:**
```json
{
  "success": true,
  "data": { "id": "uuid", "deleted": true }
}
```

**Response `404`:** Problem not found.

**Notes:**
- Soft-deleted problems are never returned by `GET /api/problems` or `GET /api/admin/problems`.
- This action is irreversible via the API — no undelete endpoint exists.

---

## How the Frontend Wires Up Each Endpoint

Replace mock data with real HTTP calls. All calls require `Authorization: Bearer <token>`.

### Overview stats
```typescript
// admin-overview.component.ts
this.http.get<{success: boolean, data: AdminStats}>(`${baseUrl}/admin/stats`, { headers })
  .pipe(map(r => r.data))
  .subscribe(stats => this.stats.set(stats));
```

### Users list
```typescript
// admin-users.component.ts
this.http.get<{data: {users: AdminUserRow[], total: number}}>
  (`${baseUrl}/admin/users`, { headers, params })
  .pipe(map(r => r.data))
  .subscribe(({ users, total }) => {
    this.allUsers.set(users);
    this.total.set(total);
  });
```

### User detail
```typescript
// admin-user-detail.component.ts
this.http.get<{data: AdminUserDetail}>(`${baseUrl}/admin/users/${id}`, { headers })
  .pipe(map(r => r.data))
  .subscribe(user => this.user.set(user));
```

### User status toggle
```typescript
// admin-users.component.ts + admin-user-detail.component.ts
this.http.patch<{data: AdminUserDetail}>
  (`${baseUrl}/admin/users/${id}/status`, { status: newStatus }, { headers })
  .pipe(map(r => r.data))
  .subscribe(updated => this.user.set(updated));
```

### Problems list
```typescript
// admin-problems.component.ts
this.http.get<{data: {problems: AdminProblemRow[], total: number}}>
  (`${baseUrl}/admin/problems`, { headers, params })
  .pipe(map(r => r.data))
  .subscribe(({ problems, total }) => {
    this.allProblems.set(problems);
    this.total.set(total);
  });
```

### Problem status toggle
```typescript
// admin-problems.component.ts — for the isActive toggle button
this.http.patch<{data: {id: string, isActive: boolean}}>
  (`${baseUrl}/problems/${id}/status`, { isActive: !current }, { headers })
  .pipe(map(r => r.data))
  .subscribe(({ isActive }) => /* update local state */);
```

### Problem create
```typescript
// admin-problem-form.component.ts
this.http.post<{data: any}>(`${baseUrl}/problems`, body, { headers })
  .subscribe({
    next: () => router.navigate(['../../../problems']),
    error: err => {
      if (err.status === 409) this.error.set('A problem with this title already exists.');
      else if (err.status === 400) this.error.set(err.error?.message);
    }
  });
```

### Problem edit
```typescript
// admin-problem-form.component.ts
this.http.patch<{data: any}>(`${baseUrl}/problems/${id}`, body, { headers })
  .subscribe({
    next: () => router.navigate(['../../../problems']),
    error: err => {
      if (err.status === 409) this.error.set('A problem with this title already exists.');
      else if (err.status === 400) this.error.set(err.error?.message);
    }
  });
```

### Problem delete
```typescript
// admin-problems.component.ts
this.http.delete<{data: {id: string, deleted: boolean}}>
  (`${baseUrl}/problems/${id}`, { headers })
  .subscribe(() => this.allProblems.update(p => p.filter(x => x.id !== id)));
```

---

## Frontend Files Reference

```
src/app/core/guards/admin.guard.ts                         ← blocks non-admins from /admin/**
src/app/features/admin/
├── admin.routes.ts
├── shell/admin-shell.component.*                          ← full-screen layout + sidebar
├── overview/admin-overview.component.*                    ← GET /api/admin/stats
├── users/admin-users.component.*                          ← GET /api/admin/users
├── user-detail/admin-user-detail.component.*              ← GET /api/admin/users/:id
├── problems/admin-problems.component.*                    ← GET /api/admin/problems
└── problem-form/admin-problem-form.component.*            ← POST + PATCH /api/problems/:id
```

---

## Testing Without Backend (Still Works)

```javascript
localStorage.setItem('codify_token', 'any-token');
localStorage.setItem('codify_user', JSON.stringify({
  id: 'admin-001',
  name: 'Admin User',
  email: 'admin@codify.com',
  role: 'admin',
  avatarInitials: 'AU',
  streak: 0
}));
location.href = '/admin/overview';
```

---

*Originally authored by: Frontend team — August 14, 2026*
*Updated by: Backend team — August 16, 2026 | Branch: `admin-integrations`*
