# Profile Page Redesign Plan

> **Codify — Dashboard → Public Profile Page**
>
> Status: **AWAITING CONFIRMATION** — no code written yet.

---

## What you asked for

> "Dashboard should be the user profile page that everybody can see — like LeetCode's profile at `/u/username`.
> Progress stays private (only the logged-in user sees it).
> Remove the `/dashboard` own link and give it a public URL like `/profile/username`."

---

## Current state

| Page | Route | Guard | What it shows |
|---|---|---|---|
| Dashboard | `/dashboard` | authGuard | Private stats + 2 charts + recommendations |
| Progress | `/progress` | authGuard | Private deep analytics |

---

## Target state after this update

| Page | Route | Guard | Visible to |
|---|---|---|---|
| **Public Profile** | `/profile/:username` | **None (public)** | Anyone, no login needed |
| Progress | `/progress` | authGuard (unchanged) | Only the logged-in student |

The old `/dashboard` route will **redirect → `/profile/:username`** of the logged-in user so no existing links break.

---

## New `ProfilePage` layout — inspired by LeetCode

### Left column (sticky sidebar, ~280px)
```
┌─────────────────────────────┐
│  [TS]   Test Student        │  ← avatar initials (large circle)
│         student@codify.com  │
│         Student · Free Plan │
│                             │
│  🔥 7-day streak            │
│  📅 Joined Jul 2026         │
│                             │
│  ─────────────────          │
│                             │
│  Solved by difficulty       │
│  Easy   ██████  18          │
│  Medium ████    10          │
│  Hard   ██       3          │
│                             │
│  Languages used             │
│  Python      · 28 problems  │
│  C#           · 8 problems  │
│  JavaScript   · 4 problems  │
│                             │
│  Topic strengths            │
│  Arrays      [strong] 83    │
│  Strings     [strong] 75    │
│  Hashing     [avg]    60    │
│  ...                        │
└─────────────────────────────┘
```

### Right column (main content)
```
┌─────────────────────────────────────────────────────────┐
│  31 / 47 problems solved   66% success rate             │
│  [Easy 18] [Medium 10] [Hard 3]                         │
│                                                         │
│  Activity heatmap — 365-day grid (GitHub-style)         │
│  ░░▓░░▓▓░░░▓▓▓░ ... (mini squares, green fill)         │
│  "X submissions in the past year · Max streak: 15 days" │
│                                                         │
│  ─────────────────────────────────────────────          │
│                                                         │
│  Recent Accepted Submissions                            │
│  Two Sum                          Easy   1 year ago     │
│  Climbing Stairs                  Easy   1 year ago     │
│  Valid Parentheses                Easy   1 year ago     │
│  ...                                                    │
└─────────────────────────────────────────────────────────┘
```

---

## Exact sections

### Left sidebar
1. **Avatar** — large circle with initials (colorful background from name hash)
2. **Name + role + plan badge**
3. **Streak** — 🔥 current streak + "Best: N days"
4. **Joined date** — from `user.createdAt` (mock: formatted nicely)
5. **Difficulty breakdown** — 3 mini horizontal bars (Easy/Medium/Hard) with solved counts. Derived from `difficultyBreakdown` in `StudentAnalytics`
6. **Languages used** — list with solve counts per language. Derived from `recentSubmissions` language field (mocked for now)
7. **Topic strengths** — compact list: topic name + badge (strong/average/weak). From `topics` in `StudentAnalytics`

### Right main
1. **Summary row** — "31 solved · 66% success · 47 attempted" — same numbers as progress hero, no animation (public page = static)
2. **Difficulty pill row** — Easy (18) · Medium (10) · Hard (3) — colored pills
3. **Activity heatmap** — 52 weeks × 7 days grid, GitHub-style. Each cell = a day; filled green if `submitted: true`. Built with inline HTML divs (no chart library needed — pure CSS grid). Only last 52 weeks shown. Tooltip on hover showing the date.
4. **"N submissions in the past year · Max streak: N days · Total active days: N"** — single stats line below the heatmap
5. **Recent Accepted Submissions** — flat list, only `status === 'Accepted'` rows, title + difficulty badge + relative time. Clicking navigates to the problem.

---

## What does NOT appear on the public profile

- AI insights / weak topic details (private)
- Hint usage stats (private)
- Recommendations (private — only on /progress)
- Success rate chart (private)
- Focus areas with AI callouts (private)
- Any "Practice Now" CTA (private)
- Score/grade data (private)

---

## Data model additions needed

