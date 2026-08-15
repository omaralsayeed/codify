# 📘 Codify — Backend Integration Documentation Index

> **Sprint 1 Complete:** August 11, 2026  
> **Status:** ✅ Ready for testing with live backend  
> **Scope:** Login, Register, Problems List, Problem Detail

---

## 🎯 Start Here

If you're new to this integration work, read the docs in this order:

1. **[INTEGRATION_SUMMARY.md](./INTEGRATION_SUMMARY.md)** — 5-minute overview (start here!)
2. **[BACKEND_INTEGRATION_COMPLETE.md](./BACKEND_INTEGRATION_COMPLETE.md)** — Sprint 1 completion details
3. **[INTEGRATION_JOURNEY.md](./INTEGRATION_JOURNEY.md)** — Deep dive: every file, every decision
4. **[FILE_CHANGE_LOG.md](./FILE_CHANGE_LOG.md)** — Exact line-by-line changes

---

## 📚 Document Reference

### Quick Reference
| Document | Purpose | Audience |
|---|---|---|
| **[INTEGRATION_SUMMARY.md](./INTEGRATION_SUMMARY.md)** | High-level overview, testing checklist | QA, PM, new developers |
| **[BACKEND_INTEGRATION_COMPLETE.md](./BACKEND_INTEGRATION_COMPLETE.md)** | Sprint summary, what's wired, what's mocked | Developers, QA |
| **[FRONTEND_INTEGRATION_GUIDE.md](./FRONTEND_INTEGRATION_GUIDE.md)** | Backend API contract (what they built) | Frontend developers |
| **[API_GUIDE.md](./API_GUIDE.md)** | Original spec (historical) | Backend developers |

### Deep Dive
| Document | Purpose | Audience |
|---|---|---|
| **[INTEGRATION_JOURNEY.md](./INTEGRATION_JOURNEY.md)** | Complete chronological change log | Developers onboarding to the codebase |
| **[FILE_CHANGE_LOG.md](./FILE_CHANGE_LOG.md)** | Exact files touched, line counts | Code reviewers, documentation |
| **[CODEBASE_SCAN.md](./CODEBASE_SCAN.md)** | Pre-integration audit (issues found) | Developers, tech debt planning |

---

## 🚀 Quick Start — Testing Sprint 1

### Prerequisites
1. Backend running at `http://localhost:5237`
2. Database seeded with at least one problem
3. Frontend running at `http://localhost:4200`

### Test Flow (5 minutes)
```bash
# Start backend (separate terminal)
cd backend
dotnet run

# Start frontend
cd frontend
npm start

# Browser: http://localhost:4200
# 1. Register new student
# 2. Verify redirect to /problems
# 3. Click a problem
# 4. Verify dynamic content loads
```

