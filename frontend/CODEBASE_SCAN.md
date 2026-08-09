# 🔍 Frontend Codebase Scan — Issues & Mismatches

> **Date:** 2026-08-09
> **Branch:** `fix/restore-profile-progress`
> **Build status:** ✅ Compiles clean — zero TypeScript errors, zero missing imports
> **Scan method:** Full build + per-file diagnostics + manual code review

---

## ✅ What's Actually Fine (confirmed)

- All 26 feature component files exist with matching `.html` + `.scss`
- All route definitions resolve to real component files
- All model imports across all services and components resolve correctly
- `chart.js` installed and imported correctly in the 3 chart components
- Guards (`authGuard`, `instructorGuard`) wired correctly in routes
- `analytics.service.ts`, `submission.service.ts`, `hint.service.ts` — all clean
- `heatmap-calendar.util.ts` — imported and used correctly in profile

---

## ⚠️ Issues Found

---

### 1 — Login Does NOT Handle `returnUrl` After Redirect

**Severity:** 🟡 Medium — functional gap, not a crash

**Files:**
- `src/app/core/guards/auth.guard.ts` — sends `returnUrl` query param ✅
- `src/app/features/auth/login/login.component.ts` — **ignores it** ❌

**Detail:**
The `authGuard` was updated (our branch) to preserve the intended URL:
```ts
router.navigate(['/auth/login'], {
  queryParams: { returnUrl: state.url },
});
```
But `login.component.ts` always navigates to `/` on success:
```ts
this.authService.login(email!, password!).subscribe(result => {
  if (result.success) {
    this.router.navigate(['/']);  // ← ignores returnUrl
  }
});
```
So if a student tries to access `/progress` while logged out, after login they land on `/` instead of `/progress`.

---

### 2 — `instructorGuard` Redirects Students to `/dashboard` (Which No Longer Exists as a Page)

**Severity:** 🟡 Medium — bad UX redirect, not a crash

**File:** `src/app/core/guards/instructor.guard.ts`

**Detail:**
```ts
router.navigate(['/dashboard']);  // line 20
```
`/dashboard` now redirects to the user's profile via a `redirectTo` in `app.routes.ts`. So it works eventually, but the redirect chain is:
`/instructor` → guard → `/dashboard` → route redirect → `/profile/test_student`

Two unnecessary hops. Should redirect directly to `/profile/:username` or at least `/` for clarity.

---

### 3 — `student-dashboard.component.ts` Is a Stub and Still Has a Route

**Severity:** 🟠 Low-Medium — placeholder still visible if navigated to directly

**Files:**
- `src/app/features/student-dashboard/student-dashboard.component.ts`
- `src/app/app.routes.ts`

**Detail:**
The old dashboard route was replaced with a redirect:
```ts
{
  path: 'dashboard',
  redirectTo: () => { ... return `/profile/${slug}` }
}
```
Good — `/dashboard` now redirects. But the stub component file still exists:
```ts
@Component({ template: '<p>Full student dashboard — coming soon</p>' })
export class StudentDashboardComponent {}
```
It's not routed to anymore but it's dead code. Minor, but worth cleaning up.

---

### 4 — `progress.service.ts` Has a Hardcoded Date (`2026-07-23`)

**Severity:** 🟡 Low-Medium — stale mock data, visually wrong

**File:** `src/app/core/services/progress.service.ts` — line ~43

**Detail:**
```ts
const today = new Date('2026-07-23T00:00:00Z');
```
This is hardcoded, so the class activity trend chart in the instructor overview will always show data anchored to July 23, 2026 — even though today is August 9, 2026. The chart labels will be 2+ weeks in the past.

Should be `new Date()` to stay relative to the real current date.

---

### 5 — `analytics.model.ts` Is Written in Compact Single-Line Format

**Severity:** 🟢 Low — cosmetic / maintainability

**File:** `src/app/core/models/analytics.model.ts`

**Detail:**
Due to a workaround during the gitignore issue, the file was written as 22 single-line exports instead of properly formatted multi-line code:
```ts
export interface TopicStat { topic: string; percentage: number; trend: 'up' | 'down' | 'flat'; }
export interface LanguageStat { language: string; solved: number; }
// ... all on one line each
```
Functionally correct — TypeScript doesn't care. But it's unreadable and inconsistent with the rest of the codebase.

---

### 6 — `profile/:username` Route Has No Auth Guard — Anyone Can View Any Profile

**Severity:** 🟢 Low — by design, but worth flagging

**File:** `src/app/app.routes.ts`

**Detail:**
```ts
{
  path: 'profile/:username',
  loadComponent: () => import('./features/profile/profile.component')...
  // no canActivate
}
```
This is intentional (profiles are public) — the profile component even has an `isOwnProfile` check. But there's no `guestGuard` either, so unauthenticated users can view profiles. Confirm this is the intended behavior before shipping.

---

### 7 — `auth.service.ts` Has Two Aliases for the Same Signal (`currentUser` and `user`)

