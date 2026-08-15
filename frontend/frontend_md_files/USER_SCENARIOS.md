# Codify – User Scenarios

---

## STUDENT SCENARIOS

---

### Scenario 1 — Solve a Problem (Code Editor Flow)

```
[Login Page]  localhost:4200/auth/login
     │
     ▼
[Home Page]  localhost:4200/
     │
     │  clicks "Problems" in nav
     ▼
[Problem List]  localhost:4200/problems
     │
     │  clicks a problem card (e.g. "Two Sum")
     ▼
[Problem Page]  localhost:4200/problems/:id
     │
     ├──── Left Panel ─────────────────────────────────────────────────────────
     │         │
     │         ├── Tab: Description
     │         │      - Problem title + difficulty badge (Easy / Medium / Hard)
     │         │      - Problem statement, examples, constraints
     │         │      - Action pills: Topics · Companies · Hint
     │         │
     │         ├── Tab: Editorial
     │         │      - Unlocked after solving the problem
     │         │
     │         ├── Tab: Solutions
     │         │      - Community solutions panel
     │         │
     │         ├── Tab: Submissions
     │         │      - Personal submission history
     │         │
     │         └── Tab: Codify (AI Hint)
     │                - Empty state  →  click "AI Hint" in toolbar
     │                - Hint 1/3  →  gentle nudge
     │                - Hint 2/3  →  deeper guidance
     │                - Hint 3/3  →  near-solution hint
     │                - "Ask Codify" button  →  opens AI chat hand-off
     │
     ├──── Toolbar (top) ──────────────────────────────────────────────────────
     │         │
     │         ├── Run  →  executes code against sample test cases
     │         │
     │         ├── Submit
     │         │      ├── [Submitting…]  spinner state
     │         │      ├── ✅ Accepted    toolbar flashes green
     │         │      └── ❌ Wrong Answer / Runtime Error / TLE  flashes red
     │         │
     │         ├── Copy  →  copies editor content to clipboard
     │         │
     │         ├── AI Hint (lightbulb)
     │         │      ├── Loading  →  spinner
     │         │      ├── Available (levels 1–3)
     │         │      └── Exhausted  →  button disabled, "All hints revealed"
     │         │
     │         └── Settings ⚙  →  editor preferences
     │
     └──── Right Panel (Code Editor) ──────────────────────────────────────────
               │
               ├── Language selector  (Python, JavaScript, Java, C++, …)
               ├── Autocomplete toggle
               ├── Fullscreen toggle
               └── Code textarea with line numbers
```

**Dead ends / terminal states inside this scenario:**

| Situation | What the user sees |
|---|---|
| Hint level 3 exhausted | Button disabled, message: "All hints revealed. Time to code it up!" |
| Submission → Accepted | Green flash on Submit button; mark-as-solved checkmark activates |
| Submission → Wrong Answer / Runtime Error / TLE | Red flash on Submit button; user stays on the problem to retry |
| Left panel collapsed | Problem description hidden; expand arrow shown on editor side |
| Right panel collapsed | Editor hidden; problem description fills full width |

---

### Scenario 2 — View Profile & Progress

```
[Login Page]  localhost:4200/auth/login
     │
     ▼
[Home Page]  localhost:4200/
     │
     ├── Option A: click avatar / username in nav
     │        redirects to  localhost:4200/dashboard
     │        which resolves to  localhost:4200/profile/:username
     │
     └── Option B: navigate directly to /progress
              redirects to  localhost:4200/progress

─────────────────────────────────────────────────────────────────────────────

PATH A — Profile Page  localhost:4200/profile/:username
     │
     ├──── Left Sidebar ───────────────────────────────────────────────────────
     │         ├── Avatar circle (initials + color)
     │         ├── Name, @username, role badge
     │         ├── "Edit Profile" button  (only on own profile)
     │         ├── 🔥 current streak  +  📅 joined year
     │         ├── Problems Solved — animated bars (Easy / Med. / Hard)
     │         ├── Languages — bar chart of languages used
     │         └── Topics — Strong / Average pills
     │
     └──── Main Content ───────────────────────────────────────────────────────
               ├── Bio card
               │      - Headline, social links (LinkedIn / GitHub / X), bio text
               │
               ├── Stats card
               │      - Solved ring (donut: Easy · Med · Hard)
               │      - 🔥 current streak counter + personal-best badge
               │      - Joined year  +  Active Days
               │
               ├── Activity Heatmap
               │      - Year filter pills (2024 / 2025 / All)
               │      - Submission heat grid
               │      - Header: total submissions · active days · max streak
               │
               └── Recent Accepted submissions
                      - Problem title, difficulty badge, language badge, time ago
                      - Clicking a row  →  navigates to  /problems/:id
                      - Empty state: "No accepted submissions yet — solve your first problem"

─────────────────────────────────────────────────────────────────────────────

PATH B — Progress Page  localhost:4200/progress
     │
     ├── Hero stats (animated count-up)
     │      - Total Attempted  ·  Total Solved  ·  Success Rate %  ·  Streak
     │
     ├── Activity dots — last 7 days (staggered animation)
     │
     ├── Success Rate Chart
     │      - Time-range toggle: 7 Days · 30 Days · 3 Months
     │
     ├── Difficulty Donut Chart
     │
     ├── Topic Radar Chart
     │
     ├── Topic Performance bars
     │      - Sorted: Weak → Average → Strong
     │      - Each bar shows strength score + badge color
     │
     ├── Hint Usage stats
     │      - Total hints used  ·  Avg hints/problem
     │      - Solved with zero hints  ·  Solved using all 3 hints
     │
     ├── Focus Areas  (up to 3 weakest topics)
     │      - "Practice" button  →  navigates to /problems?topic=<slug>
     │
     └── Recommendations
            - Problem cards suggested based on weak topics
            - Clicking  →  navigates to /problems/:id
```

