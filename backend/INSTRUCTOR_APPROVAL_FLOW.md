# Instructor Approval Flow

> **Created:** August 14, 2026  
> **Last Updated:** August 14, 2026  
> **Status:** Backend complete — Frontend pending  
> **Admin page:** Not built yet (future sprint)

---

## Overview

When a user registers as an **instructor**, they do not get immediate access. Their account is created with a `pending` status and they receive a confirmation email. An admin must approve the request before the instructor can log in. Students are unaffected — they register and log in instantly as before.

Only two statuses exist:
- `pending` — default for all new instructor registrations
- `active` — set by admin after approval

---

## User Flow

### Instructor registers
1. Fills out the register form and selects role = Instructor
2. Submits the form
3. Backend creates the account with status = `pending`
4. Frontend shows a "waiting for approval" screen instead of redirecting to the dashboard
5. Instructor receives an email: "Your request has been received. You will be notified once approved."

### Admin approves (future admin page)
1. Admin logs in to the admin panel
2. Sees a list of pending instructor requests
3. Clicks Approve
4. Instructor status changes to `active`
5. Instructor receives an email: "Your account has been approved. You can now log in."

### Instructor tries to log in
- Status `pending` → backend returns error → frontend shows: "Your account is pending admin approval. Please check your email."
- Status `active` → login succeeds normally

---

## Backend — What Was Built

### Files changed or created

#### Domain layer

**`src/Codify.Domain/Enums/UserStatus.cs`** ✅ NEW
```csharp
public enum UserStatus { Active, Pending }
```

**`src/Codify.Domain/Enums/UserRole.cs`** ✅ UPDATED  
Added `Admin` value so the `[Authorize(Roles = "Admin")]` attribute on admin endpoints works correctly with JWT role claims.
```csharp
public enum UserRole { Student, Instructor, Admin }
```

**`src/Codify.Domain/Entities/User.cs`** ✅ UPDATED  
New fields added:
- `UserStatus Status` — auto-set in `Create()`: `Instructor → Pending`, `Student/Admin → Active`
- `string? Organization` — stored at registration, shown to admin during review
- `Guid? ReviewedBy` — which admin approved this account
- `DateTime? ReviewedAt` — when the approval happened

New method added:
- `Approve(Guid approvedByAdminId)` — sets `Status = Active`, stamps `ReviewedBy` and `ReviewedAt`

`Create()` signature updated to accept optional `organization`:
```csharp
public static User Create(string fullName, string email, string passwordHash, UserRole role, string? organization = null)
```

**`src/Codify.Domain/Exceptions/PendingApprovalException.cs`** ✅ NEW  
Domain exception thrown when a pending instructor tries to log in. Maps to `HTTP 403 / ACCOUNT_PENDING` via the exception middleware.

---

#### Application layer

**`src/Codify.Application/DTOs/Auth/RegisterRequest.cs`** ✅ UPDATED  
Added optional `Organization` field (`[MaxLength(300)]`). Sent by frontend during instructor registration.

**`src/Codify.Application/DTOs/Auth/RegisterResponse.cs`** ✅ UPDATED  
Added `UserStatus Status` field. Frontend reads this to decide whether to redirect to dashboard or `/auth/pending-approval`.

**`src/Codify.Application/DTOs/Admin/PendingInstructorResponse.cs`** ✅ NEW
```csharp
public class PendingInstructorResponse {
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string? Organization { get; set; }
    public DateTime RegisteredAt { get; set; }
}
```

**`src/Codify.Application/DTOs/Admin/ApproveInstructorResponse.cs`** ✅ NEW
```csharp
public class ApproveInstructorResponse {
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public DateTime ApprovedAt { get; set; }
}
```

**`src/Codify.Application/Interfaces/IUserRepository.cs`** ✅ UPDATED  
Added:
```csharp
Task<IReadOnlyList<User>> GetPendingInstructorsAsync();
```

**`src/Codify.Application/Interfaces/IAdminService.cs`** ✅ NEW
```csharp
public interface IAdminService {
    Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync();
    Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId);
}
```

**`src/Codify.Application/Services/AuthService.cs`** ✅ UPDATED  
Two changes:
1. `RegisterAsync` — passes `request.Organization` to `User.Create()` and returns `Status` in the response
2. `LoginAsync` — checks `user.Status == UserStatus.Pending` before issuing a token. Throws `PendingApprovalException` if pending. No token is ever issued to a pending user.

