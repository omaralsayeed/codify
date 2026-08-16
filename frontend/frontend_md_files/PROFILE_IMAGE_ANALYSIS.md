# Profile Image — Current State, Problems & Upload Solutions

---

## 1. What Existed Before (Original Broken State)

### Register Page
- A file input (`<input type="file" accept="image/jpeg,image/png,image/webp">`) lets the user pick a photo before signing up.
- `onProfilePictureSelect()` reads the file with `FileReader` and stores it as a base64 data URL in the component variable `profilePicturePreview`.
- This preview is shown inside the register form (the circle above the fields).
- After a **successful registration**, this line ran:
  ```typescript
  if (this.profilePicturePreview) {
    localStorage.setItem('codify_avatar', this.profilePicturePreview);
  }
  ```
  The base64 image was written to `localStorage` under the key `codify_avatar`. That was the **only persistence** — no upload to any server happened.

### Profile Page (`/profile/:username`)
- On `ngOnInit`, the component read `localStorage.getItem('codify_avatar')` into `savedAvatar`.
- If `savedAvatar` was not null, the template showed `<img [src]="savedAvatar">`.
- Otherwise it showed a colored circle with the user's **avatar initials** (e.g., "JS").

### Navbar / Header (all pages after login)
- **Both desktop and mobile** avatars only showed `avatarInitials`:
  ```html
  <div class="nav-avatar">{{ auth.user()?.avatarInitials }}</div>
  ```
- The profile dropdown large avatar also used `avatarInitials`.
- **No image was shown anywhere in the navbar — ever.**

### User Model
```typescript
export interface User {
  id: string;
  name: string;
  email: string;
  role: 'student' | 'instructor';
  avatarInitials: string;   // ← only initials, no image URL field
  streak?: number;
  username?: string;
  joinedAt?: string;
}
```
There was **no `profileImageUrl` or `avatarUrl` field** in the User interface.

### Auth Service — Login & Session Restore
- On login the API response only returned `{ userId, fullName, role }` — no image URL.
- The `User` object built from the login response had no image field.
- On page refresh, `restoreSession()` read the user from `localStorage['codify_user']` — also no image field.

---

## 2. Why the Photo Was Not Working / Not Appearing

| # | Problem | Root Cause |
|---|---------|------------|
| 1 | **Image never uploaded to any server** | `onSubmit()` called `authService.register(...)` with only text fields. The file was never included in the HTTP request. The backend never received it. |
| 2 | **Image stored as raw base64 in localStorage** | Base64 bloats the image size by ~33%. A 300 KB photo becomes ~400 KB of text in localStorage (5–10 MB limit). On any other device or browser it was completely invisible. |
| 3 | **Navbar always showed initials, never the image** | The navbar template read `auth.user()?.avatarInitials`. There was no conditional `<img>` tag. Even if `codify_avatar` was in localStorage, the navbar would never display it. |
| 4 | **No `profileImageUrl` field in User model or auth service** | Even if the backend were to return an image URL, there was no field in the `User` interface to store it, and the auth service did not map it. |
| 5 | **`codify_avatar` was NOT cleared on logout** | `auth.logout()` removed `codify_user` and `codify_token` but not `codify_avatar`. A different user logging in on the same browser would see the previous user's photo. |
| 6 | **Profile page only loaded image from localStorage** | The profile page never called the backend to fetch a profile image URL. It only checked `localStorage`, so the feature only half-worked on the same browser, same device — never shared. |
| 7 | **Auto-login after register did not re-read localStorage** | After register, the app called `login()` internally. The login response built a fresh `User` object — it did not read `codify_avatar`. So the navbar showed no image right after registration. |
| 8 | **Logout/login cycle wiped the URL** | `login()` always rebuilt the User from the API response and overwrote `codify_user` in localStorage — losing the Cloudinary URL that `setAvatarUrl()` had saved. |

---

## 3. Best Free Options for Hosting Profile Images

### Option A — Cloudinary ✅ (Chosen)

**Why it fits best:**
- Purpose-built for image hosting. No backend storage code needed.
- Free tier: **25 GB storage + 25 GB bandwidth/month** — more than enough for a learning platform.
- Returns a permanent HTTPS URL you simply save as a string.
- Built-in on-the-fly transformations (auto crop to face, resize, compress, WebP conversion) via URL parameters — no extra code.
- Has a direct browser upload API (`unsigned upload`) so Angular uploads directly without routing through the backend.

**How it works here:**
```
Register form picks file
    ↓
Angular calls Cloudinary Upload API
    ↓
Cloudinary returns { secure_url: "https://res.cloudinary.com/..." }
    ↓
URL stored in localStorage keyed by userId
    ↓
User signal patched → photo appears in navbar + profile instantly
```

---

### Option B — AWS S3 + CloudFront

- AWS Free Tier gives **5 GB S3 storage + 15 GB outbound transfer/month for 12 months**.
- After 12 months the free tier expires.
- Requires more setup: S3 bucket policy, IAM user/role, CORS config, presigned URLs.
- **Verdict:** More complex, free tier expires. Good if already on AWS.

---

### Option C — Supabase Storage

