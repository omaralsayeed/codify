# Instructor Approval Flow

> **Created:** August 14, 2026  
> **Status:** Planned — not yet implemented  
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

## Backend Team — What to Build

### 1. Add `status` field to the User/Instructor entity

```
status: 'active' | 'pending'
```

- Students always get `active` on register
- Instructors get `pending` on register
- Admin can change instructor status to `active`

---

### 2. Update `POST /api/auth/register`

- If `role = 1` (instructor) → create account with `status = pending`
- If `role = 0` (student) → create account with `status = active` (no change from current behavior)
- Response for instructor registration should still return `201` with the user object, but include the `status` field:

```json
{
  "data": {
    "userId": "uuid",
    "email": "instructor@example.com",
    "role": 1,
    "status": "pending"
  }
}
```

---

### 3. Update `POST /api/auth/login`

- If the user's status is `pending` → return `403` with:

```json
{
  "success": false,
  "message": "Your account is pending admin approval. Please check your email.",
  "errorCode": "ACCOUNT_PENDING"
}
```

- If status is `active` → login works as normal (no change)

---

### 4. New admin endpoints (build when admin page is ready)

```
GET    /api/admin/instructors/pending       — list all pending instructor requests
PATCH  /api/admin/instructors/:id/approve   — set status to active, trigger approval email
```

Both require `[Authorize(Roles = "Admin")]`.

---

### 5. Email notifications

Trigger emails at these points:

| Event | Recipient | Subject |
|---|---|---|
| Instructor registers | Instructor | "We received your request to join Codify" |
| Admin approves | Instructor | "Your Codify instructor account is approved" |
| New pending request | Admin | "New instructor registration request" (optional) |

---

## Frontend Team — What to Build

### 1. Update register flow for instructors

In `register.component.ts` — after successful register, check the `status` field in the response:

```typescript
// After register succeeds (201):
if (response.data.role === 1 && response.data.status === 'pending') {
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

- The admin role is separate from instructor. A user with `role = admin` (new role value, e.g. `2`) can access `/api/admin/*` routes.
- The admin page will live at `/admin/dashboard` — add a new guard `adminGuard` that checks `user.role === 'admin'`.
- The pending instructors list endpoint returns all users where `role = instructor` and `status = pending`.
- When approving, the backend sends the approval email AND sets status to `active` in one atomic operation — don't split these.
- The frontend admin page needs these views:
  - Pending requests list (name, email, organization, date registered, Approve button)
  - Approved instructors list
- Consider adding a `reviewedBy` and `reviewedAt` field to the instructor record for an audit trail of which admin approved.
- The `organization` field collected during registration (currently not sent to backend) becomes important here — the admin needs to see it to make the approval decision. **Make sure the backend stores it and returns it in the pending list.**

---

## Current State (as of August 14, 2026)

| Item | Status |
|---|---|
| Register form UI | ✅ Done |
| Student register → instant login | ✅ Done |
| Instructor register → pending flow | ❌ Not built |
| `/auth/pending-approval` page | ❌ Not built |
| Login error for pending accounts | ❌ Not built |
| Backend `status` field | ❌ Not built |
| Admin endpoints | ❌ Not built |
| Admin page | ❌ Not built (future sprint) |
| Email notifications | ❌ Not built |
