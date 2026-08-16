# Admin Panel — Frontend Integration Plan

> **Created:** August 16, 2026
> **Branch:** `admin-page`
> **Backend branch:** `admin-integrations`
> **Backend status:** ✅ All 9 endpoints live
> **Frontend status:** 🔄 Mock data — ready to wire up
> **Author:** Frontend team (Kiro)

---

## Context

The admin panel UI is fully built and working with mock/hardcoded data on the `admin-page` branch.
The backend has delivered all 9 admin endpoints on `admin-integrations`.
This document is our step-by-step plan to replace every mock with a real HTTP call — in the right order, safely.

---

## Key Things to Know Before We Start

### 1. Response envelope changed
The backend now wraps every response with a `success` field:
```json
{ "success": true, "data": { ... } }
```
Every HTTP call needs to map `r.data`, not just `r`.

### 2. Role comes back as a number
`role: 0` = student, `role: 1` = instructor.
We already have `mapRole()` in `enum-mappers.ts` — just need to call it everywhere in admin services.

### 3. Error shape
```json
{ "success": false, "errorCode": "NOT_FOUND", "message": "..." }
```
Error codes we handle: `NOT_FOUND`, `FORBIDDEN`, `CONFLICT`, `VALIDATION_ERROR`, `ACCOUNT_PENDING`.

### 4. Auth header
Every admin endpoint needs:
```
Authorization: Bearer <token>
```
Token is in `localStorage['codify_token']`.

### 5. Base URL
```
http://localhost:5237/api
```

---

## Sprint 1 — Foundation (do this before touching any component)

### Step 1.1 — Create `AdminService`

**File to create:** `src/app/core/services/admin.service.ts`

One service that holds:
- `baseUrl`
- `headers()` helper (reads token from localStorage)
- All HTTP methods for all 9 admin endpoints

**Why one service?** Keeps all admin HTTP logic in one place. Each component just calls `adminSvc.getStats()`, `adminSvc.getUsers()`, etc. — no HTTP code scattered across components.

**Methods to implement:**
```typescript
getStats(): Observable<AdminStats>
getUsers(filters): Observable<{ users: AdminUserRow[], total: number }>
getUserById(id): Observable<AdminUserDetail>
updateUserStatus(id, status): Observable<AdminUserDetail>
getProblems(filters): Observable<{ problems: AdminProblemRow[], total: number }>
createProblem(body): Observable<any>
updateProblem(id, body): Observable<any>
updateProblemStatus(id, isActive): Observable<{ id: string, isActive: boolean }>
deleteProblem(id): Observable<{ id: string, deleted: boolean }>
```

**Type mappings to apply inside the service:**
- `role: number` → `mapRole(n)` → `'student' | 'instructor'`
- `difficulty: number` → `mapDifficulty(n)` → `'easy' | 'medium' | 'hard'`

---

### Step 1.2 — Verify backend is reachable + admin JWT works

**Backend is running at:** `http://localhost:5237`
**Swagger UI:** `http://localhost:5237/swagger`

**Admin account is auto-seeded on startup — no manual DB work needed.**
Just pull `admin-integrations`, run the backend, and the admin account is inserted automatically on first start.

**Credentials:**
```
Email:    admin@codify.com
Password: Admin@123456
```

**Verification steps:**
1. Pull `admin-integrations` branch and start the backend — seed runs automatically
2. Hit `POST /api/auth/login` in Swagger:
   ```json
   { "email": "admin@codify.com", "password": "Admin@123456" }
   ```
3. Copy the token from the response
4. In Swagger: click **Authorize** → paste `Bearer <token>`
5. Hit `GET /api/admin/stats` — confirm you get back the stats shape
6. CORS is already configured for `http://localhost:4200` — Angular dev server works out of the box

**Do not proceed to Sprint 2 until step 5 is confirmed.**

---

## Sprint 2 — Wire Overview Page

**Component:** `src/app/features/admin/overview/admin-overview.component.ts`
**Endpoint:** `GET /api/admin/stats`

### Steps

**Step 2.1** — Inject `AdminService` and `HttpClient` into the component.

**Step 2.2** — Add loading and error signals:
```typescript
readonly isLoading = signal(true);
readonly error     = signal<string | null>(null);
```

**Step 2.3** — Replace `readonly stats = MOCK_STATS` with a real call in `ngOnInit`:
```typescript
ngOnInit(): void {
  this.adminSvc.getStats().subscribe({
    next: stats => {
      this.stats.set(stats);
      this.isLoading.set(false);
    },
    error: err => {
      this.error.set('Failed to load stats.');
      this.isLoading.set(false);
    }
  });
}
```

**Step 2.4** — Update the HTML to show a loading skeleton while `isLoading()` is true and an error state if `error()` is set.

**Step 2.5** — Remove `MOCK_STATS` constant from the component.

**Step 2.6** — Test: log in as admin, navigate to `/admin/overview`, verify real numbers show.

---

## Sprint 3 — Wire Users List