- Free tier: **1 GB storage + 2 GB bandwidth/month** (smaller than Cloudinary).
- **Verdict:** Free tier is tight for images. Good only if already on Supabase.

---

### Option D — Firebase Storage (Google)

- Free Spark plan: **5 GB storage + 1 GB/day download**.
- **Verdict:** 1 GB/day download cap can be hit easily. More setup overhead than Cloudinary.

---

### Comparison Table

| Service | Free Storage | Free Bandwidth | Setup Effort | Best For |
|---------|-------------|----------------|--------------|----------|
| **Cloudinary** ✅ | 25 GB | 25 GB/mo | Low | This project |
| AWS S3 | 5 GB (12 mo) | 15 GB/mo | High | Already on AWS |
| Supabase | 1 GB | 2 GB/mo | Medium | Already on Supabase |
| Firebase | 5 GB | 1 GB/day | Medium | Already on Firebase |

---

## 4. Cloudinary Dashboard Setup (Done)

| Setting | Value |
|---------|-------|
| Cloud name | `mg7dsqv2` |
| Upload preset | `MS_codify-imgs` |
| Signing mode | Unsigned (Angular uploads directly from the browser) |
| Asset folder | `codify_avatars` |

---

## 5. What We Implemented (Frontend + Cloudinary Only — No Backend Changes)

### The Strategy
Store the Cloudinary URL in localStorage **keyed by `userId`** (`codify_avatar_<userId>`). On every login, the auth service reads this key using the `userId` returned by the API and merges `avatarUrl` back into the User object. The URL survives logout/login cycles without the backend ever touching it.

---

### `src/app/core/models/user.model.ts`
Added `avatarUrl?: string`:
```typescript
export interface User {
  ...
  avatarInitials: string;
  avatarUrl?: string;  // Cloudinary URL — set after photo upload
  ...
}
```

---

### `src/app/core/services/auth.service.ts`

**`login()`** — re-hydrates the Cloudinary URL on every login:
```typescript
const storedAvatarUrl =
  localStorage.getItem(`codify_avatar_${loginData.user.userId}`) ?? undefined;

const user: User = {
  id: loginData.user.userId,
  ...
  avatarUrl: storedAvatarUrl,  // restored from localStorage by userId
};
```

**`setAvatarUrl(url)`** — new method, called after a successful Cloudinary upload:
```typescript
setAvatarUrl(url: string): void {
  const current = this._currentUser();
  if (!current) return;
  const updated: User = { ...current, avatarUrl: url };
  this._currentUser.set(updated);
  localStorage.setItem(`codify_avatar_${current.id}`, url); // keyed by userId
  localStorage.setItem('codify_user', JSON.stringify(updated));
}
```

**`logout()`** — clears session keys but keeps the avatar key so it survives:
```typescript
logout(): void {
  localStorage.removeItem('codify_user');
  localStorage.removeItem('codify_token');
  // codify_avatar_<userId> intentionally kept — re-read on next login
  this._currentUser.set(null);
}
```

---

### `src/app/features/auth/register/register.component.ts`
- Added `HttpClient` injection and Cloudinary constants.
- `onProfilePictureSelect()` now stores the raw `File` object alongside the base64 preview.
- `onSubmit()` two-step flow:
  1. If a file was selected → `POST multipart/form-data` to Cloudinary → get `secure_url`
  2. Register user → call `authService.setAvatarUrl(secure_url)` → navigate home
  - If Cloudinary upload fails → registration still completes, no image, user never blocked.
- `isUploadingAvatar` flag drives button label: `"Uploading photo..."` → `"Creating account..."`

---

### `src/app/shared/components/navbar/navbar.component.html`
All 4 avatar spots (desktop trigger, desktop dropdown, mobile trigger, mobile menu header) now conditionally show an `<img>` or fall back to the initials `<div>`:
```html
@if (auth.user()?.avatarUrl) {
  <img class="nav-avatar nav-avatar--photo"
       [src]="auth.user()!.avatarUrl"
       [alt]="auth.user()!.name" />
} @else {
  <div class="nav-avatar">{{ auth.user()?.avatarInitials }}</div>
}
```

---

### `src/app/shared/components/navbar/navbar.component.scss`
Added `&--photo` modifier to `.nav-avatar` and `.profile-avatar-large`:
```scss
&--photo {
  object-fit: cover;
  background: transparent;
}
```

---

### `src/app/features/profile/profile.component.ts`
`ngOnInit` reads `avatarUrl` from the auth service signal first, falls back to the old `codify_avatar` localStorage key for sessions that existed before this change:
```typescript
const currentUser = this.authService.currentUser();
this.savedAvatar =
  currentUser?.avatarUrl ??
  localStorage.getItem('codify_avatar');
```

---

## 6. Complete Flow After Implementation

```
User picks photo → Angular uploads to Cloudinary
        ↓
Cloudinary returns secure_url
        ↓
User registers (backend — text only)
        ↓
setAvatarUrl(secure_url) called:
  • saves  codify_avatar_<userId>  in localStorage
  • patches live user signal
        ↓
Navbar + profile show photo immediately ✅
        ↓
User logs out
  • codify_user + codify_token cleared
  • codify_avatar_<userId> kept ✅
        ↓
User logs back in
  • login() reads codify_avatar_<userId> using userId from API
  • merges avatarUrl into User object
  • photo restored instantly ✅
```

