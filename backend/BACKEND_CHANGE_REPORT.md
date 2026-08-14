# Backend Change Report — Instructor Approval Flow

> **Date:** August 14, 2026  
> **Author:** Kiro (AI Dev Assistant)  
> **Feature:** Instructor Registration Approval Flow  
> **Status:** ✅ Complete and live

---

## The Story

The project already had a working registration and login system for both students and instructors. Everyone who registered could log in immediately — no gatekeeping, no approval process.

The problem: instructors should not be able to just sign up and start using the platform. An admin needs to review them first. Students should be completely unaffected.

The frontend team had already built their side — the pending-approval page, the ACCOUNT_PENDING error handler, and the status check on the register response. They were waiting on the backend to provide the right data and the right errors.

The backend had none of that. No status field, no approval gate, no admin endpoints. So everything was built from scratch in this session.

Here is exactly what changed, file by file, before and after.

---

## Files Changed

---

### 1. `src/Codify.Domain/Enums/UserStatus.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:**
```csharp
namespace Codify.Domain.Enums;

public enum UserStatus
{
    Active,
    Pending
}
```

**Why:** The system needed a type-safe way to represent whether a user account is active or waiting for approval. Using an enum keeps it consistent across the entire codebase and stored as a readable string in the database.

---

### 2. `src/Codify.Domain/Enums/UserRole.cs`
**Status: UPDATED**

**Before:**
```csharp
public enum UserRole
{
    Student,
    Instructor
}
```

**After:**
```csharp
public enum UserRole
{
    Student,
    Instructor,
    Admin
}
```

**Why:** The admin endpoints use `[Authorize(Roles = "Admin")]`. JWT role claims are based on this enum. Without `Admin` in the enum, the role claim would never match the policy and admin endpoints would always return 403.

---

### 3. `src/Codify.Domain/Entities/User.cs`
**Status: UPDATED**

**Before — fields:**
```csharp
public Guid Id { get; private set; }
public string FullName { get; private set; }
public string Email { get; private set; }
public string PasswordHash { get; private set; }
public UserRole Role { get; private set; }
public DateTime CreatedAt { get; private set; }
public DateTime? LastLoginAt { get; private set; }
// + Username, Bio, AvatarUrl, Rating, SolvedProblems, UpdatedAt, IsDeleted
```

**Before — Create method:**
```csharp
public static User Create(string fullName, string email, string passwordHash, UserRole role)
{
    return new User
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        Email = email,
        PasswordHash = passwordHash,
        Role = role,
        // ...
    };
}
```

**Before — no Approve method.**

---

**After — new fields added:**
```csharp
public UserStatus Status { get; private set; }
public string? Organization { get; private set; }
public Guid? ReviewedBy { get; private set; }
public DateTime? ReviewedAt { get; private set; }
```

**After — Create method updated:**
```csharp
public static User Create(string fullName, string email, string passwordHash, UserRole role, string? organization = null)
{
    // Instructors start as pending; students are immediately active
    var status = role == UserRole.Instructor ? UserStatus.Pending : UserStatus.Active;

    return new User
    {
        Id = Guid.NewGuid(),
        FullName = fullName,
        Email = email,
        PasswordHash = passwordHash,
        Role = role,
        Status = status,
        Organization = organization,
        // ...
    };
}
```

**After — new Approve method:**
```csharp
public void Approve(Guid approvedByAdminId)
{
    Status = UserStatus.Active;
    ReviewedBy = approvedByAdminId;
    ReviewedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
}
```

**Why:** The `User` entity is the source of truth. Status logic belongs here, not in services. The `Create()` factory sets status automatically based on role — no service code needs to know the rule. The `Approve()` method encapsulates the approval operation and stamps the audit trail in one place.

---

### 4. `src/Codify.Domain/Exceptions/PendingApprovalException.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:**
```csharp
namespace Codify.Domain.Exceptions;

/// <summary>
/// Thrown when a user with Pending status attempts to log in
/// before an admin has approved their account.
/// Maps to HTTP 403 with error code ACCOUNT_PENDING.
/// </summary>
public class PendingApprovalException(string message) : Exception(message);
```

