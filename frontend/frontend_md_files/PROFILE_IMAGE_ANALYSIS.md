# Profile Image — Current State, Problems & Upload Solutions

---

## 1. What Exists Right Now

### Register Page
- A file input (`<input type="file" accept="image/jpeg,image/png,image/webp">`) lets the user pick a photo before signing up.
- `onProfilePictureSelect()` reads the file with `FileReader` and stores it as a base64 data URL in the component variable `profilePicturePreview`.
- This preview is shown inside the register form (the circle above the fields).
- After a **successful registration**, this line runs:
  ```typescript
  if (this.profilePicturePreview) {
    localStorage.setItem('codify_avatar', this.profilePicturePreview);
  }
  ```
  The base64 image is written to `localStorage` under the key `codify_avatar`. That is the **only persistence** — no upload to any server happens.

### Profile Page (`/profile/:username`)
- On `ngOnInit`, the component reads `localStorage.getItem('codify_avatar')` into `savedAvatar`.
- If `savedAvatar` is not null, the template shows `<img [src]="savedAvatar">`.
- Otherwise it shows a colored circle with the user's **avatar initials** (e.g., "JS").

### Navbar / Header (all pages after login)
- **Both desktop and mobile** avatars only show `avatarInitials`:
  ```html
  <div class="nav-avatar">{{ auth.user()?.avatarInitials }}</div>
  ```
- The profile dropdown large avatar also uses `avatarInitials`.
- **No image is shown anywhere in the navbar — ever.**

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
There is **no `profileImageUrl` or `avatarUrl` field** in the User interface.

### Auth Service — Login & Session Restore
- On login the API response only returns `{ userId, fullName, role }` — no image URL.
- The `User` object built from the login response has no image field.
- On page refresh, `restoreSession()` reads the user from `localStorage['codify_user']` — also no image field.

---

## 2. Why the Photo Is Not Working / Not Appearing

| # | Problem | Root Cause |
|---|---------|------------|
| 1 | **Image never uploaded to any server** | `onSubmit()` calls `authService.register(...)` with only text fields. The file is never included in the HTTP request. The backend never receives it. |
| 2 | **Image stored as raw base64 in localStorage** | Base64 bloats the image size by ~33%. A 300 KB photo becomes ~400 KB of text sitting in localStorage (5–10 MB limit). On any other device or browser it is completely invisible. |
| 3 | **Navbar always shows initials, never the image** | The navbar template reads `auth.user()?.avatarInitials`. There is no conditional `<img>` tag. Even if `codify_avatar` is in localStorage, the navbar will never display it. |
| 4 | **No `profileImageUrl` field in User model or auth service** | Even if the backend were to return an image URL, there is no field in the `User` interface to store it, and the auth service does not map it. |
| 5 | **`codify_avatar` is NOT cleared on logout** | `auth.logout()` removes `codify_user` and `codify_token` but not `codify_avatar`. A different user logging in on the same browser sees the previous user's photo. |
| 6 | **Profile page only loads image from localStorage** | The profile page never calls the backend to fetch a profile image URL. It only checks `localStorage`, so the feature only half-works on the same browser, same device, never shared. |
| 7 | **Auto-login after register does not re-read localStorage** | After register, the app calls `login()` internally. The login response builds a fresh `User` object — it does not read `codify_avatar`. So when the navbar renders right after registration, the image is still invisible in the nav. |

---

## 3. Best Free Options for Hosting Profile Images

### Option A — Cloudinary (Recommended for this project)

**Why it fits best:**
- Purpose-built for image hosting. No backend storage code needed.
- Free tier: **25 GB storage + 25 GB bandwidth/month** — more than enough for a learning platform.
- Returns a permanent HTTPS URL you simply save in your database as a string.
- Built-in on-the-fly transformations (auto crop to face, resize, compress, WebP conversion) via URL parameters — no extra code.
- Has a direct browser upload API (`unsigned upload`) so the Angular frontend can upload directly from the browser without routing through your backend, keeping your .NET backend simple.

**How it would work here:**
```
Register form picks file
    ↓
Angular calls Cloudinary Upload API (POST to https://api.cloudinary.com/v1_1/<cloud>/image/upload)
    ↓
Cloudinary returns { secure_url: "https://res.cloudinary.com/..." }
    ↓
Angular includes that URL in the register POST to your .NET backend
    ↓
Backend stores the URL string in the Users table
    ↓
Login response returns the URL → saved in User object → shown in navbar + profile
```

**Cost:** Free forever for small/medium projects. Paid plans start at $89/month if you outgrow the free tier.

---

### Option B — AWS S3 + CloudFront (Free Tier)

**Why it could work:**
- AWS Free Tier gives **5 GB S3 storage + 15 GB outbound transfer/month for 12 months**.
- After 12 months the free tier expires and you pay per GB (very cheap but no longer free).
- Requires more setup: S3 bucket policy, IAM user/role, CORS config, presigned URLs or a backend endpoint to generate upload URLs.
- Your .NET backend generates a **presigned S3 URL**, the Angular app uploads directly to S3, then saves the resulting S3/CloudFront URL.

**Verdict for this project:** More complex to set up, free tier expires, and it is not purpose-built for image serving. Good if you are already on AWS, but Cloudinary is faster to integrate.

---

### Option C — Supabase Storage (Free)

**Why it could work:**
- Free tier: **1 GB storage + 2 GB bandwidth/month** (smaller than Cloudinary).
- Open-source and self-hostable.
- Has a simple JavaScript/TypeScript SDK for direct browser uploads.
- Returns a public URL after upload.
- If you are already using Supabase for your database, this is a natural fit.

**Verdict for this project:** Free tier is tight for images. Good only if you are already on Supabase.

---

### Option D — Firebase Storage (Google)

**Why it could work:**
- Free Spark plan: **5 GB storage + 1 GB/day download**.
- Google-grade CDN, very fast delivery.
- Angular SDK available (`@angular/fire`).
- Requires a Google account and Firebase project setup.

**Verdict for this project:** 1 GB/day download cap can be hit easily on a growing platform. More setup overhead than Cloudinary.

---

## 4. Recommendation Summary

| Service | Free Storage | Free Bandwidth | Setup Effort | Best For |
|---------|-------------|----------------|--------------|----------|
| **Cloudinary** ✅ | 25 GB | 25 GB/mo | Low | This project — just save the URL |
| AWS S3 | 5 GB (12 mo) | 15 GB/mo | High | Already on AWS |
| Supabase | 1 GB | 2 GB/mo | Medium | Already on Supabase |
| Firebase | 5 GB | 1 GB/day | Medium | Already on Firebase |

**Go with Cloudinary.** It requires the least backend changes, has the most generous free tier for images, and its transformation URLs (auto-crop to face, resize to 200×200, compress) mean you never serve bloated originals.

---

## 5. What Needs to Be Fixed (Summary)

1. Add `profileImageUrl?: string` to the `User` model.
2. Update the `LoginApiResponse` and `RegisterApiResponse` interfaces to include the image URL.
3. Map `profileImageUrl` in `auth.service.ts` when building the `User` object.
4. In `register.component.ts`, upload the selected file to Cloudinary first, get the URL, then include it in the register POST body.
5. Update `logout()` to also remove `codify_avatar` from localStorage.
6. In `navbar.component.html`, replace the initials-only `<div class="nav-avatar">` with a conditional: show `<img>` if `profileImageUrl` exists, otherwise show initials.
7. In `profile.component.ts`, read `profileImageUrl` from the `User` object (via auth service) instead of — or in addition to — localStorage.