**`src/Codify.Application/Services/AdminService.cs`** ✅ NEW  
Implements `IAdminService`:
- `GetPendingInstructorsAsync()` — fetches pending instructors from repo, maps to response DTOs
- `ApproveInstructorAsync(instructorId, adminId)` — loads instructor, validates role and current status, calls `instructor.Approve(adminId)`, saves

---

#### Infrastructure layer

**`src/Codify.Infrastructure/Persistence/Configurations/UserConfiguration.cs`** ✅ UPDATED  
Added EF Core configuration for the new columns:
```csharp
builder.Property(u => u.Status).HasConversion<string>().IsRequired().HasDefaultValue(UserStatus.Active);
builder.Property(u => u.Organization).HasMaxLength(300);
builder.Property(u => u.ReviewedBy);
builder.Property(u => u.ReviewedAt);
```

**`src/Codify.Infrastructure/Repositories/UserRepository.cs`** ✅ UPDATED  
Implemented `GetPendingInstructorsAsync()`:
```csharp
db.Users
    .Where(u => u.Role == UserRole.Instructor && u.Status == UserStatus.Pending)
    .OrderBy(u => u.CreatedAt)
    .ToListAsync();
```

**`src/Codify.Infrastructure/DependencyInjection.cs`** ✅ UPDATED  
Registered the new service:
```csharp
services.AddScoped<IAdminService, AdminService>();
```

**`src/Codify.Infrastructure/Persistence/Migrations/20260814000000_AddInstructorApprovalFlow.cs`** ✅ NEW  
Migration that adds four columns to the `Users` table:

| Column | Type | Nullable | Default |
|---|---|---|---|
| `Status` | `nvarchar(max)` | No | `'Active'` |
| `Organization` | `nvarchar(300)` | Yes | — |
| `ReviewedBy` | `uniqueidentifier` | Yes | — |
| `ReviewedAt` | `datetime2` | Yes | — |

> `Status` defaults to `Active` so all existing users in the database are unaffected by this migration.

`CodifyDbContextModelSnapshot.cs` was also updated to reflect these four new columns.

---

#### API layer

**`src/Codify.API/Controllers/AdminController.cs`** ✅ NEW  
```csharp
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
```

Endpoints:
- `GET /api/admin/instructors/pending` — returns all pending instructor requests
- `PATCH /api/admin/instructors/{id}/approve` — approves a pending instructor

**`src/Codify.API/Middleware/ExceptionMiddleware.cs`** ✅ UPDATED  
Added `PendingApprovalException` handling:
```csharp
PendingApprovalException e => (HttpStatusCode.Forbidden, "ACCOUNT_PENDING", e.Message),
```
This produces the exact `403` shape the frontend expects:
```json
{
  "success": false,
  "message": "Your account is pending admin approval. Please check your email.",
  "errorCode": "ACCOUNT_PENDING"
}
```

---

### Contract — `POST /api/auth/register`

**Request** (instructor example):
```json
{
  "fullName": "Jane Smith",
  "email": "jane@university.edu",
  "password": "SecurePass1",
  "role": 1,
  "organization": "Cairo University"
}
```

**Response `201`** (instructor):
```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "email": "jane@university.edu",
    "role": 1,
    "status": "Pending"
  }
}
```

**Response `201`** (student — `role: 0`):
```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "email": "student@example.com",
    "role": 0,
    "status": "Active"
  }
}
```

---

### Contract — `POST /api/auth/login` (pending instructor)

**Response `403`**:
```json
{
  "success": false,
  "message": "Your account is pending admin approval. Please check your email.",
  "errorCode": "ACCOUNT_PENDING"
}
```

---

### Contract — `GET /api/admin/instructors/pending`

Requires: `Authorization: Bearer <admin-token>`

**Response `200`**:
```json
{
  "success": true,
  "data": [
    {
      "userId": "uuid",
      "fullName": "Jane Smith",
      "email": "jane@university.edu",
      "organization": "Cairo University",
      "registeredAt": "2026-08-14T10:00:00Z"
    }
  ]
}
```

---

### Contract — `PATCH /api/admin/instructors/{id}/approve`

Requires: `Authorization: Bearer <admin-token>`

**Response `200`**:
```json
{
  "success": true,
  "data": {
    "userId": "uuid",
    "fullName": "Jane Smith",
    "email": "jane@university.edu",
    "approvedAt": "2026-08-14T12:30:00Z"
  }
}
```

**Response `404`** — instructor not found  
**Response `400`** — user is not an instructor, or account is already active

---

## Frontend Team — What to Build

### 1. Update register flow for instructors

In `register.component.ts` — after successful register, check the `status` field in the response:

