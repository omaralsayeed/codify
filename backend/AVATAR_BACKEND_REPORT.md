# Backend Report — Profile Avatar Persistence

> **From:** Backend Team  
> **To:** Frontend Team  
> **Date:** August 16, 2026  
> **Feature:** Cloudinary Profile Photo — Backend Persistence Layer  
> **Status:** ✅ Complete and live

---

## What This Solves

The frontend already uploads photos directly to Cloudinary and gets back a `secure_url`.  
The problem was that the URL was only stored in `localStorage` keyed by `userId` — meaning it was invisible on any other device or browser, and gone the moment the user cleared their cache.

The backend now stores the Cloudinary URL in the database and returns it on every login. The `localStorage` fallback you already built stays in place and works as a fast-load cache — the backend value just takes priority over it.

**No migration was needed.** The `AvatarUrl` column (`nvarchar(500)`, nullable) was already in the `Users` table from a previous migration (`AlignWithErDiagram`, April 2026). It was just never wired to any endpoint.

---

## What Changed on the Backend

### Files touched

| File | Change |
|---|---|
| `src/Codify.Application/DTOs/Auth/LoginResponse.cs` | Added `AvatarUrl` to `LoginUserInfo` |
| `src/Codify.Application/DTOs/Auth/UpdateAvatarDto.cs` | **New file** — request body for the PUT endpoint |
| `src/Codify.Application/Interfaces/IAuthService.cs` | Added `UpdateAvatarUrlAsync` method signature |
| `src/Codify.Application/Services/AuthService.cs` | Mapped `AvatarUrl` in login + implemented `UpdateAvatarUrlAsync` |
| `src/Codify.API/Controllers/AuthController.cs` | Added `PUT /api/auth/avatar` endpoint |

---

### 1. Login response now includes `avatarUrl`

`POST /api/auth/login` — response shape **updated**.

The `user` object inside the login response now has one new optional field:

```json
{
  "data": {
    "token": "eyJhbGci...",
    "expiresAt": "2026-08-17T10:00:00Z",
    "user": {
      "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "fullName": "Jane Doe",
      "role": 0,
      "avatarUrl": "https://res.cloudinary.com/mg7dsqv2/image/upload/v1/codify_avatars/abc123.jpg"
    }
  }
}
```

- `avatarUrl` is `null` / absent for users who have never uploaded a photo.
- `avatarUrl` is the full Cloudinary `secure_url` for users who have.

---

### 2. New endpoint — `PUT /api/auth/avatar`

```
PUT /api/auth/avatar
Authorization: Bearer <token>   ← required
Content-Type: application/json
```

**Request body:**
```json
{
  "avatarUrl": "https://res.cloudinary.com/mg7dsqv2/image/upload/v1/codify_avatars/abc123.jpg"
}
```

**Response `200`:**
```json
{ "data": null }
```

**Response `400` — invalid URL:**
```json
{
  "errorCode": "INVALID_AVATAR_URL",
  "message": "avatarUrl must be a valid Cloudinary URL."
}
```

**Validation rules applied server-side:**
- `avatarUrl` is required and cannot be empty
- Max length: 500 characters
- Must be a valid URL (enforced by `[Url]` data annotation)
- Must start with `https://res.cloudinary.com/` (enforced in the controller — returns `400 INVALID_AVATAR_URL` if not)
- Must be a JWT-authenticated request — returns `401` if no token

**What the endpoint does:**
1. Reads `userId` from the JWT claims
2. Loads the user from the database
3. Saves the URL to the `AvatarUrl` column (the user's existing `Bio` value is preserved — only the avatar changes)
4. Returns `200`

---

## What You Need to Change on the Frontend

Two small changes. Both are in `AuthService`.

---

### Change 1 — Read `avatarUrl` from the login response

Your `LoginApiResponse` interface needs one new optional field:

```typescript
// In your login response interface (wherever you have it)
user: {
  userId: string;
  fullName: string;
  role: number;
  avatarUrl?: string;   // ← ADD THIS
}
```

Then in `login()`, prefer the backend value and fall back to localStorage (which you already have):

```typescript
const storedAvatarUrl =
  loginData.user.avatarUrl ??                                        // ← backend first (cross-device)
  localStorage.getItem(`codify_avatar_${loginData.user.userId}`) ?? // ← localStorage fallback
  undefined;

const user: User = {
  id: loginData.user.userId,
  name: loginData.user.fullName,
  role: loginData.user.role === 0 ? 'student' : 'instructor',
  avatarInitials: this.getInitials(loginData.user.fullName),
  streak: 0,
  avatarUrl: storedAvatarUrl   // ← pass it through
};
```

This is purely additive — nothing else in your login flow changes.

---

### Change 2 — Fix the `PUT` URL in `setAvatarUrl()`

Your current implementation fires `PUT /api/users/avatar`.  
**The correct URL is `PUT /api/auth/avatar`** — the endpoint lives in `AuthController`, not a separate `UsersController`.

```typescript
// Change this:
this.http.put(`${this.baseUrl}/users/avatar`, { avatarUrl: url }, ...)

// To this:
this.http.put(`${this.baseUrl}/auth/avatar`, { avatarUrl: url }, ...)
```

That's the only URL change. Headers, body shape, and fire-and-forget behavior all stay exactly as you had them.

---

## Complete Updated Flow

```
User picks photo in register form
          ↓
Angular uploads directly to Cloudinary
          ↓
Cloudinary returns secure_url
          ↓
setAvatarUrl(secure_url) fires:
  • live signal patched → navbar shows photo immediately ✅
  • saved to localStorage as fast-load cache
  • PUT /api/auth/avatar → saved in DB ✅
          ↓
User logs out → logs back in on any device
  • login response includes avatarUrl from DB
  • photo restored everywhere ✅
```

---

## Error Cases to Handle

| Scenario | Backend response | Suggested frontend behaviour |
|---|---|---|
| `PUT /api/auth/avatar` with non-Cloudinary URL | `400 INVALID_AVATAR_URL` | Log warning, swallow silently — UI already shows photo from signal |
| `PUT /api/auth/avatar` with expired token | `401` | Same as any other 401 — your interceptor handles it |
| `PUT /api/auth/avatar` fails (network/server) | Any 5xx | Already a fire-and-forget — your `console.warn` on error is fine |
| Login for user with no photo | `avatarUrl` is absent from response | Falls through to localStorage fallback, then no avatar — correct behaviour |

---

## What You Do NOT Need to Change

- Cloudinary config, upload logic, preset name — untouched
- Register flow in `register.component.ts` — already calls `setAvatarUrl()`, nothing to change
- Navbar template and SCSS — already wired for `avatarUrl`
- Profile page — already reads `avatarUrl` from the auth signal
- Logout logic — `codify_avatar_<userId>` is still intentionally kept on logout
- Any other service, endpoint, or component

---

## One Thing We Need From You

**When a user changes their photo after registration** (an "Edit Profile" upload flow — not yet built), the call is the same `PUT /api/auth/avatar`. The backend will overwrite the old URL with the new one. When you build that flow, no backend changes are needed — just call the same endpoint.

Let us know when that page is being built and we can confirm nothing else is needed.

---

*For any response shape that doesn't match what's documented here, share the exact request and response and we'll look at it immediately.*