**Why:** Following the existing pattern in the project (`NotFoundException`, `ForbiddenException`, `ValidationException`), a dedicated exception type makes the intent clear and allows the exception middleware to map it to the exact HTTP shape the frontend expects.

---

### 5. `src/Codify.Application/DTOs/Auth/RegisterRequest.cs`
**Status: UPDATED**

**Before:**
```csharp
public class RegisterRequest
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserRole Role { get; set; }
}
```

**After:**
```csharp
public class RegisterRequest
{
    public string FullName { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public UserRole Role { get; set; }

    [MaxLength(300)]
    public string? Organization { get; set; }  // NEW
}
```

**Why:** The admin needs to see the instructor's organization when reviewing a pending request. The frontend registration form collects this field. Without storing it on the backend, the admin would have no way to evaluate the request. Optional so students are not affected.

---

### 6. `src/Codify.Application/DTOs/Auth/RegisterResponse.cs`
**Status: UPDATED**

**Before:**
```csharp
public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; }
}
```

**After:**
```csharp
public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public UserRole Role { get; set; }
    public UserStatus Status { get; set; }  // NEW
}
```

**Why:** The frontend reads `status` from the register response to decide what to do next. If `role = 1` and `status = "Pending"`, it redirects to `/auth/pending-approval` instead of logging the user in. Without this field, the frontend has no signal and would try to log the instructor in, which would fail.

---

### 7. `src/Codify.Application/Services/AuthService.cs`
**Status: UPDATED**

**Before — RegisterAsync:**
```csharp
var user = User.Create(request.FullName, request.Email, passwordHash, request.Role);

return new RegisterResponse
{
    UserId = user.Id,
    Email = user.Email,
    Role = user.Role
    // no Status
};
```

**Before — LoginAsync:**
```csharp
if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    throw new ValidationException("Invalid email or password.");

user.RecordLogin();
await userRepo.SaveChangesAsync();
// issued token to everyone, including pending instructors
```

---

**After — RegisterAsync:**
```csharp
var user = User.Create(request.FullName, request.Email, passwordHash, request.Role, request.Organization);

return new RegisterResponse
{
    UserId = user.Id,
    Email = user.Email,
    Role = user.Role,
    Status = user.Status  // NEW
};
```

**After — LoginAsync:**
```csharp
if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    throw new ValidationException("Invalid email or password.");

// NEW — block pending instructors before issuing any token
if (user.Status == UserStatus.Pending)
    throw new PendingApprovalException("Your account is pending admin approval. Please check your email.");

user.RecordLogin();
await userRepo.SaveChangesAsync();
// token only issued from here if status is Active
```

