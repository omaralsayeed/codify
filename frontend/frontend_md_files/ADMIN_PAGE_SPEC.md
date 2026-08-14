# Codify — Admin Panel Specification

> **Created:** August 14, 2026
> **Branch:** `admin-page`
> **Status:** Planning — not yet implemented
> **Author:** Kiro (AI dev agent)

---

## Overview

The Admin Panel is a separate dashboard accessible only to users with `role = 'admin'`. It sits at `/admin/dashboard` and gives platform administrators full control over users, instructors, and problems. It is completely separate from the Instructor dashboard.

Admin capabilities (as requested + recommended additions):
- View all users (students + instructors) with filters
- Activate or set any user/instructor to pending
- Add, edit, and delete problems
- Platform-wide statistics (user counts, problem counts, submission counts)

---

## Route Plan

| Route | Component | Guard |
|---|---|---|
| `/admin/dashboard` | AdminShellComponent | authGuard + adminGuard |
| `/admin/dashboard/overview` | AdminOverviewComponent | authGuard + adminGuard |
| `/admin/dashboard/users` | AdminUsersComponent | authGuard + adminGuard |
| `/admin/dashboard/users/:id` | AdminUserDetailComponent | authGuard + adminGuard |
| `/admin/dashboard/problems` | AdminProblemsComponent | authGuard + adminGuard |
| `/admin/dashboard/problems/new` | AdminProblemFormComponent | authGuard + adminGuard |
| `/admin/dashboard/problems/:id/edit` | AdminProblemFormComponent | authGuard + adminGuard |

---

## Features — Detailed Breakdown

---

### Feature 1 — Platform Overview (Stats Dashboard)

**Priority: 🔴 High — build first**

A summary page showing key platform metrics at a glance. First page the admin sees on login.

#### What It Shows
- Total registered users (students + instructors combined)
- Total students count
- Total instructors count (broken down: active vs pending)
- Total problems in the platform
- Total submissions (all time)
- New registrations today / this week
- Platform activity chart (submissions per day, last 14 days)

#### Stat Cards (top row)
```
[ Total Users ]   [ Total Problems ]   [ Pending Instructors ]   [ Submissions Today ]
     124                 32                      3                        87
  ↑ 5 this week     ↑ 2 this week           needs review              vs 64 yesterday
```

#### Activity Chart
- Line chart: submissions per day over the last 14 days
- Reuses the same chart pattern from InstructorOverviewComponent

---

#### Frontend Spec

**Component:** `AdminOverviewComponent`

**Data needed:**
```typescript
interface AdminStats {
  totalUsers: number;
  totalStudents: number;
  totalInstructors: number;
  activeInstructors: number;
  pendingInstructors: number;
  totalProblems: number;
  totalSubmissions: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  submissionsToday: number;
  activityTrend: { date: string; dayLabel: string; submissions: number; }[];
}
```

**UI elements:**
- 4 stat cards in a row (responsive grid)
- Activity trend SVG chart (bezier curve, same pattern as instructor overview)
- Secondary stats row (instructors breakdown, new users)

**State:** All data fetched on `ngOnInit`, loading skeleton shown while fetching.

---

#### Backend Spec

**New endpoint:**
```
GET /api/admin/stats
Authorization: Bearer <token> [Admin role required]
```

**Response:**
```json
{
  "data": {
    "totalUsers": 124,
    "totalStudents": 118,
    "totalInstructors": 6,
    "activeInstructors": 3,
    "pendingInstructors": 3,
    "totalProblems": 32,
    "totalSubmissions": 4820,
    "newUsersToday": 5,
    "newUsersThisWeek": 18,
    "submissionsToday": 87,
    "activityTrend": [
      { "date": "2026-08-01", "dayLabel": "Sat 1", "submissions": 45 },
      ...
    ]
  }
}
```

---

### Feature 2 — User Management (View All Users)

**Priority: 🔴 High — build second**

A full table of every registered user on the platform with search, filter, and sort.

#### What It Shows
- Full name + avatar initials
- Email address
- Role badge (Student / Instructor / Admin)
- Status badge (Active / Pending) — only relevant for instructors
- Registration date
- Last active date
- Problems solved (students) / students managed (instructors)
- Action button: View Details

#### Filters (top toolbar)
- Search by name or email (text input)
- Filter by role: All / Students / Instructors
- Filter by status: All / Active / Pending
- Sort by: Name / Date Registered / Last Active

#### Instructor-specific: Activate / Set Pending
- Instructors have a status pill: green "Active" or orange "Pending"
- Clicking the pill OR opening the user detail shows a toggle/button
- Admin can flip the status in one click
- A confirmation modal appears: "Are you sure you want to activate [name]?"

