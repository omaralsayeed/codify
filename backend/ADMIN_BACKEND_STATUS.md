# Admin Backend — Current Status & Sprint Plan

> **Branch:** `admin-integrations`
> **Date:** August 16, 2026
> **Team:** Backend
> **Goal:** Deliver all 9 endpoints described in `ADMIN_PAGE_SPEC.md` so the frontend can replace its mock data with real API calls.

---

## Quick Reference — The 9 Endpoints We Must Ship

| # | Method | Endpoint | Phase | Status |
|---|---|---|---|---|
| 1 | GET | `/api/admin/stats` | 1 | ✅ Done |
| 2 | GET | `/api/admin/users` | 1 | ✅ Done |
| 3 | GET | `/api/admin/users/:id` | 1 | ✅ Done |
| 4 | PATCH | `/api/admin/users/:id/status` | 1 | ✅ Done |
| 5 | GET | `/api/admin/problems` | 2 | ❌ Not built |
| 6 | POST | `/api/problems` | 2 | ⚠️ Exists but wrong role |
| 7 | PATCH | `/api/problems/:id` | 2 | ⚠️ Exists as PUT, wrong role |
| 8 | PATCH | `/api/problems/:id/status` | 3 | ❌ Not built |
| 9 | DELETE | `/api/problems/:id` | 3 | ⚠️ Exists but wrong role |

---

## What Already Exists (and Is Usable)

### ✅ Fully Ready — No Changes Needed

| Asset | Location | Notes |
|---|---|---|
| `UserRole` enum | `Domain/Enums/UserRole.cs` | Student=0, Instructor=1, Admin=2 — matches spec exactly |
| `UserStatus` enum | `Domain/Enums/UserStatus.cs` | Active, Pending — matches spec exactly |
| `User` entity | `Domain/Entities/User.cs` | Has `FullName`, `Email`, `Role`, `Status`, `Organization`, `CreatedAt`, `LastLoginAt`, `SolvedProblems`, `IsDeleted` — all fields we need |
| `User.Approve()` | `Domain/Entities/User.cs` | Now a wrapper around `SetStatus(Active)` — existing approval flow unchanged |
| `Problem` entity | `Domain/Entities/Problem.cs` | Has `IsActive`, `IsDeleted`, `CreatedAt`, `AcceptedSubmissionsCount`, `TotalSubmissionsCount`, `TimeLimitMs`, `MemoryLimitMb` — all fields we need |
| `Problem.SoftDelete()` | `Domain/Entities/Problem.cs` | Sets `IsDeleted=true`, `IsActive=false` |
| `Problem.Deactivate()` | `Domain/Entities/Problem.cs` | Sets `IsActive=false`, `IsPublic=false` |
| `AdminController` | `API/Controllers/AdminController.cs` | Already has `[Authorize(Roles = "Admin")]` — just needs new endpoints added |
| `IAdminService` / `AdminService` | `Application/Interfaces` + `Services` | Instructor approval logic is done — we extend this |
| `ProblemService.CreateAsync()` | `Application/Services/ProblemService.cs` | Fully implemented — just needs role fix in controller |
| `ProblemService.UpdateAsync()` | `Application/Services/ProblemService.cs` | Implemented — needs to change from PUT → PATCH + role fix |
| `ProblemService.DeleteAsync()` | `Application/Services/ProblemService.cs` | Soft delete implemented — just needs role fix |
| `Login` pending-account block | `Application/Services/AuthService.cs` | Already throws `PendingApprovalException` for pending users — spec requirement already met |
| DI registrations | `Infrastructure/DependencyInjection.cs` | `IAdminService`, `IProblemService`, all repos already registered |

### ⚠️ Exists But Needs Changes