**Why:** The critical gate. A pending instructor must never receive a JWT token. The check happens after password verification (so we don't leak whether an account exists) but before `RecordLogin()` and token generation.

---

### 8. `src/Codify.Application/Interfaces/IUserRepository.cs`
**Status: UPDATED**

**Before:** 4 methods — `GetByIdAsync`, `GetByEmailAsync`, `GetWithAnalyticsDataAsync`, `GetInstructorWithProblemsAndSubmissionsAsync`, `AddAsync`, `SaveChangesAsync`.

**After:** Added:
```csharp
/// <summary>Returns all instructors with Status = Pending, ordered by registration date.</summary>
Task<IReadOnlyList<User>> GetPendingInstructorsAsync();
```

**Why:** The admin service needs a way to fetch pending instructors. Adding the method to the interface keeps the Infrastructure layer properly abstracted from the Application layer.

---

### 9. `src/Codify.Application/Interfaces/IAdminService.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:**
```csharp
public interface IAdminService
{
    Task<IReadOnlyList<PendingInstructorResponse>> GetPendingInstructorsAsync();
    Task<ApproveInstructorResponse> ApproveInstructorAsync(Guid instructorId, Guid adminId);
}
```

**Why:** Follows the same service-interface pattern used throughout the project (`IAuthService`, `IProblemService`, etc.). The controller depends on the interface, not the concrete class, keeping things testable and decoupled.

---

### 10. `src/Codify.Application/Services/AdminService.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:** Implements `IAdminService`.
- `GetPendingInstructorsAsync()` — fetches pending instructors, maps to response DTOs
- `ApproveInstructorAsync(instructorId, adminId)` — loads instructor, validates role and current status, calls `instructor.Approve(adminId)`, saves

Guards included:
- 404 if instructor not found
- 400 if the target user is not an instructor
- 400 if already active (idempotency guard)

---

### 11. `src/Codify.Application/DTOs/Admin/PendingInstructorResponse.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:**
```csharp
public class PendingInstructorResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public string? Organization { get; set; }
    public DateTime RegisteredAt { get; set; }
}
```

**Why:** The admin page needs name, email, organization, and registration date to make an informed approval decision. This is exactly the data the future admin UI table will display.

---

### 12. `src/Codify.Application/DTOs/Admin/ApproveInstructorResponse.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:**
```csharp
public class ApproveInstructorResponse
{
    public Guid UserId { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public DateTime ApprovedAt { get; set; }
}
```

**Why:** Returns a confirmation of the approval with the timestamp, which the admin UI can show as feedback.

---

### 13. `src/Codify.Infrastructure/Repositories/UserRepository.cs`
**Status: UPDATED**

**Before:** 6 methods, no pending instructor query.

**After:** Added:
```csharp
public async Task<IReadOnlyList<User>> GetPendingInstructorsAsync() =>
    await db.Users
        .Where(u => u.Role == Domain.Enums.UserRole.Instructor
                 && u.Status == Domain.Enums.UserStatus.Pending)
        .OrderBy(u => u.CreatedAt)
        .ToListAsync();
```

**Why:** Implementation of the new interface method. Filters by both role and status, orders by registration date (oldest first — fairness for the admin review queue).

---

### 14. `src/Codify.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
**Status: UPDATED**

**Before:**
```csharp
builder.Property(u => u.Role).HasConversion<string>();
// no Status, Organization, ReviewedBy, ReviewedAt
```

**After:**
```csharp
builder.Property(u => u.Role).HasConversion<string>();
builder.Property(u => u.Status).HasConversion<string>().IsRequired().HasDefaultValue(Domain.Enums.UserStatus.Active);
builder.Property(u => u.Organization).HasMaxLength(300);
builder.Property(u => u.ReviewedBy);
builder.Property(u => u.ReviewedAt);
```

**Why:** EF Core needs explicit configuration for the new columns. `Status` is stored as a string (human-readable in the DB), is required, and defaults to `Active` — meaning all existing users in the database automatically stay active when the migration runs.

---

### 15. `src/Codify.Infrastructure/DependencyInjection.cs`
**Status: UPDATED**

**Before:**
```csharp
services.AddScoped<IAuthService, AuthService>();
// IAdminService not registered
```

**After:**
```csharp
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IAdminService, AdminService>();  // NEW
```

**Why:** Without this line, injecting `IAdminService` into `AdminController` would throw a runtime exception.

---

### 16. `src/Codify.API/Controllers/AdminController.cs`
**Status: NEW FILE**

**Before:** Did not exist.

**After:**
```csharp
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpGet("instructors/pending")]
    public async Task<IActionResult> GetPendingInstructors() { ... }

    [HttpPatch("instructors/{id:guid}/approve")]
    public async Task<IActionResult> ApproveInstructor(Guid id) { ... }
}
```

Both endpoints are protected by `[Authorize(Roles = "Admin")]` — only JWT tokens with the Admin role can reach them.

---

### 17. `src/Codify.API/Middleware/ExceptionMiddleware.cs`
**Status: UPDATED**

**Before:**
```csharp
var (statusCode, errorCode, message) = ex switch
{
    NotFoundException e      => (HttpStatusCode.NotFound,    "NOT_FOUND",       e.Message),
    ForbiddenException e     => (HttpStatusCode.Forbidden,   "FORBIDDEN",       e.Message),
    ValidationException e    => (HttpStatusCode.BadRequest,  "VALIDATION_ERROR", e.Message),
    _                        => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
};
```

**After:**
```csharp
var (statusCode, errorCode, message) = ex switch
{
    NotFoundException e         => (HttpStatusCode.NotFound,    "NOT_FOUND",        e.Message),
    ForbiddenException e        => (HttpStatusCode.Forbidden,   "FORBIDDEN",        e.Message),
    PendingApprovalException e  => (HttpStatusCode.Forbidden,   "ACCOUNT_PENDING",  e.Message),  // NEW
    ValidationException e       => (HttpStatusCode.BadRequest,  "VALIDATION_ERROR", e.Message),
    _                           => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
};
```

**Why:** When `AuthService.LoginAsync` throws `PendingApprovalException`, the middleware catches it and returns the exact JSON shape the frontend is looking for:
```json
{
  "success": false,
  "message": "Your account is pending admin approval. Please check your email.",
  "errorCode": "ACCOUNT_PENDING"
}
```

---

### 18. `src/Codify.Infrastructure/Persistence/Migrations/20260814141322_AddInstructorApprovalFlow.cs`
**Status: NEW FILE (generated by EF, Up() filled in manually)**

**Story of this file:** This was the hardest part of the session. The migration was initially written manually — but without a `.Designer.cs` companion file, EF didn't recognize it. When `dotnet ef migrations add` was run it generated a proper migration file, but with an empty `Up()` body — because the model snapshot had already been manually updated to match the new model, so EF thought no changes were needed. EF then recorded the empty migration as "applied" in `__EFMigrationsHistory`.

**Fix:** The `Up()` and `Down()` methods were filled in manually, the stale history entry was deleted directly from the database with:
```sql
DELETE FROM [__EFMigrationsHistory]
WHERE MigrationId = '20260814141322_AddInstructorApprovalFlow';
```
Then `dotnet ef database update` was run again, which executed the real SQL.

**What the migration adds to the `Users` table:**

| Column | Type | Nullable | Default |
|---|---|---|---|
| `Status` | `nvarchar(max)` | NOT NULL | `'Active'` |
| `Organization` | `nvarchar(300)` | NULL | — |
| `ReviewedBy` | `uniqueidentifier` | NULL | — |
| `ReviewedAt` | `datetime2` | NULL | — |

The `Active` default on `Status` means every existing user in the database keeps working with no data migration needed.

---

---

# 👤 For Admin — What Was Built for You

This section is specifically for whoever is building or using the Admin side of the platform. Everything below was built to support the admin approval workflow.

---

## Goal

The admin needs to be able to:
- See a list of instructors who registered and are waiting for approval
- Review their name, email, organization, and registration date
- Approve them so they can log in

The backend fully supports this. Two endpoints are ready and waiting for the admin page to be built.

---

## Your Endpoints

Both require an **Admin JWT token** (`Authorization: Bearer <token>`).

---

### Get all pending instructors

```
GET /api/admin/instructors/pending
```

Returns every instructor whose account is in `Pending` status, ordered by registration date (oldest first).

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Jane Smith",
      "email": "jane@university.edu",
      "organization": "Cairo University",
      "registeredAt": "2026-08-14T10:00:00Z"
    }
  ]
}
```

---

### Approve an instructor

```
PATCH /api/admin/instructors/{id}/approve
```

Sets the instructor's status to `Active`. After this, they can log in normally with no further steps.

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "fullName": "Jane Smith",
    "email": "jane@university.edu",
    "approvedAt": "2026-08-14T12:30:00Z"
  }
}
```