---

#### Frontend Spec

**Component:** `AdminUsersComponent`

**Data model:**
```typescript
interface AdminUserRow {
  id: string;
  name: string;
  initials: string;
  email: string;
  role: 'student' | 'instructor' | 'admin';
  status: 'active' | 'pending';       // always 'active' for students
  registeredAt: string;               // ISO date
  lastActiveAt: string | null;
  problemsSolved?: number;            // students
  organization?: string;              // instructors
}
```

**UI elements:**
- Toolbar: search input + role filter dropdown + status filter dropdown
- Sortable table (Name, Role, Status, Registered, Last Active)
- Role badge: blue = Student, gold = Instructor, red = Admin
- Status pill: green = Active, orange = Pending
- Row click → navigates to `/admin/dashboard/users/:id`
- Inline action button on instructor rows: "Activate" (if pending) or "Set Pending" (if active)
- Confirmation modal for status changes

**State signals:**
```typescript
searchQuery = signal('')
roleFilter  = signal<'all' | 'student' | 'instructor'>('all')
statusFilter = signal<'all' | 'active' | 'pending'>('all')
sortField   = signal<'name' | 'registeredAt' | 'lastActiveAt'>('registeredAt')
sortDir     = signal<'asc' | 'desc'>('desc')
users       = signal<AdminUserRow[]>([])
isLoading   = signal(true)
```

---

#### Backend Spec

**New endpoints:**

```
GET  /api/admin/users
GET  /api/admin/users/:id
PATCH /api/admin/users/:id/status
```

**GET /api/admin/users**

Query params:
| Param | Type | Notes |
|---|---|---|
| `role` | `student` \| `instructor` \| `admin` | Filter by role |
| `status` | `active` \| `pending` | Filter by status |
| `search` | string | Name or email search |
| `sortBy` | `name` \| `registeredAt` \| `lastActiveAt` | Sort field |
| `sortDir` | `asc` \| `desc` | Sort direction |
| `page` | number | Pagination |
| `pageSize` | number | Default 20 |

**Response:**
```json
{
  "data": {
    "users": [
      {
        "id": "uuid",
        "name": "Jane Doe",
        "initials": "JD",
        "email": "jane@example.com",
        "role": 0,
        "status": "active",
        "registeredAt": "2026-07-01T10:00:00Z",
        "lastActiveAt": "2026-08-13T14:22:00Z",
        "problemsSolved": 14,
        "organization": null
      }
    ],
    "total": 124,
    "page": 1,
    "pageSize": 20
  }
}
```

**PATCH /api/admin/users/:id/status**

Request body:
```json
{ "status": "active" }
```
or
```json
{ "status": "pending" }
```

Response `200`: Updated user object.
Response `400`: Invalid status value.
Response `404`: User not found.
Response `403`: Cannot change status of another admin.

---

### Feature 3 — User Detail Page

**Priority: 🟡 Medium — build after users list**

A full profile view for any user from the admin perspective.

#### What It Shows
- All profile fields (name, email, role, status, organization, registered date)
- For students: problems solved, avg score, streak, recent submissions
- For instructors: pending/active status with change button, organization, recent activity
- Danger zone: ability to change role (with confirmation)

---

#### Frontend Spec

**Component:** `AdminUserDetailComponent`

**Data model:**
```typescript
interface AdminUserDetail extends AdminUserRow {
  streak?: number;
  avgScore?: number;
  totalSubmissions?: number;
  recentSubmissions?: {
    problemTitle: string;
    status: string;
    submittedAt: string;
  }[];
}
```

**UI elements:**
- Back button → returns to users list
- Profile header: avatar initials, name, email, role badge, status pill
- Stats row (for students): problems solved, avg score, streak
- Recent submissions table (last 5)
- For instructors: status section with Activate / Set Pending button + confirmation
- Danger zone section (role change) — visually separated, red border

---

#### Backend Spec

**GET /api/admin/users/:id**

Response: Full user detail (same as user row + extra fields above).

---

### Feature 4 — Problem Management (View + Add + Edit)

**Priority: 🔴 High — build alongside users**

Full CRUD for problems. Admin can see all problems, add new ones, edit existing ones.

#### Problem List View

**Filters:**
- Search by title
- Filter by difficulty: All / Easy / Medium / Hard
- Filter by topic/tag: All / Arrays / Graphs / Trees / etc.
- Filter by status: All / Active / Inactive
- Sort by: Title / Difficulty / Solved Count / Created Date