**Component:** `src/app/features/admin/users/admin-users.component.ts`
**Endpoints:** `GET /api/admin/users` + `PATCH /api/admin/users/:id/status`

### Steps

**Step 3.1** — Remove `MOCK_USERS` array from the component.

**Step 3.2** — Add loading and pagination signals:
```typescript
readonly isLoading   = signal(true);
readonly error       = signal<string | null>(null);
readonly serverTotal = signal(0);   // total from backend (for pagination later)
```

**Step 3.3** — Move filtering/sorting to backend params. The current component does all filtering client-side in memory. Switch to sending filters as query params and re-fetching:
```typescript
// When any filter changes → call loadUsers()
effect(() => {
  // reads searchQuery(), roleFilter(), statusFilter(), sortField(), sortDir()
  this.loadUsers();
});
```

**Step 3.4** — Implement `loadUsers()`:
```typescript
private loadUsers(): void {
  this.isLoading.set(true);
  this.adminSvc.getUsers({
    search:   this.searchQuery(),
    role:     this.roleFilter() !== 'all' ? this.roleFilter() : undefined,
    status:   this.statusFilter() !== 'all' ? this.statusFilter() : undefined,
    sortBy:   this.sortField(),
    sortDir:  this.sortDir(),
  }).subscribe({
    next: ({ users, total }) => {
      this.allUsers.set(users);  // already mapped by AdminService
      this.serverTotal.set(total);
      this.isLoading.set(false);
    },
    error: () => {
      this.error.set('Failed to load users.');
      this.isLoading.set(false);
    }
  });
}
```

**Step 3.5** — Wire the status toggle to the real endpoint:
```typescript
confirmChange(): void {
  const user   = this.confirmUser();
  const action = this.confirmAction();
  if (!user || !action) return;

  const newStatus = action === 'activate' ? 'active' : 'pending';
  this.adminSvc.updateUserStatus(user.id, newStatus).subscribe({
    next: () => this.loadUsers(),   // re-fetch to get fresh data
    error: err => {
      if (err.error?.errorCode === 'FORBIDDEN') {
        this.error.set('Cannot change status of an admin account.');
      } else {
        this.error.set('Failed to update status.');
      }
    }
  });
  this.closeModal();
}
```

**Step 3.6** — Add loading skeleton to the HTML.

**Step 3.7** — Test: filters, sort, activate/set-pending all work with real data.

---

## Sprint 4 — Wire User Detail

**Component:** `src/app/features/admin/user-detail/admin-user-detail.component.ts`
**Endpoints:** `GET /api/admin/users/:id` + `PATCH /api/admin/users/:id/status`

### Steps

**Step 4.1** — Remove `MOCK_DETAILS` object from the component.

**Step 4.2** — Add loading signal. Replace `ngOnInit` mock lookup with a real call:
```typescript
ngOnInit(): void {
  const id = this.route.snapshot.paramMap.get('id') ?? '';
  this.isLoading.set(true);
  this.adminSvc.getUserById(id).subscribe({
    next: user => {
      this.user.set(user);
      this.isLoading.set(false);
    },
    error: err => {
      if (err.status === 404) this.user.set('not-found');
      else this.error.set('Failed to load user.');
      this.isLoading.set(false);
    }
  });
}
```

**Step 4.3** — Wire the status toggle button to the real endpoint:
```typescript
confirmChange(): void {
  const action = this.confirmAction();
  const current = this.user();
  if (!action || !current || current === 'not-found') return;

  const newStatus = action === 'activate' ? 'active' : 'pending';
  this.adminSvc.updateUserStatus(current.id, newStatus).subscribe({
    next: updated => this.user.set(updated),
    error: err => {
      if (err.error?.errorCode === 'FORBIDDEN') {
        this.error.set('Cannot change status of an admin account.');
      }
    }
  });
  this.closeModal();
}
```

**Step 4.4** — Test: clicking a user row in the list loads real data. Activate/set-pending updates the status badge live.

---

## Sprint 5 — Wire Problems List

**Component:** `src/app/features/admin/problems/admin-problems.component.ts`
**Endpoints:** `GET /api/admin/problems` + `PATCH /api/problems/:id/status` + `DELETE /api/problems/:id`

### Steps

**Step 5.1** — Remove `MOCK_PROBLEMS` array from the component.

**Step 5.2** — Add loading signal + `loadProblems()` method (same pattern as users):
```typescript
private loadProblems(): void {
  this.isLoading.set(true);
  this.adminSvc.getProblems({
    search:     this.searchQuery() || undefined,
    difficulty: this.difficultyFilter() !== 'all'
                  ? difficultyToNumber(this.difficultyFilter() as Difficulty)
                  : undefined,
    tag:        this.tagFilter() !== 'all' ? this.tagFilter() : undefined,
    isActive:   this.statusFilter() === 'all'
                  ? undefined
                  : this.statusFilter() === 'active',
    sortBy:     this.sortField(),
    sortDir:    this.sortDir(),
  }).subscribe({
    next: ({ problems }) => {
      this.allProblems.set(problems);
      this.isLoading.set(false);
    },
    error: () => this.isLoading.set(false)
  });
}
```