### Add to `User` model
```ts
username?: string;   // URL-safe slug, e.g. "test_student"
joinedAt?: string;   // ISO date
```

### Add to `analytics.model.ts`
```ts
export interface LanguageStat {
  language: string;   // 'Python', 'C#', 'JavaScript'
  solved: number;
}

export interface ActivityDay {
  date: string;       // 'YYYY-MM-DD'
  count: number;      // 0 = no submission, 1+ = submitted
}

export interface PublicProfileData {
  user: {
    username: string;
    name: string;
    avatarInitials: string;
    role: 'student' | 'instructor';
    joinedAt: string;
  };
  totalSolved: number;
  totalAttempted: number;
  successRate: number;
  streak: {
    currentStreak: number;
    longestStreak: number;
    totalActiveDays: number;
  };
  difficultyBreakdown: DifficultyBreakdown;
  languageStats: LanguageStat[];
  topicStats: TopicPerformance[];        // reuse existing
  activityGrid: ActivityDay[];           // last 365 days, always 365 items
  recentAccepted: RecentSubmission[];    // last 10 accepted only
}
```

### New service method
```ts
// AnalyticsService
getPublicProfile(username: string): Observable<PublicProfileData>
// TODO: GET /api/profile/:username
// Mock: returns data for any username (ignores the param for now)
```

---

## Route changes

### `app.routes.ts`
```ts
// ADD — public, no guard
{ path: 'profile/:username', loadComponent: () => import('./features/profile/profile.component').then(m => m.ProfileComponent) },

// CHANGE — redirect /dashboard to own profile
{ path: 'dashboard', redirectTo: () => {
    const user = inject(AuthService).currentUser();
    return user ? `/profile/${user.name.toLowerCase().replace(/\s+/g, '_')}` : '/';
}}
```

> Note: Angular redirect with a function requires a `redirectTo` factory. This is supported in Angular 17+. The project is on Angular 21 so it works.

### Navbar changes
- Remove the `Dashboard` link in the logged-in nav
- In the profile dropdown, replace "Dashboard" → **"My Profile"** → navigates to `/profile/:username`
- Keep `Progress` link unchanged (private, auth-guarded)

---

## New files to create

```
src/app/features/profile/
  profile.component.ts
  profile.component.html
  profile.component.scss
  activity-heatmap.component.ts    ← small standalone sub-component
```

---

## Files to modify

| File | Change |
|---|---|
| `src/app/app.routes.ts` | Add `/profile/:username`, change `/dashboard` to redirect |
| `src/app/core/models/analytics.model.ts` | Add `LanguageStat`, `ActivityDay`, `PublicProfileData` |
| `src/app/core/models/user.model.ts` | Add `username?`, `joinedAt?` |
| `src/app/core/services/analytics.service.ts` | Add `getPublicProfile(username)` with mock |
| `src/app/shared/components/navbar/navbar.component.html` | Replace Dashboard link → My Profile |
| `src/app/shared/components/navbar/navbar.component.ts` | Add helper to build profile URL from currentUser |
| `src/app/features/student-dashboard/` | **DELETE** all three files (component no longer needed) |

---

## What stays exactly the same

- `/progress` — unchanged, still private, still full analytics deep-dive
- All other routes
- All existing models except additions above
- Auth flow

---

## Mobile layout

On mobile (<768px):
- Left sidebar collapses to a top header bar (avatar + name + streak inline)
- Difficulty bars, languages, and topic strengths move below the activity heatmap
- Activity heatmap scrolls horizontally (52 weeks wide)

---

## Scope boundary — what this update does NOT include

- No follow/follower system (LeetCode has it, we won't)
- No badges system
- No contest ratings
- No profile editing (no "Edit Profile" button — read-only for now)
- No instructor profile variant (same layout, instructor sees all)

---

## Confirmation checklist

Please confirm or comment on each before I write any code:

- [ ] Route: `/profile/:username` — public, no auth required ✓
- [ ] Old `/dashboard` redirects to own profile ✓
- [ ] Left sidebar: avatar + stats + difficulty + languages + topics ✓
- [ ] Right: summary row + difficulty pills + activity heatmap + recent accepted ✓
- [ ] Activity heatmap: 52-week grid (GitHub style), CSS-only (no chart library) ✓
- [ ] Remove `StudentDashboardComponent` entirely ✓
- [ ] Navbar "Dashboard" → "My Profile" link ✓
- [ ] `/progress` stays private and unchanged ✓
- [ ] Mock `PublicProfileData` with `delay(1200)` ✓
- [ ] No profile editing, no followers, no badges ✓