**Table columns:**
- Title
- Difficulty badge (Easy / Medium / Hard)
- Tags (joined: "Arrays · Hash Map")
- Solved count
- Status (Active / Inactive toggle)
- Created date
- Actions: Edit button | Deactivate/Activate toggle

#### Problem Form (Add / Edit)

A form page used for both creating new problems and editing existing ones.

**Fields:**
```
Title *                     [text input]
Difficulty *                [dropdown: Easy / Medium / Hard]
Tags *                      [multi-select or comma-separated input]
Problem Statement *         [large textarea]
Constraints                 [textarea — one per line]
Sample Test Cases           [repeatable block: Input + Expected Output]
Active                      [toggle — default true]
Time Limit (ms)             [number — default 2000]
Memory Limit (MB)           [number — default 256]
```

**Validation:**
- Title: required, min 3 chars
- Difficulty: required
- Tags: at least 1
- Statement: required, min 50 chars
- At least 1 sample test case

**On submit:**
- POST `/api/problems` (new) or PATCH `/api/problems/:id` (edit)
- Success → redirect back to problems list with success toast
- Error → inline validation messages

---

#### Frontend Spec

**Components:**
- `AdminProblemsComponent` — list with filters, table, action buttons
- `AdminProblemFormComponent` — shared form for create + edit (reads `:id` from route if editing)

**Data model:**
```typescript
interface AdminProblemRow {
  id: string;
  title: string;
  difficulty: 'easy' | 'medium' | 'hard';
  tags: string[];
  solvedCount: number;
  isActive: boolean;
  createdAt: string;
}

interface AdminProblemForm {
  title: string;
  difficulty: 'easy' | 'medium' | 'hard';
  tags: string[];
  statement: string;
  constraints: string;        // newline-separated
  sampleTestCases: {
    input: string;
    expectedOutput: string;
  }[];
  isActive: boolean;
  timeLimitMs: number;
  memoryLimitMb: number;
}
```

**State for list:**
```typescript
searchQuery      = signal('')
difficultyFilter = signal<'all' | 'easy' | 'medium' | 'hard'>('all')
tagFilter        = signal<string>('all')
statusFilter     = signal<'all' | 'active' | 'inactive'>('all')
sortField        = signal<'title' | 'difficulty' | 'solvedCount' | 'createdAt'>('createdAt')
sortDir          = signal<'asc' | 'desc'>('desc')
problems         = signal<AdminProblemRow[]>([])
isLoading        = signal(true)
```

---

#### Backend Spec

**New/updated endpoints:**

```
GET    /api/admin/problems              — paginated list with filters (admin view, includes inactive)
POST   /api/problems                   — create problem [Admin only]
PATCH  /api/problems/:id               — update problem fields [Admin only]
PATCH  /api/problems/:id/status        — toggle active/inactive [Admin only]
DELETE /api/problems/:id               — soft delete (sets isActive = false) [Admin only]
```

**Note:** `GET /api/problems` already exists for students/instructors but only returns `isActive = true`. Admin gets a separate endpoint that returns all problems including inactive ones.

**POST /api/problems** request body:
```json
{
  "title": "Two Sum",
  "difficulty": 0,
  "tags": ["Arrays", "Hash Map"],
  "statement": "Given an array of integers...",
  "constraints": "2 <= nums.length <= 10^4\n-10^9 <= nums[i] <= 10^9",
  "sampleTestCases": [
    { "input": "nums = [2,7,11,15], target = 9", "expectedOutput": "[0,1]" }
  ],
  "isActive": true,
  "timeLimitMs": 2000,
  "memoryLimitMb": 256
}
```

**PATCH /api/problems/:id** — same shape, all fields optional (partial update).

**Response `201`:** Created problem (full shape matching GET /api/problems/:id).
**Response `400`:** Validation error.
**Response `403`:** Not an admin.

---

### Feature 5 — Admin Navbar Link (Navigation Entry Point)

**Priority: 🔴 High — needed for any admin to reach the panel**

Add an "Admin" link in the navbar and profile dropdown, visible only when `user.role === 'admin'`.

---

#### Frontend Spec

In `navbar.component.ts` — add getter:
```typescript
get isAdmin(): boolean {
  return this.auth.user()?.role === 'admin';
}
```

In `navbar.component.html` — add in 3 places (same pattern as `isInstructor`):

1. Desktop nav links:
```html
@if (isAdmin) {
  <a routerLink="/admin/dashboard" routerLinkActive="nav-link--active"
     class="nav-link nav-link--admin">Admin</a>
}
```

2. Desktop profile dropdown quick actions:
```html
@if (isAdmin) {
  <button class="quick-action quick-action--admin"
          (click)="navigateTo('/admin/dashboard')">
    🛡️ <span>Admin Panel</span>
  </button>
}
```

