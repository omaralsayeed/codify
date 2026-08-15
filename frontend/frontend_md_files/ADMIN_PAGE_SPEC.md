# Codify — Admin Panel: Frontend & Backend Handoff

> **Last Updated:** August 14, 2026
> **Branch:** `admin-page` (branched from `linking-backend-with-frontend`)
> **Frontend Status:** ✅ Fully built — all pages working with mock data, waiting for backend
> **Backend Status:** ❌ All admin endpoints still need to be built
> **Shared by:** Frontend team → Backend team

---

## The Story So Far

The frontend team has built a **complete admin panel** on the `admin-page` branch. Every page is fully functional with mock/hardcoded data. The UI is done, the routing is done, the guards are done, the forms are done. The only thing missing is real data from the backend.

The moment the backend delivers the endpoints listed in this document, the frontend will replace the mock data with real HTTP calls and the admin panel will be live.

This document tells the backend team exactly what to build, what shape the data needs to be, and what priority to build it in.

---

## What the Admin Panel Is

The admin panel is a **completely separate full-screen control system** — not the regular Codify student/instructor app. When a user with `role = admin` (role number = **2**) logs in, they are automatically redirected to `/admin/overview` and see a dedicated dark-sidebar control panel. The regular navbar and footer are hidden entirely.

### What Admins Can Do (all built on frontend, needs backend)
1. **Overview dashboard** — see platform stats at a glance
2. **User management** — view all students and instructors, activate or set-pending any of them
3. **User detail** — deep dive into a single user's profile, stats, recent submissions
4. **Problem list** — see all problems including inactive ones, toggle active/inactive
5. **Problem create/edit** — full form to add new problems or edit existing ones

### What Admins Cannot Do (by design)
- See or manage other admin accounts (admins are hidden from the users list)
- The admin panel has no access to student-facing features (problems list, code editor, etc.)

---

## Role System — Critical for Backend

The frontend already maps roles as follows. The backend **must** use these exact numbers:

| Role | Backend number | Frontend string |
|---|---|---|
| Student | `0` | `'student'` |
| Instructor | `1` | `'instructor'` |
| **Admin** | **`2`** | **`'admin'`** |

The login endpoint `POST /api/auth/login` must return `role: 2` for admin users. The frontend `AuthService` already maps this correctly and redirects admins to `/admin/overview` automatically.

**All `/api/admin/*` endpoints must require `[Authorize(Roles = "Admin")]`.**

If a non-admin tries to call an admin endpoint, return `403 Forbidden`.

---

## Global Response Conventions

All responses follow the existing envelope pattern already used by the backend:

```json
{
  "data": { ... }
}
```

Error responses:
```json
{
  "message": "Human-readable error description"
}
```

Standard HTTP codes: `200`, `201`, `400`, `401`, `403`, `404`, `409`, `500`.

---

## Backend Endpoints — Build in This Order

---

### 1. GET /api/admin/stats
**Priority: 🔴 Build first — this is the first thing admin sees after login**

Returns platform-wide statistics for the overview dashboard.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request:** No params needed.

