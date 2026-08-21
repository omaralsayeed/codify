# Hardcoded API URL Problem & Fix Plan

> **Created:** August 21, 2026
> **Author:** Frontend team
> **Priority:** 🔴 Must fix before any production deployment

---

## The Problem

Every service in this codebase has the backend API URL written directly into the TypeScript source code as a string literal:

```typescript
private readonly baseUrl = 'http://localhost:5237/api';
```

This is called a **hardcoded URL**. It means the string `http://localhost:5237` is physically baked into the compiled JavaScript bundle that ships to users.

---

## Why This Is a Real Problem

### In development — works fine
The backend runs on your machine at port `5237`. The Angular dev server runs at `4200`. Everyone connects locally. No issue.

### In production — breaks completely
When you deploy:
- The frontend is hosted on a real server (e.g. `https://codify.app`)
- The backend is hosted somewhere else (e.g. `https://api.codify.app` or `https://codify.app:8080`)
- The port `5237` is an ASP.NET Core development port — it will NOT exist in production

Every single API call from every user hits `http://localhost:5237` — which is **their** localhost, not the server. Every request fails. The app is completely broken in production.

### It also means:
- If the backend team changes the dev port (e.g. to `5238`), you have to update **9 files**
- Staging environments (e.g. `https://staging-api.codify.app`) need a different URL — impossible without changing source code
- You cannot do a proper CI/CD pipeline because the URL is not configurable at build time

---

## Affected Files — Full List

9 places in the codebase with hardcoded `http://localhost:5237`:

| # | File | Variable Name | Hardcoded Value |
|---|------|--------------|-----------------|
| 1 | `src/app/core/services/auth.service.ts` | `baseUrl` | `http://localhost:5237/api` |
| 2 | `src/app/core/services/problem.service.ts` | `baseUrl` | `http://localhost:5237/api` |
| 3 | `src/app/core/services/admin.service.ts` | `baseUrl` | `http://localhost:5237/api` |
| 4 | `src/app/core/services/submission.service.ts` | `API` | `http://localhost:5237/api` |
| 5 | `src/app/core/services/hint.service.ts` | `API` | `http://localhost:5237/api` |
| 6 | `src/app/core/services/analytics.service.ts` | `API` | `http://localhost:5237/api` |
| 7 | `src/app/core/services/instructor.service.ts` | `baseUrl` | `http://localhost:5237/api/analytics` ← also has the path segment baked in |
| 8 | `src/app/core/services/contest.service.ts` | `baseUrl` | `http://localhost:5237/api/contests` ← also has the path segment baked in |
| 9 | `src/app/features/instructor/contest-create/instructor-contest-create.component.ts` | `apiBase` | `http://localhost:5237/api` ← this one is directly in a **component**, not even a service |

Note: files 7 and 8 have an extra issue — they bake the path segment (`/analytics`, `/contests`) into the base URL variable, which is inconsistent with the other services that only store `/api`.

---

## The Fix — Angular Environment Files

Angular has a built-in solution for exactly this: **environment files**.

The idea:
- You define the API URL in one dedicated file per environment
- All services import from that one file
- When you build for production, Angular automatically swaps in the production file
- To change the URL for any environment, you edit **one file**, not 9

### How it works

```
src/environments/
  environment.ts          ← used during  ng serve  (development)
  environment.prod.ts     ← used during  ng build  (production)
```

`environment.ts` (dev):
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5237/api'
};
```

`environment.prod.ts` (prod):
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.codify.app/api'   // ← real production URL goes here
};
```

Every service then does:
```typescript
import { environment } from '../../../environments/environment';

// instead of:
private readonly baseUrl = 'http://localhost:5237/api';

// it becomes:
private readonly baseUrl = environment.apiUrl;
```

Angular CLI (`angular.json`) is configured with a `fileReplacements` rule so that when you run `ng build` (production), it automatically replaces `environment.ts` with `environment.prod.ts` before bundling. Services don't need to know which environment they're in — they just read `environment.apiUrl` and get the right value.