| Asset | Problem | Fix Needed |
|---|---|---|
| `POST /api/problems` | `[Authorize(Roles = "Instructor")]` — only instructors can create | Change to `[Authorize(Roles = "Admin")]` |
| `PUT /api/problems/:id` | (a) Wrong HTTP verb — spec wants `PATCH`, (b) `[Authorize(Roles = "Instructor")]` | Change verb to `PATCH`, change role to `Admin` |
| `DELETE /api/problems/:id` | `[Authorize(Roles = "Instructor")]` | Change to `[Authorize(Roles = "Admin")]` |
| `ProblemFilterRequest` | Has `Difficulty`, `Tag`, `Search`, `Page`, `PageSize` but missing `sortBy`, `sortDir`, `isActive` | Add those 3 fields for admin filter |
| `ProblemRepository.GetAllAsync()` | Filters out inactive problems unless `isInstructor=true`. Admin needs ALL problems including soft-deleted | Add an admin-specific query method |
| `UpdateProblemRequest` | Missing `IsActive`, `TimeLimitMs`, `MemoryLimitMb`, `SampleTestCases` fields | Add missing fields + update `Problem.Update()` to accept them |
| `ProblemSummaryResponse` | Missing `solvedCount`, `totalSubmissions`, `createdAt` fields | Add those fields |

---

## What Is Completely Missing

### New DTOs needed (none of these exist yet)

| DTO | Purpose |
|---|---|
| `AdminStatsResponse` | Response shape for `GET /api/admin/stats` |
| `AdminUserRow` | Single user row in paginated list for `GET /api/admin/users` |
| `AdminUserDetailResponse` | Full user detail with recent submissions for `GET /api/admin/users/:id` |
| `AdminUserSubmissionRow` | The 5 recent submissions nested in `AdminUserDetailResponse` |
| `UpdateUserStatusRequest` | Request body for `PATCH /api/admin/users/:id/status` → `{ "status": "active" }` |
| `AdminProblemRow` | Problem row for `GET /api/admin/problems` — includes `isActive`, `solvedCount` |
| `AdminProblemFilterRequest` | Filter params for admin problems list (includes `isActive`, sortBy/sortDir) |
| `AdminUserFilterRequest` | Filter params for admin users list (search, role, status, sortBy, sortDir, page) |
| `ProblemStatusUpdateRequest` | Request body for `PATCH /api/problems/:id/status` → `{ "isActive": false }` |

### New repository methods needed (none exist yet)

| Method | Repository | Why |
|---|---|---|
| `GetAdminUsersAsync(AdminUserFilterRequest)` | `IUserRepository` | Paginated, filterable user list — excludes admins |
| `GetByIdWithRecentSubmissionsAsync(Guid)` | `IUserRepository` | User detail with last 5 submissions + problem title |
| `SetStatusAsync(Guid, UserStatus)` via `User.SetStatus()` | Domain + `IUserRepository` | Generic status setter — `Approve()` only does Active, we need to also set Pending |
| `GetAdminProblemsAsync(AdminProblemFilterRequest)` | `IProblemRepository` | All problems incl. inactive, with sort + filters |
| `GetSubmissionsCountTodayAsync()` | `ISubmissionRepository` | For `/api/admin/stats` |
| `GetNewUsersCountAsync(DateTime from)` | `IUserRepository` | For `/api/admin/stats` (today + this week) |

### New domain method needed

| Method | Entity | Why |
|---|---|---|
| `User.SetStatus(UserStatus status)` | `User.cs` | ✅ DONE — added `SetStatus(UserStatus, Guid)`, `Approve()` refactored as wrapper |

---

## Known Problems & Their Solutions

### Problem 1 — Creating problems is currently Instructor-only
**Root cause:** `ProblemsController.Create()` has `[Authorize(Roles = "Instructor")]`.
**Solution:** Change to `[Authorize(Roles = "Admin")]`. Instructors lose create access by design — the spec says problem management is admin-only.

### Problem 2 — Update problem is a PUT, spec wants PATCH
**Root cause:** `ProblemsController.Update()` uses `[HttpPut("{id:guid}")]`. The spec requires `PATCH /api/problems/:id` for partial updates.
**Solution:** Change attribute to `[HttpPatch("{id:guid}")]`. The `UpdateProblemRequest` already uses nullable fields so partial update is already supported in the service.

### Problem 3 — `UpdateProblemRequest` is incomplete
**Root cause:** It only has Title, Statement, Difficulty, Constraints, LanguageSupport, TagIds. Missing `IsActive`, `TimeLimitMs`, `MemoryLimitMb`, `SampleTestCases`.
**Solution:** Add missing fields to `UpdateProblemRequest` and update `Problem.Update()` signature to apply them.

### Problem 4 — No paginated user list query exists
**Root cause:** `IUserRepository` only has `GetPendingInstructorsAsync()` for listing. No general paginated query.
**Solution:** Add `GetAdminUsersAsync(AdminUserFilterRequest filter)` to interface + implementation in `UserRepository.cs`.