**Dead ends / terminal states in this scenario:**

| Situation | What the user sees |
|---|---|
| Profile of another user | "Edit Profile" button hidden; read-only view |
| No accepted submissions | Empty state with link to /problems |
| No profile found | Error alert: profile-error message |
| Progress page, no data yet | Skeleton loaders, then zero-state analytics |

---
---

## INSTRUCTOR SCENARIOS

> An instructor account also has the full Student role, so **Scenario 1** and
> **Scenario 2** above apply exactly. The instructor simply has an additional
> entry point: the **Teach** button / nav link.

---

### Scenario 3 — Instructor Dashboard

```
[Login Page]  localhost:4200/auth/login
     │
     ▼
[Home Page]  localhost:4200/
     │
     │  clicks "Teach" (instructor-only nav item)
     ▼
[Instructor Shell]  localhost:4200/instructor/dashboard
     │
     │  auto-redirects to Overview
     ▼
[Overview]  localhost:4200/instructor/dashboard/overview
     │
     ├── Sidebar nav ─────────────────────────────────────────────────────────
     │         ├── Overview     → /instructor/dashboard/overview
     │         ├── Students     → /instructor/dashboard/students
     │         ├── Integrity    → /instructor/dashboard/integrity
     │         └── Contests     → /instructor/dashboard/contests
     │
     ├──── Overview  (/overview) ──────────────────────────────────────────────
     │         - Class-wide analytics at a glance
     │
     ├──── Students  (/students) ──────────────────────────────────────────────
     │         │
     │         │  clicks a student row
     │         ▼
     │      [Student Detail]  /instructor/dashboard/students/:id
     │         - Individual student analytics, submission history,
     │           topic breakdown
     │
     ├──── Integrity  (/integrity) ────────────────────────────────────────────
     │         - Plagiarism / academic-integrity monitoring
     │
     └──── Contests  (/contests) ──────────────────────────────────────────────
               │
               ├── Contest list  →  /instructor/dashboard/contests
               │
               ├── clicks "New Contest"
               │        ▼
               │     [Create Contest]  /instructor/dashboard/contests/new
               │         - Fill in title, date range, problems, rules
               │         - Save  →  redirects back to contest list
               │
               └── clicks an existing contest
                        ▼
                     [Contest Detail]  /instructor/dashboard/contests/:id
                         - Stats, leaderboard, submission breakdown
```

**Route guard behaviour:**

| Guard | Behaviour |
|---|---|
| `authGuard` | Redirects unauthenticated users to `/auth/login` |
| `instructorGuard` | Redirects non-instructor users away from `/instructor/**` |

**Dead ends / terminal states for instructor:**

| Situation | What the user sees |
|---|---|
| Not logged in and tries /instructor/… | Redirected to /auth/login |
| Logged in as student and tries /instructor/… | Blocked by instructorGuard, redirected to / |
| Contest created successfully | Redirected back to contests list with new entry visible |
| Student detail: no data | Empty / loading state for that student |

---

## Full Route Reference

| Screen | URL | Auth required |
|---|---|---|
| Home | `/` | No |
| Login | `/auth/login` | No |
| Register | `/auth/register` | No |
| Forgot Password | `/auth/forgot-password` | No |
| Problem List | `/problems` | ✅ Yes |
| Problem Page (editor) | `/problems/:id` | ✅ Yes |
| Profile | `/profile/:username` | No (public) |
| Dashboard (redirect) | `/dashboard` | ✅ Yes (redirects to own profile) |
| Progress | `/progress` | ✅ Yes |
| Instructor Overview | `/instructor/dashboard/overview` | ✅ + instructor role |
| Instructor Students | `/instructor/dashboard/students` | ✅ + instructor role |
| Student Detail | `/instructor/dashboard/students/:id` | ✅ + instructor role |
| Integrity | `/instructor/dashboard/integrity` | ✅ + instructor role |
| Contests | `/instructor/dashboard/contests` | ✅ + instructor role |
| Create Contest | `/instructor/dashboard/contests/new` | ✅ + instructor role |
| Contest Detail | `/instructor/dashboard/contests/:id` | ✅ + instructor role |