3. Mobile menu:
```html
@if (isAdmin) {
  <a routerLink="/admin/dashboard" class="mobile-link mobile-link--admin"
     (click)="isMobileMenuOpen=false">Admin Panel</a>
}
```

No backend work needed for this feature.

---

### Feature 6 — Admin Guard + Role Enforcement

**Priority: 🔴 High — security, must exist before any admin route**

A route guard that blocks non-admin users from accessing `/admin/**`.

---

#### Frontend Spec

**File:** `src/app/core/guards/admin.guard.ts`

```typescript
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isLoggedIn()) {
    router.navigate(['/auth/login']);
    return false;
  }

  if (authService.user()?.role === 'admin') {
    return true;
  }

  router.navigate(['/']);
  return false;
};
```

Also requires updating `User` model, `enum-mappers.ts`, and `auth.service.ts` to recognize `role = 'admin'` (role = 2 in backend).

No backend changes needed for the guard itself — the backend already enforces `[Authorize(Roles = "Admin")]` on admin endpoints.

---

## Recommended Additional Features (Future Sprints)

These were not in the original request but are strongly recommended for a complete admin panel.

| # | Feature | Why | Priority |
|---|---|---|---|
| A | **Audit Log** | Track every admin action (who activated who, who edited which problem) — critical for accountability | 🟡 Medium |
| B | **Bulk Actions** | Select multiple users → bulk activate/deactivate. Makes managing large cohorts fast | 🟡 Medium |
| C | **Problem Test Cases Manager** | Full CRUD for hidden test cases (not just sample ones) — currently no UI for this | 🟡 Medium |
| D | **Role Change** | Admin can promote a student to instructor or demote an instructor | 🟡 Medium |
| E | **Admin Notifications** | Bell icon shows new pending instructor requests so admin doesn't have to manually check | 🟠 Low-Med |
| F | **Export Data** | Export user list or submission stats as CSV | 🟢 Low |
| G | **Announcement Banner** | Admin posts a platform-wide banner message (e.g. maintenance notice) | 🟢 Low |
| H | **Problem Import/Export** | Bulk import problems via JSON/CSV — useful when seeding a new instance | 🟢 Low |

---

## Implementation Priority Order

Work through features in this exact order:

```
Sprint 1 (Foundation — do these first, they unblock everything)
├── Feature 6: Admin Guard + Role System update     [~1 hour]
├── Feature 5: Admin navbar link                    [~30 min]
└── Admin Shell + Routes scaffold                   [~1 hour]

Sprint 2 (Core Pages)
├── Feature 1: Overview / Stats dashboard           [~3 hours]
├── Feature 2: User Management list                 [~4 hours]
└── Feature 3: User Detail page                     [~2 hours]

Sprint 3 (Problem Management)
├── Feature 4a: Problem list with filters           [~3 hours]
└── Feature 4b: Problem add/edit form              [~4 hours]

Sprint 4 (Polish + Extras)
├── Feature A: Audit log                            [~3 hours]
├── Feature B: Bulk actions                         [~2 hours]
└── Feature D: Role change                          [~1 hour]
```

---

## Current State (August 14, 2026)

| Item | Status |
|---|---|
| Branch `admin-page` created | ✅ Done |
| Admin guard | ❌ Not built |
| Role system updated (user model, enum-mappers, auth service) | ❌ Not built |
| Admin shell + routes | ❌ Not built |
| Admin navbar link | ❌ Not built |
| Overview page | ❌ Not built |
| User management | ❌ Not built |
| Problem management | ❌ Not built |
| Backend endpoints | ❌ Not built |

---

## Backend Endpoint Summary

All admin endpoints require `[Authorize(Roles = "Admin")]`.

| Method | Endpoint | Feature | Priority |
|---|---|---|---|
| GET | `/api/admin/stats` | Overview dashboard | 🔴 High |
| GET | `/api/admin/users` | User list | 🔴 High |
| GET | `/api/admin/users/:id` | User detail | 🟡 Medium |
| PATCH | `/api/admin/users/:id/status` | Activate/pending toggle | 🔴 High |
| GET | `/api/admin/problems` | Problem list (incl. inactive) | 🔴 High |
| POST | `/api/problems` | Create problem | 🔴 High |
| PATCH | `/api/problems/:id` | Edit problem | 🔴 High |
| PATCH | `/api/problems/:id/status` | Toggle active/inactive | 🟡 Medium |
| DELETE | `/api/problems/:id` | Soft delete problem | 🟡 Medium |

---

*Spec written by Kiro — August 14, 2026*