**Severity:** 🟢 Low — technical debt

**File:** `src/app/core/services/auth.service.ts` — line ~35

**Detail:**
```ts
readonly currentUser = this._currentUser.asReadonly();
readonly user = this._currentUser.asReadonly(); // Alias for backward compatibility
```
Both exist as separate `asReadonly()` calls returning separate signal instances. Functionally fine (signals derived from the same source), but redundant. `app.routes.ts` uses `currentUser()`, navbar uses `user()`, profile uses `currentUser()`. Inconsistent usage across the codebase — should standardize on one name.

---

### 8 — No `footer` on Profile or Progress Pages (Layout Issue)

**Severity:** 🟢 Low — visual inconsistency

**File:** `src/app/app.html`

**Detail:**
The footer is shown when `hideLayout` is false. The profile and progress pages don't set `hideLayout: true` in their route data, so the footer should appear. However, this is worth verifying visually — both pages have very long scrollable content and the footer might feel out of place or get cut off by fixed bottom elements in the profile page.

---

### 9 — Sass `@import` and `darken()` Deprecation Warnings

**Severity:** 🟢 Low — warnings, not errors, app works fine

**Files:**
- `src/app/features/auth/forgot-password/forgot-password.component.scss`
- `src/app/features/auth/login/login.component.scss`
- `src/app/features/auth/register/register.component.scss`
- `src/app/features/problem-page/problem-page.component.scss`

**Detail:**
These files use `@import` (deprecated in Dart Sass 3.0) and `darken()` (deprecated global built-in). These are warnings, not errors — the app builds and runs. They'll become errors when Dart Sass 3.0 ships. Not urgent.

Fix: Replace `@import` with `@use`, replace `darken($color, 10%)` with `color.adjust($color, $lightness: -10%)`.

---

### 10 — `DailyActivity` Interface Name Collision Between Two Models

**Severity:** 🟡 Medium — potential future confusion / import mistakes

**Files:**
- `src/app/core/models/analytics.model.ts` — exports `DailyActivity { date, submitted }`
- `src/app/core/models/progress.model.ts` — exports `DailyActivity { date, dayLabel, submissions }`

**Detail:**
Two completely different interfaces share the exact name `DailyActivity` in two different model files. They have different shapes:
```ts
// analytics.model.ts
export interface DailyActivity { date: string; submitted: boolean; }

// progress.model.ts
export interface DailyActivity { date: string; dayLabel: string; submissions: number; }
```
Right now this doesn't cause errors because they're in different files and imports are explicit. But if anyone mixes up the import path it'll cause silent type bugs. The `progress.model` version is used by `ProgressService` for the instructor chart. The `analytics.model` version is used by `StudentProgressComponent` for the streak dots.

Should be renamed — e.g., `StreakDay` in analytics and `ClassActivityDay` in progress.

---

## 📊 Summary Table

| # | Issue | File(s) | Severity | Type |
|---|---|---|---|---|
| 1 | Login ignores `returnUrl` | `login.component.ts` | 🟡 Medium | Functional gap |
| 2 | `instructorGuard` redirects to dead `/dashboard` | `instructor.guard.ts` | 🟡 Medium | Bad UX |
| 3 | `StudentDashboardComponent` is dead stub code | `student-dashboard.component.ts` | 🟠 Low-Med | Dead code |
| 4 | Hardcoded date in `progress.service.ts` | `progress.service.ts` | 🟡 Low-Med | Stale data |
| 5 | `analytics.model.ts` is unreadable compact format | `analytics.model.ts` | 🟢 Low | Cosmetic |
| 6 | Profile route has no auth — public by design? | `app.routes.ts` | 🟢 Low | Design question |
| 7 | `currentUser` / `user` dual alias in auth service | `auth.service.ts` | 🟢 Low | Tech debt |
| 8 | Footer visibility on long pages (visual check needed) | `app.html` | 🟢 Low | Visual |
| 9 | Sass `@import` + `darken()` deprecation warnings | auth + problem scss files | 🟢 Low | Future-proofing |
| 10 | `DailyActivity` name collision across two models | `analytics.model` + `progress.model` | 🟡 Medium | Tech debt / type risk |

---

## 🎯 Fix Priority Order (suggested)

1. **Fix #1** — `returnUrl` in login (quick, high value)
2. **Fix #4** — hardcoded date in progress service (one-liner)
3. **Fix #2** — instructor guard redirect target (one-liner)
4. **Fix #10** — rename `DailyActivity` collision (rename across 3 files)
5. **Fix #5** — reformat `analytics.model.ts` (cosmetic but worth it)
6. **Fix #3** — delete dead `StudentDashboardComponent` (cleanup)
7. **Fix #9** — Sass deprecations (lower priority, still works)
8. **Clarify #6** — confirm profile page public access is intentional
9. **Fix #7** — standardize `currentUser` vs `user` alias

---

*Scan completed by Kiro — 2026-08-09*
