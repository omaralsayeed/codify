# Sprint Status — Bug Fix Batch 1

> **Date:** August 22, 2026  
> **Sprint:** Bug Fix Batch 1  
> **Status:** 🏁 CLOSED — All 5 issues resolved + 3 follow-up polish changes applied

---

## Summary Table

| # | Issue | Owner | Status |
|---|-------|-------|--------|
| 1 | Runtime & memory not shown after submit | Frontend | ✅ Done |
| 2 | Submission history missing in submissions tab | Frontend | ✅ Done |
| 3 | Settings icon does nothing | Frontend | ✅ Done — icon now fully removed from UI |
| 4 | Filter by topic broken (exact match) | Backend | ✅ Done |
| 5 | Instructor submit gives 403 / button missing | Frontend + Backend | ✅ Done |

---

## Follow-up Polish (applied after backend confirmed)

| Change | Status |
|--------|--------|
| Settings gear icon removed for all roles (students + instructors) | ✅ Done |
| Submit button now visible for instructors (backend updated to allow `Student,Instructor`) | ✅ Done |
| Submission history redesigned — numbered rows, stacked status+date, language pill, icons | ✅ Done |

---

## Sprint Tree

```
Sprint: Bug Fix Batch 1 — CLOSED ✅
│
├── 🎨 FRONTEND — ALL DONE
│   │
│   ├── F1  Runtime & memory display         ✅  Bindings already wired — verified, no change needed
│   ├── F2  Submission history tab           ✅  API + redesigned table (numbered, icons, pill lang)
│   ├── F3  Settings icon                    ✅  Removed from DOM entirely (no role needed it)
│   └── F5  Submit button                    ✅  canSubmit = student OR instructor
│
└── 🔧 BACKEND — ALL DONE
    ├── B4  Tag filter: == → LIKE            ✅  EF.Functions.Like() in ProblemRepository.cs
    └── B5  Instructor submit role           ✅  "Student,Instructor" in SubmissionsController.cs
```

---

## Issue Details

---

### ✅ F1 — Runtime & Memory After Submit

No code change needed. Getters and HTML bindings were already in place in both the result banner and the result panel stats row.

---

### ✅ F2 — Submission History Tab

Table redesigned to match design reference:

| Column | Content |
|--------|---------|
| `#` | Row number descending (newest = highest number) |
| Status | Bold colored text (`Accepted` green / rejected red) + date below in grey |
| Language | Rounded pill badge (e.g. `C++`) |
| Runtime | Clock icon + `X ms` |
| Memory | Chip icon + `X.X MB` |

**Files:** `submission.model.ts` · `submission.service.ts` · `problem-page.component.ts` · `problem-page.component.html` · `problem-page.component.scss`

---

### ✅ F3 — Settings Icon Removed

Settings gear completely removed from the toolbar DOM for all users. The modal code remains in the component in case it's needed later, but no icon triggers it.

**File:** `problem-page.component.html`

---

### ✅ F5 — Submit Button for Instructors

`canSubmit` getter updated:

```typescript
// Before — students only
get canSubmit(): boolean {
  return role === 'student';
}

// After — students + instructors (backend now allows both)
get canSubmit(): boolean {
  return role === 'student' || role === 'instructor';
}
```

All 3 submit button sites (toolbar, bottom panel, mobile bar) now show for both roles.

**File:** `problem-page.component.ts`

---

### ✅ B4 — Tag Filter (Backend)

```csharp
// Applied in ProblemRepository.cs
query = query.Where(p => p.ProblemTags.Any(
    pt => EF.Functions.Like(pt.ConceptTag.Name, $"%{filter.Tag}%")));
```

---

### ✅ B5 — Instructor Submit Role (Backend)

```csharp
// Applied in SubmissionsController.cs
[Authorize(Roles = "Student,Instructor")]
```

---

## Final Checklist

- [x] F1 — Runtime & memory render from backend response
- [x] F2 — Submissions tab: numbered rows, stacked status+date, lang pill, icons
- [x] F3 — Settings gear icon removed from toolbar for all users
- [x] F5 — Submit button visible for students and instructors
- [x] B4 — Tag filter uses `LIKE` (partial, case-insensitive)
- [x] B5 — Instructor role allowed on `POST /api/submissions`