### Problem 5 — `lastActiveAt` field isn't tracked properly
**Root cause:** `User` entity has `LastLoginAt` (set on login) but spec wants `lastActiveAt` which should be last submission or last login, whichever is more recent.
**Solution:** Use `LastLoginAt` as a proxy for now. If we want to be precise, we can compute `MAX(LastLoginAt, last SubmittedAt)` in the repository query by joining with Submissions. **Decision: use `LastLoginAt` for Phase 1, document as known limitation.**

### Problem 6 — `GET /api/admin/problems` doesn't exist; existing `GET /api/problems` filters inactive
**Root cause:** `ProblemRepository.GetAllAsync()` applies `WHERE IsActive = 1` for non-instructors. Admin needs everything.
**Solution:** Add `GetAdminProblemsAsync(AdminProblemFilterRequest)` — a separate query that never filters by `IsActive` (but can optionally filter by it when the frontend passes `isActive=true/false`).

### Problem 7 — No `User.SetStatus()` method — can only Approve, not set-to-Pending ✅ SOLVED
**Root cause:** `User.Approve()` existed but there was no reverse operation.
**Solution:** Added `User.SetStatus(UserStatus status, Guid adminId)` to `User.cs`. Refactored `Approve()` as a one-line wrapper: `=> SetStatus(UserStatus.Active, approvedByAdminId)`. Existing approval flow is completely unchanged.

### Problem 8 — Stats endpoint needs cross-table aggregation but no service method exists
**Root cause:** Platform stats (total users, submissions today, etc.) require queries across `Users`, `Submissions`, `Problems` tables. Nothing like this exists.
**Solution:** Add `GetAdminStatsAsync()` to `IAdminService` and implement it. It can use `IUserRepository`, `ISubmissionRepository`, `IProblemRepository` in one service method. All simple `COUNT` queries.

---

## Sprint Plan

---

### Sprint 1 — User Management Endpoints (Phase 1)
**Goal:** Unblock the Overview and Users pages. Deliver 4 endpoints.

#### Task 1.1 — Domain layer: add `User.SetStatus()` ✅ DONE
- File: `src/Codify.Domain/Entities/User.cs`
- Added `SetStatus(UserStatus status, Guid adminId)` — sets Status, records ReviewedBy + ReviewedAt + UpdatedAt
- Refactored `Approve()` to be a one-line convenience wrapper around `SetStatus(Active)`
- Effort: ~10 min

#### Task 1.2 — New DTOs: Admin user responses ✅ DONE
- Created `src/Codify.Application/DTOs/Admin/AdminStatsResponse.cs`
- Created `src/Codify.Application/DTOs/Admin/AdminUserRow.cs`
- Created `src/Codify.Application/DTOs/Admin/AdminUserDetailResponse.cs`
- Created `src/Codify.Application/DTOs/Admin/AdminUserSubmissionRow.cs`
- Created `src/Codify.Application/DTOs/Admin/AdminUserFilterRequest.cs`
- Created `src/Codify.Application/DTOs/Admin/UpdateUserStatusRequest.cs`
- Effort: ~20 min

#### Task 1.3 — Repository layer: new user queries ✅ DONE
- `IUserRepository` extended with `GetAdminUsersAsync`, `GetByIdWithRecentSubmissionsAsync`, `GetNewUsersCountAsync`
- `UserRepository` implements all 3 — EF split queries, filter/sort/page logic, excludes admins and deleted users
- `ISubmissionRepository` extended with `GetCountFromAsync`
- `SubmissionRepository` implements it — single `CountAsync` with date filter
- `IProblemRepository` extended with `GetTotalCountAsync`
- `ProblemRepository` implements it
- Effort: ~45 min

#### Task 1.4 — Repository layer: submissions count for stats ✅ DONE
- `ISubmissionRepository.GetCountFromAsync(DateTime from)` added (covered in Task 1.3 above)
- Effort: ~15 min

#### Task 1.5 — Service layer: extend `IAdminService` + `AdminService` ✅ DONE
- `IAdminService` extended with `GetStatsAsync`, `GetUsersAsync`, `GetUserByIdAsync`, `UpdateUserStatusAsync`
- `AdminService` constructor updated to inject `ISubmissionRepository` + `IProblemRepository`
- All 4 methods implemented including `ComputeInitials` and `ComputeStreak` helpers
- Effort: ~60 min