**Error cases:**
- `404` — instructor not found
- `400` — user is not an instructor, or account is already active

---

## What the Database Stores for Each Instructor

When an instructor registers, the database records:

| Field | Description |
|---|---|
| `FullName` | Their full name |
| `Email` | Their email address |
| `Organization` | The organization they submitted at registration (e.g. university name) |
| `Status` | `Pending` on register, changes to `Active` on approval |
| `CreatedAt` | When they registered |
| `ReviewedBy` | The admin's user ID who approved them (set on approval) |
| `ReviewedAt` | The timestamp of the approval (set on approval) |

The `ReviewedBy` and `ReviewedAt` fields form an audit trail — you can always know which admin approved which instructor and when.

---

## Files Built Specifically for Admin

| File | Purpose |
|---|---|
| `src/Codify.API/Controllers/AdminController.cs` | The two admin endpoints |
| `src/Codify.Application/Interfaces/IAdminService.cs` | Service contract |
| `src/Codify.Application/Services/AdminService.cs` | Business logic — fetch pending, approve |
| `src/Codify.Application/DTOs/Admin/PendingInstructorResponse.cs` | Shape of each instructor in the pending list |
| `src/Codify.Application/DTOs/Admin/ApproveInstructorResponse.cs` | Shape of the approval confirmation |
| `src/Codify.Domain/Enums/UserRole.cs` | Added `Admin` role value so JWT auth works |
| `src/Codify.Infrastructure/Repositories/UserRepository.cs` | Added DB query for pending instructors |