---

## Step-by-Step Implementation Plan

### Step 1 — Create the environments folder and two files

**File:** `src/environments/environment.ts`
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5237/api'
};
```

**File:** `src/environments/environment.prod.ts`
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://YOUR_PRODUCTION_API_URL/api'  // team fills this in at deploy time
};
```

---

### Step 2 — Register the file swap in `angular.json`

Inside the `production` configuration block in `angular.json`, add a `fileReplacements` array:

```json
"production": {
  "fileReplacements": [
    {
      "replace": "src/environments/environment.ts",
      "with": "src/environments/environment.prod.ts"
    }
  ],
  "optimization": { ... },
  ...
}
```

This is the only change needed to `angular.json`. After this, `ng build` (production) swaps the file automatically.

---

### Step 3 — Update all 9 affected files

For each file: remove the hardcoded string, import `environment`, read from `environment.apiUrl`.

**Special cases to handle carefully:**
- `instructor.service.ts` — currently stores `http://localhost:5237/api/analytics`. Fix: use `environment.apiUrl` as base, append `/analytics` at the call site or as a separate constant inside the service.
- `contest.service.ts` — currently stores `http://localhost:5237/api/contests`. Same fix: use `environment.apiUrl` as base, append `/contests` at the call site.
- `instructor-contest-create.component.ts` — has a direct `HttpClient` injection and a raw API URL in a component. This is an architectural issue too (HTTP calls belong in services, not components). For now: just fix the hardcoded URL by importing `environment`. The refactor to move HTTP out of the component is a separate task.

---

### Step 4 — Verify

Run `ng build --configuration development` — should compile clean with no new errors.
Run `ng build --configuration production` — should compile clean, and the bundled JS should NOT contain `localhost:5237` anywhere.

---

## File Change Summary

| File | Change |
|------|--------|
| `src/environments/environment.ts` | **CREATE** — dev environment config |
| `src/environments/environment.prod.ts` | **CREATE** — prod environment config |
| `angular.json` | **EDIT** — add `fileReplacements` to production config |
| `src/app/core/services/auth.service.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |
| `src/app/core/services/problem.service.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |
| `src/app/core/services/admin.service.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |
| `src/app/core/services/submission.service.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |
| `src/app/core/services/hint.service.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |
| `src/app/core/services/analytics.service.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |
| `src/app/core/services/instructor.service.ts` | **EDIT** — replace hardcoded URL, fix baked-in path segment |
| `src/app/core/services/contest.service.ts` | **EDIT** — replace hardcoded URL, fix baked-in path segment |
| `src/app/features/instructor/contest-create/instructor-contest-create.component.ts` | **EDIT** — replace hardcoded URL with `environment.apiUrl` |

**Total: 2 new files, 10 edits.**

---

## What Changes for the Team After This Fix

**To change the dev API URL:** Edit one line in `src/environments/environment.ts`

**To set the production URL:** Edit one line in `src/environments/environment.prod.ts`

**To add a staging environment:** Create `src/environments/environment.staging.ts`, add a new configuration in `angular.json`, run `ng build --configuration staging`

**Nothing else changes.** All services keep working exactly as they do now.

---

## What This Does NOT Fix

This plan only fixes the hardcoded URL problem. These separate issues are out of scope here:

- The `ProblemService.update()` using `PUT` instead of `PATCH`
- The hardcoded date in `progress.service.ts`
- The `DailyActivity` interface name collision
- The raw HTTP call inside `instructor-contest-create.component.ts` (architectural smell — HTTP belongs in services)

Those are tracked separately in `FINAL_CHECK.md`.

---

*Ready to implement — confirm and we execute all 12 file changes in one pass.*

---

## Reviewer Recommendations (addendum — plan steps unchanged)

> These were raised as a review pass on the original plan. None of them change Steps 1–4 or block implementation. They refine two specific decisions and add one verification improvement.

---