#### Task 1.6 — Controller layer: add 4 endpoints to `AdminController` ✅ DONE
- `GET /api/admin/stats` → `GetStats()`
- `GET /api/admin/users` → `GetUsers([FromQuery] AdminUserFilterRequest)`
- `GET /api/admin/users/{id}` → `GetUserById(Guid id)`
- `PATCH /api/admin/users/{id}/status` → `UpdateUserStatus(Guid id, [FromBody] UpdateUserStatusRequest)`
- Effort: ~30 min

**Sprint 1 Deliverables:** 4 endpoints live. Frontend Overview + Users pages fully functional.

---

### Sprint 2 — Problem Management Endpoints (Phase 2)
**Goal:** Unblock the Problems list and Create/Edit form. Deliver 3 endpoints + fix 3 existing ones.

#### Task 2.1 — Fix existing problem endpoints — role + verb
- File: `src/Codify.API/Controllers/ProblemsController.cs`
  - `POST /api/problems` → change `[Authorize(Roles = "Instructor")]` to `[Authorize(Roles = "Admin")]`
  - `PUT /api/problems/:id` → change to `PATCH`, change role to `Admin`
  - `DELETE /api/problems/:id` → change role to `Admin`
- Effort: ~5 min

#### Task 2.2 — Extend `UpdateProblemRequest` + `Problem.Update()`
- File: `src/Codify.Application/DTOs/Problems/UpdateProblemRequest.cs`
  - Add `bool? IsActive`, `int? TimeLimitMs`, `int? MemoryLimitMb`, `List<SampleTestCaseInput>? SampleTestCases`
- File: `src/Codify.Domain/Entities/Problem.cs`
  - Extend `Update()` or add `SetActive(bool)` method to handle `IsActive` toggle
  - Add `UpdateLimits(int timeLimitMs, int memoryLimitMb)` 
- Effort: ~20 min

#### Task 2.3 — New DTOs: Admin problem responses
- Create `src/Codify.Application/DTOs/Admin/AdminProblemRow.cs`
- Create `src/Codify.Application/DTOs/Admin/AdminProblemFilterRequest.cs`
- Effort: ~15 min

#### Task 2.4 — Repository layer: admin problems query
- File: `src/Codify.Application/Interfaces/IProblemRepository.cs`
  - Add `GetAdminProblemsAsync(AdminProblemFilterRequest filter)`
- File: `src/Codify.Infrastructure/Repositories/ProblemRepository.cs`
  - Implement: no `IsActive` filter by default, supports `isActive` param, supports sortBy/sortDir
  - Never returns `IsDeleted = true` records (those are truly gone from UI)
- Effort: ~30 min

#### Task 2.5 — Service layer: admin problem methods in `IAdminService`
- File: `src/Codify.Application/Interfaces/IAdminService.cs`
  - Add `GetAdminProblemsAsync(AdminProblemFilterRequest filter)`
- File: `src/Codify.Application/Services/AdminService.cs`
  - Implement — maps `Problem` → `AdminProblemRow`
- Effort: ~25 min

#### Task 2.6 — Controller layer: add `GET /api/admin/problems`
- File: `src/Codify.API/Controllers/AdminController.cs`
  - Add `GET /api/admin/problems`
- Effort: ~15 min

**Sprint 2 Deliverables:** Problems list page live. Create + Edit forms live.

---

### Sprint 3 — Polish & Remaining Endpoints (Phase 3)
**Goal:** Problem status toggle + soft delete. Final cleanup.

#### Task 3.1 — `PATCH /api/problems/:id/status`
- New DTO: `ProblemStatusUpdateRequest` — `{ IsActive: bool }`
- New service method on `IProblemService`: `SetActiveAsync(Guid id, bool isActive)`
- Implement in `ProblemService`: calls `problem.Deactivate()` or re-activates
- Add to `ProblemsController` with `[Authorize(Roles = "Admin")]`
- **Note:** Need to add `Activate()` method to `Problem` entity (currently only `Deactivate()` exists)
- Effort: ~25 min

#### Task 3.2 — `DELETE /api/problems/:id` role fix (from Sprint 2)
- Already covered in Task 2.1 — just confirm it's done.
- Effort: 0 min (done in Sprint 2)