---

## What Still Needs to Be Done (Admin Side)

| Item | Notes |
|---|---|
| Admin page frontend | Build at `/admin/dashboard` — endpoints are ready |
| Admin auth guard | Check `user.role === 'Admin'` on the frontend route |
| Admin user in the database | No admin user exists yet — needs a DB insert or seed script to create the first admin account |
| Email on approval | When `PATCH .../approve` is called, an email should fire to the instructor — the injection point is ready in `AdminService.ApproveInstructorAsync` |

---

---

## New Files Summary

| File | Type |
|---|---|
| `src/Codify.Domain/Enums/UserStatus.cs` | New |
| `src/Codify.Domain/Exceptions/PendingApprovalException.cs` | New |
| `src/Codify.Application/Interfaces/IAdminService.cs` | New |
| `src/Codify.Application/Services/AdminService.cs` | New |
| `src/Codify.Application/DTOs/Admin/PendingInstructorResponse.cs` | New |
| `src/Codify.Application/DTOs/Admin/ApproveInstructorResponse.cs` | New |
| `src/Codify.API/Controllers/AdminController.cs` | New |
| `src/Codify.Infrastructure/Persistence/Migrations/20260814141322_AddInstructorApprovalFlow.cs` | New |

## Updated Files Summary

| File | What Changed |
|---|---|
| `src/Codify.Domain/Enums/UserRole.cs` | Added `Admin` value |
| `src/Codify.Domain/Entities/User.cs` | Added `Status`, `Organization`, `ReviewedBy`, `ReviewedAt` fields + `Approve()` method, updated `Create()` signature |
| `src/Codify.Application/DTOs/Auth/RegisterRequest.cs` | Added `Organization` field |
| `src/Codify.Application/DTOs/Auth/RegisterResponse.cs` | Added `Status` field |
| `src/Codify.Application/Interfaces/IUserRepository.cs` | Added `GetPendingInstructorsAsync()` |
| `src/Codify.Application/Services/AuthService.cs` | Passes `Organization` to `User.Create()`, returns `Status` in response, blocks pending login |
| `src/Codify.Infrastructure/Repositories/UserRepository.cs` | Implemented `GetPendingInstructorsAsync()` |
| `src/Codify.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | Added EF config for 4 new columns |
| `src/Codify.Infrastructure/DependencyInjection.cs` | Registered `IAdminService` |
| `src/Codify.API/Middleware/ExceptionMiddleware.cs` | Added `PendingApprovalException → 403 ACCOUNT_PENDING` mapping |
| `src/Codify.Infrastructure/Persistence/Migrations/CodifyDbContextModelSnapshot.cs` | Updated to reflect new schema |

---

## Behaviour Before vs After

### Registration

| Scenario | Before | After |
|---|---|---|
| Student registers | `status` not in response | `status: "Active"` in response |
| Instructor registers | `status` not in response | `status: "Pending"` in response |
| `organization` field sent | Ignored / not stored | Stored in database |

### Login

| Scenario | Before | After |
|---|---|---|
| Student logs in | ✅ Works | ✅ Still works, unchanged |
| Active instructor logs in | ✅ Works | ✅ Still works, unchanged |
| Pending instructor logs in | ✅ Got a token (wrong) | ❌ 403 `ACCOUNT_PENDING` — no token issued |

### Admin

| Endpoint | Before | After |
|---|---|---|
| `GET /api/admin/instructors/pending` | Did not exist (404) | ✅ Returns pending list |
| `PATCH /api/admin/instructors/:id/approve` | Did not exist (404) | ✅ Approves instructor |

---

## What Is Still Pending

| Item | Notes |
|---|---|
| Email notifications | Hooks exist in `AuthService.RegisterAsync` and `AdminService.ApproveInstructorAsync` — an email service just needs to be injected |
| Admin page (frontend) | Future sprint — endpoints are ready |
| Admin user seeding | No seeded admin user exists yet — currently needs a direct DB insert or a seed script |