**Response `200`:**
```json
{
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

**Notes:**
- `totalUsers` = students + instructors (do NOT count admin accounts)
- `pendingInstructors` = instructors where status = pending
- `newUsersToday` = registered today (UTC)
- `newUsersThisWeek` = registered in the last 7 days
- `submissionsToday` = submissions created today (UTC)

---

### 2. GET /api/admin/users
**Priority: 🔴 Build second — core feature**

Returns a paginated, filterable list of all users. **Admins are excluded from this list** — only students and instructors appear.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Query params:**

| Param | Type | Default | Notes |
|---|---|---|---|
| `search` | string | — | Filter by name OR email (case-insensitive contains) |
| `role` | `student` \| `instructor` | — | Filter by role. Omit = return both |
| `status` | `active` \| `pending` | — | Filter by status. Omit = return both |
| `sortBy` | `name` \| `registeredAt` \| `lastActiveAt` | `registeredAt` | Sort field |
| `sortDir` | `asc` \| `desc` | `desc` | Sort direction |
| `page` | number | `1` | Page number |
| `pageSize` | number | `20` | Items per page |

**Response `200`:**
```json
{
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
- `role` — return as number (`0` = student, `1` = instructor). Frontend maps it.
- `status` — `"active"` or `"pending"` as a string. Students are always `"active"`.
- `initials` — first letter of first name + first letter of last name, uppercase (e.g. "Karim Ahmed" → "KA"). Backend can generate this or frontend will derive it.
- `problemsSolved` — for students: count of accepted submissions. For instructors: `null`.
- `organization` — for instructors: their institution. For students: `null`.
- `lastActiveAt` — last time the user made any request (submission, login, etc.). `null` if never active after registration.

---

### 3. GET /api/admin/users/:id
**Priority: 🔴 Build alongside #2**

Returns full detail for a single user.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Response `200`:**
```json
{
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
    "avgScore": 92,
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
- `avgScore` — average score across all accepted submissions (0–100). `null` for instructors.
- `streak` — current daily streak. `null` for instructors.
- `totalSubmissions` — total submission count. `0` for instructors.
- `recentSubmissions` — last 5 submissions, newest first. Empty array `[]` for instructors.
- `recentSubmissions[].status` — same SubmissionStatus enum values as the existing submissions endpoint: `"Accepted"`, `"WrongAnswer"`, `"RuntimeError"`, etc.

**Response `404`:** User not found or is an admin (admins cannot be viewed via this endpoint).

---

### 4. PATCH /api/admin/users/:id/status
**Priority: 🔴 Build alongside #2 — core admin action**

Activates or sets a user to pending. Works for both students and instructors. Cannot be used on admin accounts.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body:**
```json
{ "status": "active" }
```
or
```json
{ "status": "pending" }
```

**Response `200`:** The full updated user object (same shape as `GET /api/admin/users/:id`).

**Response `400`:** Invalid status value (anything other than `"active"` or `"pending"`).

**Response `403`:** Target user is an admin — cannot change admin status.

**Response `404`:** User not found.

**Side effects:**
- If setting an instructor to `"active"` who was `"pending"` — this is the approval action. The backend should send the approval email at this point (see `INSTRUCTOR_APPROVAL_FLOW.md`).
- If setting a student to `"pending"` — they will not be able to log in (login endpoint should return `403` with `errorCode: "ACCOUNT_PENDING"` for pending accounts).

---

### 5. GET /api/admin/problems
**Priority: 🔴 Build alongside users — core feature**

Returns ALL problems including inactive ones. The existing `GET /api/problems` only returns active problems. This admin endpoint returns everything.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Query params:**

| Param | Type | Default | Notes |
|---|---|---|---|
| `search` | string | — | Filter by title (case-insensitive contains) |
| `difficulty` | `0` \| `1` \| `2` | — | Filter by difficulty number. Omit = all |
| `tag` | string | — | Filter by tag name. Omit = all |
| `isActive` | `true` \| `false` | — | Filter by active status. Omit = all |
| `sortBy` | `title` \| `difficulty` \| `solvedCount` \| `createdAt` | `createdAt` | Sort field |
| `sortDir` | `asc` \| `desc` | `desc` | Sort direction |
| `page` | number | `1` | Page number |
| `pageSize` | number | `20` | Items per page |

**Response `200`:**
```json
{
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

### 6. POST /api/problems
**Priority: 🔴 Build alongside #5**

Creates a new problem. This endpoint already exists in the API spec but needs to be implemented and restricted to admins.

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

**Validation (return `400` if violated):**
- `title`: required, min 3 chars, must be unique
- `difficulty`: required, must be `0`, `1`, or `2`
- `tags`: required, at least 1 tag
- `statement`: required, min 50 chars
- `sampleTestCases`: required, at least 1, each must have non-empty `input` and `expectedOutput`
- `timeLimitMs`: positive integer, default `2000` if omitted
- `memoryLimitMb`: positive integer, default `256` if omitted

**Response `201`:** The created problem in full detail (same shape as `GET /api/problems/:id`).

**Response `400`:** Validation failure — `{ "message": "..." }`.

**Response `409`:** Title already exists — `{ "message": "A problem with this title already exists." }`.

---

### 7. PATCH /api/problems/:id
**Priority: 🔴 Build alongside #6**

Updates an existing problem. All fields are optional — only send what changed (partial update).

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body** (all fields optional):
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

**Response `200`:** The updated problem in full detail.

**Response `400`:** Validation failure.

**Response `404`:** Problem not found.

---

### 8. PATCH /api/problems/:id/status
**Priority: 🟡 Medium — needed for the toggle in the problems list**

Toggles a problem's active/inactive status. Separated from PATCH so it's a clean single-purpose action.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Request body:**
```json
{ "isActive": false }
```

**Response `200`:** `{ "data": { "id": "uuid", "isActive": false } }`

**Response `404`:** Problem not found.

**Notes:**
- This is a soft operation — the problem record is kept, just hidden from students.
- Existing submissions to this problem are not affected.

---

### 9. DELETE /api/problems/:id
**Priority: 🟡 Medium — soft delete only**

Soft-deletes a problem. Sets `isActive = false` and marks it as deleted. The record is never physically removed from the database.

**Authorization:** `[Authorize(Roles = "Admin")]`

**Response `200`:** `{ "data": { "id": "uuid", "deleted": true } }`

**Response `404`:** Problem not found.

**Notes:**
- Do NOT hard-delete. Submissions reference problem IDs — deleting problems would break submission history.
- A soft-deleted problem behaves like inactive: not visible to students, not returned by `GET /api/problems`.
- It IS returned by `GET /api/admin/problems` with `isActive: false`.

---

## Frontend Files Changed (for backend team reference)

These are all the Angular files that were created or modified on the `admin-page` branch. The backend team does not need to touch these but should know what exists so they understand what each endpoint maps to.

### New files created

```
src/app/core/guards/admin.guard.ts                         ← blocks non-admins from /admin/**
src/app/features/admin/
├── admin.routes.ts                                        ← all admin routes
├── shell/
│   ├── admin-shell.component.ts/html/scss                 ← full-screen layout + sidebar
├── overview/
│   └── admin-overview.component.ts/html/scss              ← maps to GET /api/admin/stats
├── users/
│   └── admin-users.component.ts/html/scss                 ← maps to GET /api/admin/users
├── user-detail/
│   └── admin-user-detail.component.ts/html/scss           ← maps to GET /api/admin/users/:id
├── problems/
│   └── admin-problems.component.ts/html/scss              ← maps to GET /api/admin/problems
└── problem-form/
    └── admin-problem-form.component.ts/html/scss          ← maps to POST + PATCH /api/problems/:id
```

### Modified files

```
src/app/core/models/user.model.ts          ← role now includes 'admin'
src/app/core/utils/enum-mappers.ts         ← mapRole(2) → 'admin', roleToNumber('admin') → 2
src/app/core/services/auth.service.ts      ← isValidUser() accepts role === 'admin'
src/app/app.routes.ts                      ← added /admin lazy-loaded module
src/app/features/auth/login/login.component.ts  ← admin redirected to /admin/overview
```

---

## How the Frontend Will Wire Up Each Endpoint

Once each backend endpoint is ready, the frontend will replace the mock data with a real HTTP call. Here is exactly what that looks like for each feature:

### Overview stats
```typescript
// In admin-overview.component.ts — replace MOCK_STATS with:
this.http.get<{data: AdminStats}>(`${baseUrl}/admin/stats`, { headers })
  .pipe(map(r => r.data))
  .subscribe(stats => this.stats.set(stats));
```

### Users list
```typescript
// In admin-users.component.ts — replace MOCK_USERS with:
this.http.get<{data: {users: AdminUserRow[], total: number}}>
  (`${baseUrl}/admin/users`, { headers, params })
  .pipe(map(r => r.data.users))
  .subscribe(users => this.allUsers.set(users));
```

### User status toggle
```typescript
// In admin-users.component.ts + admin-user-detail.component.ts:
this.http.patch<{data: AdminUserRow}>
  (`${baseUrl}/admin/users/${id}/status`, { status: newStatus }, { headers })
  .subscribe(r => /* update signal */);
```

### Problems list
```typescript
// In admin-problems.component.ts — replace MOCK_PROBLEMS with:
this.http.get<{data: {problems: AdminProblemRow[], total: number}}>
  (`${baseUrl}/admin/problems`, { headers, params })
  .pipe(map(r => r.data.problems))
  .subscribe(problems => this.allProblems.set(problems));
```

### Problem create/edit
```typescript
// In admin-problem-form.component.ts — replace setTimeout mock with:
const url = isEdit
  ? `${baseUrl}/problems/${id}`
  : `${baseUrl}/problems`;
const method = isEdit ? 'patch' : 'post';
this.http[method]<{data: any}>(url, body, { headers })
  .subscribe({ next: () => router.navigate(['../../../problems']), error: ... });
```

---

## Build Priority Order for Backend Team

```
Phase 1 — Unblocks Overview + Users pages (build together)
├── GET  /api/admin/stats                  ← overview dashboard
├── GET  /api/admin/users                  ← users list
├── GET  /api/admin/users/:id              ← user detail
└── PATCH /api/admin/users/:id/status      ← activate/set-pending

Phase 2 — Unblocks Problem Management (build after Phase 1)
├── GET  /api/admin/problems               ← problems list (incl. inactive)
├── POST /api/problems                     ← create problem
└── PATCH /api/problems/:id               ← edit problem

Phase 3 — Nice to have (build after Phase 2)
├── PATCH /api/problems/:id/status         ← toggle active/inactive
└── DELETE /api/problems/:id              ← soft delete
```

---

## Endpoint Summary Table

All endpoints require `Authorization: Bearer <token>` header and `[Authorize(Roles = "Admin")]`.

| # | Method | Endpoint | Purpose | Phase |
|---|---|---|---|---|
| 1 | GET | `/api/admin/stats` | Overview dashboard stats | 1 |
| 2 | GET | `/api/admin/users` | User list with filters | 1 |
| 3 | GET | `/api/admin/users/:id` | Single user detail | 1 |
| 4 | PATCH | `/api/admin/users/:id/status` | Change user status | 1 |
| 5 | GET | `/api/admin/problems` | Problem list incl. inactive | 2 |
| 6 | POST | `/api/problems` | Create new problem | 2 |
| 7 | PATCH | `/api/problems/:id` | Edit existing problem | 2 |
| 8 | PATCH | `/api/problems/:id/status` | Toggle active/inactive | 3 |
| 9 | DELETE | `/api/problems/:id` | Soft delete problem | 3 |

---

## Testing the Frontend Right Now (Without Backend)

The admin panel is fully testable today using this browser console snippet:

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

All pages, filters, modals, and forms work with mock data. Once the backend is ready, each component will be updated to call real endpoints one at a time.

---

## Questions for the Backend Team

1. **User `status` field** — does the existing `Users` table have a `status` column (`active` / `pending`)? If not, it needs to be added as part of the instructor approval flow (see `INSTRUCTOR_APPROVAL_FLOW.md`).

2. **`initials` field** — should the backend compute and return this, or should the frontend derive it from `name`? Frontend can derive it — let us know your preference.

3. **`lastActiveAt` field** — does the backend currently track this? If not, this can be the last `updatedAt` on the user record or the last submission timestamp as a proxy.

4. **`avgScore` for students** — how is this calculated? Average score across all submissions, or only accepted ones?

5. **Problem `createdAt`** — is this already stored on the problem entity? The frontend sorts by it in the admin problems list.

6. **Admin seeding** — how will the first admin account be created? The register form only supports `student` and `instructor` roles. Admin accounts likely need to be seeded directly in the database or created via a separate admin-creation script.

---

*Document maintained by the Frontend team — August 14, 2026*
*Branch: `admin-page` | Base: `linking-backend-with-frontend`*