### Rec 1 — The plan is host-agnostic, keep it that way

The environment-file pattern works by reading one variable (`environment.apiUrl`) at build time. It does not care what hosting provider is used — only the value of that one string changes per host. If the team moves off any current provider later, nothing in Steps 1–4 needs to change. You edit one line in one file and redeploy.

**Impact on plan:** None. Just a framing note — do not mentally tie this fix to any specific host.

---

### Rec 2 — The real decision before setting `environment.prod.ts` is same-origin vs cross-origin

This is the architectural question that determines what the production `apiUrl` value should be:

| Deployment shape | Frontend URL | Backend URL | `apiUrl` value |
|---|---|---|---|
| Same domain, same server | `https://codify.app` | `https://codify.app/api` | `'/api'` (relative) |
| Different domains / services | `https://codify.app` | `https://api.codify.app` | `'https://api.codify.app/api'` (absolute) |

If the value is wrong:
- **Relative path on a cross-origin setup** → every API call 404s silently
- **Absolute URL with wrong domain** → CORS errors on every request

This question is independent of which hosting provider is used. It needs to be answered with the backend teammate before deploy day.

---

### Rec 3 — Default `environment.prod.ts` to `apiUrl: '/api'` (relative), not a placeholder string

The current plan has `'https://YOUR_PRODUCTION_API_URL/api'` as the placeholder. That is a forgotten-placeholder risk — if someone runs `ng build` without filling it in, that literal string ships to production and every API call fails with no obvious error.

**The safer default:**

```typescript
// environment.prod.ts
export const environment = {
  production: true,
  // Same-origin assumed (frontend + backend on same host/port).
  // If frontend and backend are split across different domains,
  // replace this with the full absolute URL: 'https://api.your-domain.com/api'
  apiUrl: '/api'
};
```

A relative path:
- Works immediately if frontend and backend are on the same origin (most likely for a monorepo / single-server deploy)
- Sidesteps CORS configuration entirely — one less thing to debug on deploy day
- Fails loudly and obviously if the assumption is wrong (404 on every request), which is easier to diagnose than a CORS error from a wrong absolute URL

**Change to Step 1 in the plan:** use `'/api'` as the default value in `environment.prod.ts`, not a placeholder domain string.

---

### Rec 4 — Add an inline comment to the `apiUrl` line in `environment.prod.ts`

If the hosting decision changes later (frontend and backend end up on different services), whoever edits the file needs an immediate, visible cue — not a silent bug where API calls go to the wrong place with no build-time error.

The comment in Rec 3 above serves this purpose. It should be in the file, not just in this document.

**Required comment (already shown in Rec 3):**
```typescript
// Same-origin assumed. If frontend and backend are on different domains,
// replace with full absolute URL: 'https://api.your-domain.com/api'
apiUrl: '/api'
```

---

### Rec 5 — Confirm same-origin vs cross-origin with backend teammate before deploy day, not before implementation

This confirmation does **not** block any of the 9 file edits, the `angular.json` change, or the special-case fixes in `instructor.service.ts` / `contest.service.ts`.

It only affects one value in one file (`environment.prod.ts`). The entire implementation can proceed right now. The value gets filled in correctly at deploy time after a 5-minute conversation with the backend team.

**Action:** Implement everything now. Before the first production build, confirm with backend:
> "Are frontend and backend being served from the same domain and port, or different ones?"

---

### Rec 6 — Add a post-build grep check to Step 4's verification

Step 4 currently says the production bundle "should NOT contain `localhost:5237`" but doesn't give a concrete way to confirm that. This turns it into a pass/fail check:

```bash
grep -r "localhost:5237" dist/ || echo "clean"
```

If any output appears before `clean` — one of the 9 files was missed. If only `clean` prints — the bundle is safe.

**Change to Step 4 in the plan:** add this command as the final verification step after both build commands.

---

## Revised Step 4 (incorporating Rec 6)