**Step 5.3** — Wire the activate/deactivate toggle:
```typescript
confirmToggle(): void {
  const p = this.confirmProblem();
  if (!p) return;
  this.adminSvc.updateProblemStatus(p.id, !p.isActive).subscribe({
    next: () => this.loadProblems(),
    error: () => this.error.set('Failed to update problem status.')
  });
  this.closeModal();
}
```

**Step 5.4** — Wire the delete button (add delete button to the table row actions):
```typescript
deleteProblem(id: string): void {
  this.adminSvc.deleteProblem(id).subscribe({
    next: () => this.allProblems.update(list => list.filter(p => p.id !== id)),
    error: () => this.error.set('Failed to delete problem.')
  });
}
```

**Step 5.5** — Note: difficulty comes back as a number from the backend. `AdminService` should map it via `mapDifficulty()` before returning to the component.

**Step 5.6** — Test: filters work, toggle active/inactive, delete removes from list.

---

## Sprint 6 — Wire Problem Form (Create + Edit)

**Component:** `src/app/features/admin/problem-form/admin-problem-form.component.ts`
**Endpoints:** `POST /api/problems` + `PATCH /api/problems/:id`

### Steps

**Step 6.1** — Remove the `setTimeout` mock submit and `MOCK_PROBLEMS` lookup from the component.

**Step 6.2** — For **edit mode**, load real problem data in `ngOnInit`:
```typescript
// Edit mode — load real problem from backend
this.adminSvc.getProblemById(id).subscribe({
  next: problem => this.populateForm(problem),
  error: () => this.error.set('Problem not found.')
});
```
> Note: This requires adding a `getProblemById(id)` method to `AdminService` that calls the existing `GET /api/problems/:id` endpoint (not an admin endpoint — uses the regular problems endpoint).

**Step 6.3** — Replace `onSubmit()` mock with real HTTP:
```typescript
onSubmit(): void {
  this.touched.set(true);
  if (!this.isValid()) return;

  this.isSubmitting.set(true);
  const f = this.form();
  const body = {
    title:          f.title.trim(),
    difficulty:     difficultyToNumber(f.difficulty as Difficulty),
    tags:           f.tags,
    statement:      f.statement.trim(),
    constraints:    f.constraints.trim(),
    sampleTestCases: f.testCases,
    isActive:       f.isActive,
    timeLimitMs:    f.timeLimitMs,
    memoryLimitMb:  f.memoryLimitMb,
  };

  const call$ = this.isEditMode()
    ? this.adminSvc.updateProblem(this.problemId()!, body)
    : this.adminSvc.createProblem(body);

  call$.subscribe({
    next: () => {
      this.isSubmitting.set(false);
      this.submitSuccess.set(true);
      setTimeout(() => this.router.navigate(['../../problems'], { relativeTo: this.route }), 1200);
    },
    error: err => {
      this.isSubmitting.set(false);
      if (err.error?.errorCode === 'CONFLICT') {
        this.formError.set('A problem with this title already exists.');
      } else if (err.status === 400) {
        this.formError.set(err.error?.message || 'Validation error. Check your fields.');
      } else {
        this.formError.set('Something went wrong. Please try again.');
      }
    }
  });
}
```

**Step 6.4** — Add a `formError` signal to display server-side errors below the submit button.

**Step 6.5** — Test: create a new problem, verify it appears in the problems list. Edit an existing problem, verify changes persist. Try duplicate title — verify the conflict error shows.

---

## Completion Checklist

| Step | Task | Status |
|---|---|---|
| 1.1 | Create `AdminService` | ✅ Done |
| 1.2 | Verify backend reachable + admin JWT works | ✅ Done — `admin@codify.com` / `Admin@123456` |
| 2 | Wire overview stats | ✅ Done |
| 3 | Wire users list + status toggle | ✅ Done |
| 4 | Wire user detail + status toggle | ✅ Done |
| 5 | Wire problems list + toggle + delete | ❌ |
| 6 | Wire problem form create + edit | ❌ |

---

## Error Handling Reference

Handle these consistently across all components:

| Scenario | What to show |
|---|---|
| Network error / backend down | "Could not reach the server. Please try again." |
| `404 NOT_FOUND` | "Not found." or redirect to list |
| `403 FORBIDDEN` | "You don't have permission to do this." |
| `409 CONFLICT` | "A problem with this title already exists." |
| `400 VALIDATION_ERROR` | Show `err.error.message` inline |
| Loading state | Skeleton rows or spinner |

---

## Notes

- Do **not** remove the mock data until the real call is verified working in the browser.
- Wire one sprint at a time — test each before moving to the next.
- The `AdminService` is the only file that imports `HttpClient`. Components only call service methods.
- `mapRole()` and `mapDifficulty()` from `enum-mappers.ts` must be applied inside `AdminService` before data reaches the components.

---

*Plan created by Frontend team — August 16, 2026*