**Full test checklist:** See [BACKEND_INTEGRATION_COMPLETE.md § Testing Checklist](./BACKEND_INTEGRATION_COMPLETE.md#testing-checklist)

---

## ✅ What's Live

| Feature | Status | Component |
|---|---|---|
| Login | ✅ Wired | `AuthService.login()` |
| Register | ✅ Wired | `AuthService.register()` |
| Problems List | ✅ Wired | `ProblemService.getAll()` |
| Problem Detail | ✅ Wired | `ProblemService.getById()` |

---

## ❌ What's Still Mocked

| Feature | Status | Reason |
|---|---|---|
| Run Code | ⚠️ Mocked | Needs Judge0 |
| Submit Code | ⚠️ Mocked | Needs Judge0 |
| AI Hints | ⚠️ Mocked | Needs OpenAI |
| AI Feedback | ⚠️ Mocked | Needs OpenAI |
| Student Progress | ⚠️ Mocked | Sprint 2 |
| Instructor Features | ⚠️ Mocked | Sprint 4 |

**Full breakdown:** See [BACKEND_INTEGRATION_COMPLETE.md § What's Still Mocked](./BACKEND_INTEGRATION_COMPLETE.md#whats-still-mocked)

---

## 📁 Files Changed

### Created (5 files)
```
src/app/core/utils/enum-mappers.ts          ← Enum mapping utilities
INTEGRATION_JOURNEY.md                      ← Complete change log
BACKEND_INTEGRATION_COMPLETE.md             ← Sprint summary
INTEGRATION_SUMMARY.md                      ← Quick reference
FILE_CHANGE_LOG.md                          ← Detailed file changes
README_INTEGRATION.md                       ← This file
```

### Modified (12 files)
```
src/app/core/services/auth.service.ts                              [HTTP calls]
src/app/core/services/problem.service.ts                           [HTTP calls]
src/app/features/problem-list/problem-list.component.{ts,html}     [Async loading]
src/app/features/problem-page/problem-page.component.{ts,html}     [Dynamic data]
src/app/features/instructor/contest-*.component.ts (2 files)       [Compile fixes]
src/app/features/home/components/student-dashboard-preview/*.ts    [Compile fix]
API_GUIDE.md                                                       [Status notice]
```

**Detailed breakdown:** See [FILE_CHANGE_LOG.md](./FILE_CHANGE_LOG.md)

---

## 🔑 Key Concepts

### Enum Mapping
Backend sends enums as integers (`0`, `1`, `2`). Frontend uses strings (`'easy'`, `'medium'`, `'hard'`).

**Utility:** `src/app/core/utils/enum-mappers.ts`

```typescript
import { mapDifficulty, difficultyToNumber } from '@core/utils/enum-mappers';

// Backend → Frontend
mapDifficulty(0) // → 'easy'

// Frontend → Backend
difficultyToNumber('easy') // → 0
```

### Field Renames
| Backend | Frontend |
|---|---|
| `userId` | `id` |
| `fullName` | `name` |
| `statement` | `description` |
| `tags[]` | `topic`, `topicLabel` |
| `sampleTestCases` | `examples` |

**Full mapping table:** See [INTEGRATION_JOURNEY.md § Field Mapping](./INTEGRATION_JOURNEY.md#step-3--problem-service)

### Register → Login Chain
Backend returns **no token** on register. Auto-chain to login:

```typescript
register(data) 
  → POST /api/auth/register (201) 
  → switchMap(() => login(email, password))
  → POST /api/auth/login (200 with token)
```

---

## 🐛 Known Limitations

1. **Starter code hardcoded** — Backend doesn't provide per-language templates
2. **`solvedCount` is 0 in list** — Backend list endpoint doesn't include count
3. **Topic slugs cast `as any`** — Backend tags are free-form, frontend Topic is strict enum
4. **No pagination UI** — Service supports it, component doesn't expose it
5. **Sync fallback methods** — Instructor features use old mock data

**Full details:** See [BACKEND_INTEGRATION_COMPLETE.md § Known Issues](./BACKEND_INTEGRATION_COMPLETE.md#known-issues--limitations)

---

## 🔧 Build Verification

All checks passed:

```bash
# TypeScript
npx tsc --noEmit
# ✅ Zero errors

# Angular build
npx ng build --configuration development
# ✅ Success (Sass warnings are pre-existing)
```

---

## 📞 Support & Questions

### Common Questions

**Q: Why are instructor features still mocked?**  
A: Backend endpoints for instructor dashboard, contests, and analytics aren't built yet (Sprint 4).

**Q: Why does every problem show the same starter code?**  
A: Backend doesn't return `starterCode` in the problem detail response yet.

**Q: Why is `solvedCount` always 0 in the list?**  
A: Backend list endpoint `GET /api/problems` doesn't include `acceptedSubmissionsCount`. Only detail endpoint has it.

**Q: What are the sync fallback methods in ProblemService?**  
A: Temporary stubs (`getAllSync()`, etc.) so mocked instructor components compile. Remove when instructor endpoints are ready.

### Finding Answers

1. Check **[INTEGRATION_SUMMARY.md](./INTEGRATION_SUMMARY.md)** — Quick answers
2. Search **[INTEGRATION_JOURNEY.md](./INTEGRATION_JOURNEY.md)** — Detailed context
3. Review **[FRONTEND_INTEGRATION_GUIDE.md](./FRONTEND_INTEGRATION_GUIDE.md)** — Backend contract

---

## 🎯 Next Sprints

### Sprint 2 — Student Progress & Analytics
**When endpoints ready:**
- Update `ProgressService`
- Update `AnalyticsService`
- Wire profile page
- Wire student progress page

### Sprint 3 — Code Execution & AI
**When Judge0 + OpenAI configured:**
- Update `SubmissionService` (run + submit)
- Update `HintService`
- Wire feedback endpoint

### Sprint 4 — Instructor Features
**When endpoints ready:**
- Update `InstructorService`
- Update `ContestService`
- Remove sync fallback methods from `ProblemService`
- Async-refactor instructor components

---

## 📊 Integration Progress

```
Phase 1 (Sprint 1) ████████████████████████ 100% ✅ COMPLETE
├─ Login          ✅
├─ Register       ✅
├─ Problems List  ✅
└─ Problem Detail ✅

Phase 2 (Sprint 2) ░░░░░░░░░░░░░░░░░░░░░░░░   0% ⏸️ PENDING
├─ Student Progress
├─ Analytics
└─ Dashboard

Phase 3 (Sprint 3) ░░░░░░░░░░░░░░░░░░░░░░░░   0% ⏸️ PENDING
├─ Run Code
├─ Submit Code
├─ AI Hints
└─ AI Feedback

Phase 4 (Sprint 4) ░░░░░░░░░░░░░░░░░░░░░░░░   0% ⏸️ PENDING
├─ Instructor Dashboard
├─ Contests
└─ Class Analytics
```

---

## 🔗 External Links

- **Backend:** http://localhost:5237
- **Swagger UI:** http://localhost:5237/swagger
- **Frontend:** http://localhost:4200

---

**Last Updated:** August 11, 2026  
**Maintained By:** Kiro (AI dev agent)