#### Task 3.3 — `initials` field decision
- The spec asks for `initials` (e.g., "Karim Ahmed" → "KA") in user list responses.
- The `User` entity has no `Initials` property. Frontend said they can derive it.
- **Decision: compute it in the mapping in `AdminService`, don't store in DB.**
- Implementation: `string.Concat(name.Split(' ').Take(2).Select(w => char.ToUpper(w[0])))`
- Effort: ~5 min (just the mapping logic)

#### Task 3.4 — Verify login `ACCOUNT_PENDING` response format
- Spec says login must return `403` with `{ "errorCode": "ACCOUNT_PENDING" }` for pending users.
- Currently `AuthService` throws `PendingApprovalException` which returns a generic `{ "message": "..." }`.
- Check `ExceptionMiddleware` — if it doesn't return `errorCode`, update it to include it for this specific exception type.
- Effort: ~15 min

#### Task 3.5 — Verify admin is excluded from `/api/admin/users`
- Double-check `GetAdminUsersAsync` filters `Role != Admin`.
- Verify `GET /api/admin/users/:id` returns `404` if user is an admin.
- Effort: ~10 min (code review + test)

**Sprint 3 Deliverables:** All 9 endpoints complete. Every admin panel page fully operational.

---

## File Change Summary (All Sprints)

```
Domain/
├── Entities/
│   ├── User.cs                         → ADD SetStatus() method
│   └── Problem.cs                      → ADD Activate() method, extend Update()

Application/
├── DTOs/Admin/
│   ├── AdminStatsResponse.cs           → NEW
│   ├── AdminUserRow.cs                 → NEW
│   ├── AdminUserDetailResponse.cs      → NEW
│   ├── AdminUserSubmissionRow.cs       → NEW
│   ├── AdminUserFilterRequest.cs       → NEW
│   ├── UpdateUserStatusRequest.cs      → NEW
│   ├── AdminProblemRow.cs              → NEW
│   └── AdminProblemFilterRequest.cs    → NEW
├── DTOs/Problems/
│   └── UpdateProblemRequest.cs         → EXTEND (add IsActive, TimeLimitMs, MemoryLimitMb)
├── Interfaces/
│   ├── IAdminService.cs                → EXTEND (add 5 new methods)
│   ├── IUserRepository.cs              → EXTEND (add 3 new methods)
│   ├── IProblemRepository.cs           → EXTEND (add 1 new method)
│   └── ISubmissionRepository.cs        → EXTEND (add GetCountTodayAsync)
└── Services/
    └── AdminService.cs                 → EXTEND (implement 5 new methods)

Infrastructure/
└── Repositories/
    ├── UserRepository.cs               → EXTEND (implement 3 new methods)
    ├── ProblemRepository.cs            → EXTEND (implement 1 new method)
    └── SubmissionRepository.cs         → EXTEND (add GetCountTodayAsync)

API/
└── Controllers/
    ├── AdminController.cs              → EXTEND (add 5 new endpoints)
    └── ProblemsController.cs           → FIX roles + verb on 3 existing endpoints, add PATCH status
```

---

## Answered Questions from the Spec

The frontend asked 6 questions. Here are our answers:

1. **`status` column** — ✅ Yes. `UserStatus` enum + `Status` column already exist on the `Users` table, added as part of the instructor approval flow.

2. **`initials` field** — Backend will compute it in the mapping layer (no DB column needed). Format: first letter of each word in `FullName`, max 2 letters, uppercase.

3. **`lastActiveAt`** — We'll use `LastLoginAt` as a proxy. This field is already updated by `User.RecordLogin()` on every login. Future improvement: take `MAX(LastLoginAt, lastSubmissionAt)`.

4. **`avgScore` for students** — Computed as the average of `Score` across all non-deleted submissions for the user. `Score` is stored per-submission (0–100) and already calculated in `Submission.MarkAsAccepted()`.

5. **`createdAt` on problems** — ✅ Already stored as `Problem.CreatedAt`.

6. **Admin seeding** — Admin accounts must be seeded directly in the database. The registration form intentionally excludes Admin role. We'll add a data seed script or EF seed in `CodifyDbContext` for the initial admin account.

---

*Maintained by: Backend team | Branch: `admin-integrations` | August 16, 2026*