---

## 7. Known Limitations (Accepted Trade-offs)

| Scenario | Behaviour |
|----------|-----------|
| User clears browser data | Avatar URL lost — they'd need to re-upload |
| Different browser / device | No avatar shown — URL lives only in the original browser's localStorage |
| Change photo after registration | Not yet implemented — needs an "Edit Profile" upload flow |

When the backend eventually adds `avatarUrl` to the Users table and login response, the `codify_avatar_<userId>` localStorage key can simply be removed — no other frontend changes needed.

---

## 8. Upgrade — Backend Persistence (Current Implementation)

### Why
localStorage alone means the photo disappears if the user logs in from another device or clears their browser cache. The correct fix is to store the Cloudinary URL in the backend database and return it on every login response.

### What Changed on the Frontend

**`src/app/core/services/auth.service.ts`**

`LoginApiResponse` interface now includes the new backend field:
```typescript
user: {
  userId: string;
  fullName: string;
  role: number;
  avatarUrl?: string; // returned by backend once column is added
}
```

`login()` prefers the backend value, falls back to localStorage for existing sessions:
```typescript
const storedAvatarUrl =
  loginData.user.avatarUrl ??                                       // ← backend (cross-device)
  localStorage.getItem(`codify_avatar_${loginData.user.userId}`) ?? // ← local fallback
  undefined;
```

`setAvatarUrl()` now does three things in order:
1. Patches the live signal → UI updates instantly
2. Saves to `localStorage` as a local fallback
3. Fires `PUT /api/users/avatar` to persist to the backend (fire-and-forget, UI never blocked)

```typescript
this.http.put(
  `${this.baseUrl}/users/avatar`,
  { avatarUrl: url },
  { headers: { Authorization: `Bearer ${token}` } }
).subscribe({ error: err => console.warn('Avatar URL could not be saved to backend:', err) });
```

No changes were needed in `register.component.ts` — it already calls `setAvatarUrl()` after the Cloudinary upload.

### Complete Flow After This Upgrade

```
User picks photo → Angular uploads to Cloudinary
        ↓
Cloudinary returns secure_url
        ↓
setAvatarUrl(secure_url):
  • signal patched → navbar shows photo immediately ✅
  • saved to localStorage as fallback
  • PUT /api/users/avatar { avatarUrl } → saved in DB ✅
        ↓
User logs out → logs back in on ANY device
  • login response includes avatarUrl from DB
  • photo restored everywhere ✅
```

---

## 9. Backend Requirements — For Backend Team

### 1 — Add `AvatarUrl` column to Users table

```sql
ALTER TABLE Users ADD AvatarUrl NVARCHAR(500) NULL;
```

Or in the EF Core entity:
```csharp
public string? AvatarUrl { get; set; }
```

---

### 2 — Return `avatarUrl` in the login response

Current login response shape:
```json
{
  "data": {
    "token": "...",
    "expiresAt": "...",
    "user": {
      "userId": "...",
      "fullName": "...",
      "role": 0
    }
  }
}
```

Required shape (add one field):
```json
{
  "data": {
    "token": "...",
    "expiresAt": "...",
    "user": {
      "userId": "...",
      "fullName": "...",
      "role": 0,
      "avatarUrl": "https://res.cloudinary.com/mg7dsqv2/image/upload/..."
    }
  }
}
```

---

### 3 — Add endpoint `PUT /api/users/avatar`

- **Auth:** JWT Bearer token required
- **Request body:**
```json
{ "avatarUrl": "https://res.cloudinary.com/mg7dsqv2/image/upload/..." }
```
- **Behaviour:** Update `AvatarUrl` for the user identified by the JWT claim
- **Response:** `200 OK` (body ignored by frontend)
- **Validation:** `avatarUrl` must be a non-empty string, max 500 chars, must start with `https://res.cloudinary.com/`

Example C# controller action:
```csharp
[HttpPut("avatar")]
[Authorize]
public async Task<IActionResult> UpdateAvatar([FromBody] UpdateAvatarDto dto)
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    await _userService.UpdateAvatarUrlAsync(userId, dto.AvatarUrl);
    return Ok();
}

public record UpdateAvatarDto(string AvatarUrl);
```

---

### Summary for Backend Team

| What | Where | Notes |
|------|-------|-------|
| Add `AvatarUrl` column | `Users` table | `NVARCHAR(500)`, nullable |
| Return `avatarUrl` in login response | `POST /api/auth/login` | Map the DB column into the user object in the response |
| New endpoint | `PUT /api/users/avatar` | JWT-protected, saves URL for the currently authenticated user |

The frontend is already calling `PUT /api/users/avatar` on every Cloudinary upload. Once the endpoint exists, the URL will be stored in the DB automatically. Once the login response includes `avatarUrl`, the localStorage fallback becomes redundant and can be cleaned up on the frontend side with no other changes needed.