```bash
# Development build — confirm no compile errors
ng build --configuration development

# Production build — confirm no compile errors  
ng build --configuration production

# Verify the production bundle contains no localhost references
grep -r "localhost:5237" dist/ || echo "clean"
# Expected output: just "clean"
# If any file paths print above "clean" → go back and fix those files
```

---

## Revised Step 1 — `environment.prod.ts` (incorporating Recs 3 & 4)

```typescript
// src/environments/environment.prod.ts
export const environment = {
  production: true,
  // Same-origin assumed (frontend + backend served from the same host).
  // If frontend and backend are split across different domains or ports,
  // replace this with the full absolute URL, e.g.: 'https://api.codify.app/api'
  // Confirm with backend team before first production deploy.
  apiUrl: '/api'
};
```

---

*Recommendations reviewed and accepted — August 21, 2026*

---

## Implementation Progress Log

> **Executed:** August 21, 2026
> **Status:** ✅ Complete — all 12 file changes done, both builds verified clean

---

### Files Created

| File | Notes |
|------|-------|
| `src/environments/environment.ts` | Dev config — `apiUrl: 'http://localhost:5237/api'` |
| `src/environments/environment.prod.ts` | Prod config — `apiUrl: '/api'` with same-origin assumption comment |

---

### Files Modified

| File | Change |
|------|--------|
| `angular.json` | Added `fileReplacements` to `production` config — swaps `environment.ts` → `environment.prod.ts` at build time |
| `src/app/core/services/auth.service.ts` | Import + `baseUrl = environment.apiUrl` |
| `src/app/core/services/problem.service.ts` | Import + `baseUrl = environment.apiUrl` |
| `src/app/core/services/admin.service.ts` | Import + `baseUrl = environment.apiUrl` |
| `src/app/core/services/submission.service.ts` | Import + `API = environment.apiUrl` |
| `src/app/core/services/hint.service.ts` | Import + `API = environment.apiUrl` |
| `src/app/core/services/analytics.service.ts` | Import + `API = environment.apiUrl` |
| `src/app/core/services/instructor.service.ts` | Import + `baseUrl = \`${environment.apiUrl}/analytics\`` (fixed baked-in path segment) |
| `src/app/core/services/contest.service.ts` | Import + `baseUrl = \`${environment.apiUrl}/contests\`` (fixed baked-in path segment) |
| `src/app/features/instructor/contest-create/instructor-contest-create.component.ts` | Import + `apiBase = environment.apiUrl` |

---

### Import Path Reference (for future files)

The depth varies by file location. Always use a relative path:

| File location | Correct import path |
|---|---|
| `src/app/core/services/*.ts` | `'../../../environments/environment'` |
| `src/app/features/**/*.ts` (4 levels deep) | `'../../../../environments/environment'` |
| `src/app/features/**/*.ts` (5 levels deep) | `'../../../../../environments/environment'` |

---

### Build Verification

**Development build:**
```
✅ ng build --configuration development → clean (0 errors)
```
The dev bundle contains `localhost:5237` in exactly one place — `chunk-WAVKUVB3.js` — which is the compiled `environment.ts` file itself. This is correct and expected behavior.

**Production build:**
```
✅ ng build --configuration production → clean (0 errors)
```

**Production bundle grep check (Rec 6):**
```powershell
$found = Select-String -Path "dist/codify/browser/*.js" -Pattern "localhost:5237" -Quiet
# Result: CLEAN - no localhost:5237 in production bundle ✅
```

The production bundle contains zero references to `localhost:5237`. The `fileReplacements` swap is working correctly.

---

### What Remains (Pre-Deploy)

Before the first production deploy, confirm with the backend team:
> "Are frontend and backend served from the same domain and port, or different ones?"

- **Same origin** → `environment.prod.ts` is already correct with `apiUrl: '/api'`, no change needed
- **Different origins** → update `apiUrl` in `environment.prod.ts` to the full absolute backend URL (one line change)

---

*Implementation completed by Kiro — August 21, 2026*