```typescript
// After register succeeds (201):
if (response.data.role === 1 && response.data.status === 'Pending') {
  // Don't log them in — show the pending screen instead
  this.router.navigate(['/auth/pending-approval']);
} else {
  // Student — log in and redirect as normal
  this.login(email, password);
}
```

In `auth.service.ts` — update the register method to read `status` from the 201 response and return it in `AuthResult`:

```typescript
// Add to AuthResult interface:
export interface AuthResult {
  success: boolean;
  error?: string;
  user?: User;
  pendingApproval?: boolean; // true when instructor registers and is pending
}
```

> **Note on casing:** The backend returns `status` as `"Pending"` / `"Active"` (PascalCase, matching the C# enum string conversion). Make sure the frontend comparison matches this exactly.

---

### 2. New page: `/auth/pending-approval`

Create `src/app/features/auth/pending-approval/` with a simple component:

- Heading: "Request Received"
- Message: "Your instructor account request is pending review. We'll send you an email once an admin approves your account."
- Icon: clock / hourglass
- Link back to login page
- No auth guard needed (public route)

Add to `auth.routes.ts`:
```typescript
{ path: 'pending-approval', component: PendingApprovalComponent }
```

---

### 3. Update login error handling

In `auth.service.ts` login `catchError` — handle the new error code:

```typescript
catchError(error => {
  const errorCode = error?.error?.errorCode;

  if (errorCode === 'ACCOUNT_PENDING') {
    return of({ success: false, error: 'Your account is pending admin approval. Please check your email.' });
  }

  // Default
  return of({ success: false, error: error?.error?.message || 'Invalid email or password' });
})
```

---

### 4. Update `AuthResult` interface

```typescript
// src/app/core/models/auth.model.ts
export interface AuthResult {
  success: boolean;
  error?: string;
  user?: User;
  pendingApproval?: boolean; // true when instructor registers and is pending
}
```

---

## Notes for When the Admin Page is Built

- The admin role is separate from instructor. A user with `role = Admin` (value `2` in the enum) can access `/api/admin/*` routes.
- The admin page will live at `/admin/dashboard` — add a new guard `adminGuard` that checks `user.role === 'Admin'`.
- The pending instructors list endpoint returns all users where `role = Instructor` and `status = Pending`.
- When approving, the backend sets status to `active` in one operation — the `Approve()` method on the `User` entity handles both status and audit fields atomically.
- The frontend admin page needs these views:
  - Pending requests list (name, email, organization, date registered, Approve button)
  - Approved instructors list
- `reviewedBy` and `reviewedAt` are already stored in the database. The approval response returns `approvedAt` for immediate confirmation.
- The `organization` field is already stored in the database and returned by the pending list endpoint. No additional backend work needed for this.

---

## Email Notifications — Not yet implemented

Planned email triggers:

| Event | Recipient | Subject |
|---|---|---|
| Instructor registers | Instructor | "We received your request to join Codify" |
| Admin approves | Instructor | "Your Codify instructor account is approved" |
| New pending request | Admin | "New instructor registration request" (optional) |

An email service needs to be wired in. The `AdminService.ApproveInstructorAsync` and `AuthService.RegisterAsync` are the two injection points — both already have the user's email available at the moment the email should fire.

---

## To Apply the Migration

Stop the running API, then run:

```bash
dotnet ef database update --project src/Codify.Infrastructure --startup-project src/Codify.API
```

---

## Current State (as of August 14, 2026)

| Item | Status |
|---|---|
| Register form UI | ✅ Done (frontend) |
| Student register → instant login | ✅ Done |
| `UserStatus` enum | ✅ Done (backend) |
| `status` field on User entity | ✅ Done (backend) |
| `organization` field on User entity | ✅ Done (backend) |
| Approval audit fields (`reviewedBy`, `reviewedAt`) | ✅ Done (backend) |
| EF migration for new columns | ✅ Done (backend) |
| `POST /api/auth/register` returns `status` | ✅ Done (backend) |
| Instructor register → `status = pending` | ✅ Done (backend) |
| Student register → `status = active` | ✅ Done (backend) |
| `POST /api/auth/login` blocks pending accounts | ✅ Done (backend) |
| `403 ACCOUNT_PENDING` error response | ✅ Done (backend) |
| `GET /api/admin/instructors/pending` | ✅ Done (backend) |
| `PATCH /api/admin/instructors/:id/approve` | ✅ Done (backend) |
| Instructor register → pending flow (frontend) | ❌ Not built |
| `/auth/pending-approval` page | ❌ Not built |
| Login error handling for `ACCOUNT_PENDING` | ❌ Not built |
| Admin page | ❌ Not built (future sprint) |
| Email notifications | ❌ Not built |
