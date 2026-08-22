# What We Need From Backend — Detailed Handoff

> **Date:** August 21, 2026
> **Written by:** Frontend team
> **Purpose:** Precise list of everything the frontend is waiting on from the backend team,
> with exact request/response contracts, our current frontend status per item, and
> what unblocks automatically once each item is delivered.
>
> **Last updated:** August 21, 2026 — backend status updated to reflect actual codebase state.

---

## Quick Status Overview

| # | Feature | Backend Status | Frontend Status | Priority |
|---|---------|---------------|-----------------|----------|
| 1 | Judge0 — Run Code | ✅ Endpoint exists + fully wired to Judge0 client, ⚠️ Judge0 service must be running at `localhost:2358` | ✅ Fully wired (Python + C#) | 🔴 High |
| 2 | Judge0 — Submit + Poll | ✅ Endpoint exists + async background queue implemented, ⚠️ Judge0 service must be running at `localhost:2358` | ✅ Fully wired (Python + C#) | 🔴 High |
| 3 | AI Hints | ✅ Endpoint exists + Tutor Agent implemented, ✅ OpenAI key IS configured (`appsettings.json`) | ✅ Fully wired | 🔴 High |
| 4 | Avatar URL in login response | ✅ Built — `avatarUrl` is in `LoginResponse`, DB column exists, returned from `AuthService` | ✅ Wired (fire-and-forget) | 🔴 High |
| 5 | PUT /api/auth/avatar | ✅ Built — endpoint exists, validates Cloudinary URL, persists to DB | ✅ Wired (fire-and-forget) | 🔴 High |
| 6 | Contests (all 5 endpoints) | ✅ All 5 contest endpoints exist in `ContestsController` + `IContestService` wired | ✅ HTTP calls written, graceful fallback | 🟡 Medium |
| 7 | AI Submission Feedback | ✅ Endpoint exists (`GET /api/submissions/:id/feedback`), `GetFeedbackAsync` implemented — ⚠️ response shape differs from frontend expectation (see §7) | ✅ Wired, mocked response | 🟡 Medium |
| 8 | PUT /api/auth/profile | ✅ Built — `PUT /api/auth/profile` exists, `UpdateProfileAsync` persists to DB | ✅ Wired, graceful fallback | 🟡 Medium |
| 9 | acceptedSubmissionsCount in problem list | ❌ Field exists on `Problem` entity and in `ProblemDetailResponse` but **missing from `ProblemSummaryResponse`** — `MapToSummary()` does not include it | ✅ Shows 0 as fallback | 🟡 Medium |
| 10 | Starter code per problem | ⚠️ `StarterCode` field exists on `CodeTemplate` entity and `GetStarterCode()` in `ICodeWrapperService`, but **not exposed in `ProblemDetailResponse`** | ⚠️ Hardcoded "Two Sum" pattern | 🟡 Medium |
| 11 | POST /api/auth/forgot-password | ❌ Not built — no endpoint, no service method | ⚠️ Form exists, fully mocked | 🟢 Low |
| 12 | GET /api/problems/recommended | ❌ Not built | ⚠️ Returns first 3 from getAll() | 🟢 Low |

---

## Infrastructure Checklist

| Item | Affects | Status |
|------|---------|--------|
| Judge0 running at `http://localhost:2358` | Run code (#1), Submit/Poll (#2) | ⚠️ Client is configured and wired — Judge0 Docker container must be started (`judge0/docker-compose.yml`) |
| OpenAI API key | AI Hints (#3), AI Feedback (#7) | ✅ Key is present in `appsettings.json` under `OpenAI:ApiKey`, pointing to ITI proxy at `apiaccess.iti.net.eg` |
| `AvatarUrl` column in Users table | Avatar persistence (#4, #5) | ✅ Column exists (`nvarchar(500) NULL`), migration applied |

---

## 1. 🔴 Judge0 — Run Code

**Endpoint:** `POST /api/execution/run`

**Backend actual status:**
The endpoint exists in `ExecutionController`. A full `Judge0Client` is implemented (`Judge0Client.cs`) that POSTs to Judge0, polls for a result, and maps it back. The `appsettings.json` has `Judge0:BaseUrl = "http://localhost:2358"` with empty `ApiKey` and `ApiHost` (correct for a self-hosted instance).

**The only blocker:** Judge0 must be running. Use the Docker Compose file at `judge0/docker-compose.yml`.

**Note on auth:** The `[Authorize(Roles = "Student")]` attribute on the `run` endpoint means the JWT must be present and the user must have role `Student`. Make sure the frontend is sending the Bearer token.

**Our frontend status:**
The HTTP call is fully written in `SubmissionService.run()`. For Python and C# it hits the real backend endpoint. For JavaScript, Java, C++ it uses a local mock (those languages have no judge support yet — we are aware). The moment Judge0 is running and the endpoint returns the correct shape, everything works automatically — no frontend changes needed.

**Request we send (already sending this):**
```json
POST /api/execution/run
Authorization: Bearer <jwt>

{
  "problemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "def twoSum(nums, target):\n    seen = {}\n    for i, n in enumerate(nums):\n        if target - n in seen:\n            return [seen[target - n], i]\n        seen[n] = i",
  "language": "Python"
}
```

**Language values we send:** `"Python"` or `"CSharp"` (string, not number)

**Response shape we expect:**
```json
{
  "data": {
    "stdout": "[0, 1]\n[1, 2]\n[0, 1]",
    "stderr": "",
    "executionTimeMs": 42,
    "status": "Accepted",
    "testResults": [
      {
        "input": "nums=[2,7,11,15], target=9",
        "expectedOutput": "[0,1]",
        "actualOutput": "[0,1]",
        "passed": true
      }
    ]
  }
}
```

**`status` values we handle:** `Accepted`, `WrongAnswer`, `RuntimeError`, `TimeLimitExceeded`, `CompileError`, `MemoryLimitExceeded`

---

## 2. 🔴 Judge0 — Submit + Poll

**Endpoints:**
- `POST /api/submissions`
- `GET /api/submissions/:id`

**Backend actual status:**
Both endpoints exist in `SubmissionsController`. Submission creation enqueues work to a `Channel`-based background queue (`SubmissionEvaluationBackgroundService`), which calls Judge0 asynchronously. Returns `202` immediately. `GET /api/submissions/:id` is implemented and returns `SubmissionDetailResponse` which includes `testCaseResults`.

**The only blocker:** Judge0 must be running (same as #1).

**Request we send for POST:**
```json
POST /api/submissions
Authorization: Bearer <jwt>

{
  "problemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "code": "def twoSum(nums, target):\n    ...",
  "language": "Python"
}
```

**Response we expect — `202 Accepted` immediately:**
```json
{
  "data": {
    "submissionId": "uuid-string",
    "problemId": "uuid-string",
    "userId": "uuid-string",
    "code": "...",
    "language": "Python",
    "status": "Pending",
    "submittedAt": "2026-08-21T10:00:00Z",
    "executionTimeMs": null,
    "memoryUsedKb": null,
    "passedTestCases": 0,
    "totalTestCases": 32,
    "score": null,
    "result": null,
    "aiFeedback": [],
    "testCaseResults": []
  }
}
```

**Response we expect — GET /api/submissions/:id (final):**
```json
{
  "data": {
    "submissionId": "uuid-string",
    "problemId": "uuid-string",
    "userId": "uuid-string",
    "code": "...",
    "language": "Python",
    "status": "Accepted",
    "submittedAt": "2026-08-21T10:00:00Z",
    "executionTimeMs": 38,
    "memoryUsedKb": 14200,
    "passedTestCases": 32,
    "totalTestCases": 32,
    "score": 100,
    "result": {
      "passedTestCount": 32,
      "failedTestCount": 0,
      "totalTestCount": 32,
      "errorMessage": null,
      "outputSummary": "All test cases passed."
    },
    "aiFeedback": [],
    "testCaseResults": []
  }
}
```

**Critical — polling logic:**
- While `status === "Pending"` or `status === "Running"` → we keep polling
- Any other status → we stop polling and show the result
- `testCaseResults` array is optional — we render it if present, skip it if empty

**`status` values we handle:**
`Pending` | `Running` | `Accepted` | `WrongAnswer` | `RuntimeError` | `TimeLimitExceeded` | `CompileError` | `MemoryLimitExceeded`

---

## 3. 🔴 AI Hints

**Endpoint:** `POST /api/ai/hints` (also routed at `POST /api/hints`)

**Backend actual status:**
Fully implemented. `HintsController` → `IAiHintService` → `AiHintService` → Tutor Agent. The OpenAI key is configured in `appsettings.json` pointing to the ITI proxy (`http://apiaccess.iti.net.eg/api/v1`). Rate limited to 10 requests/hour/user. **This should be working now** — if hints are failing, check the ITI proxy connectivity and the OpenAI key validity.

**Request we send:**
```json
POST /api/ai/hints
Authorization: Bearer <jwt>

{
  "problemId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "studentCode": "def twoSum(nums, target):\n    pass",
  "hintLevel": 1,
  "previousHints": [],
  "attemptCount": 2,
  "lastSubmissionStatus": "WrongAnswer"
}
```

**Response shape we expect:**
```json
{
  "data": {
    "hintText": "Think about what data structure lets you look up a value in O(1)...",
    "hintLevel": 1,
    "followUpQuestion": "What would you store as the key and value in a hash map?",
    "hasMoreHints": true
  }
}
```

---

## 4. 🔴 Avatar URL in Login Response

**Endpoint:** `POST /api/auth/login`

**Backend actual status:**
✅ **Fully done.** The `LoginResponse.LoginUserInfo` DTO has `AvatarUrl: string?`. The `AuthService.LoginAsync()` reads `user.AvatarUrl` from the DB and puts it in the response. The `AvatarUrl` column (`nvarchar(500) NULL`) exists in the `Users` table via migration `20260424163742_AlignWithErDiagram`. No work remaining.

---

## 5. 🔴 PUT /api/auth/avatar

**Endpoint:** `PUT /api/auth/avatar`

**Backend actual status:**
✅ **Fully done.** The endpoint exists in `AuthController`, validates that the URL starts with `https://res.cloudinary.com/`, calls `UpdateAvatarUrlAsync` which persists to the DB. Returns `200 OK`. No work remaining.

**Request we send:**
```json
PUT /api/auth/avatar
Authorization: Bearer <jwt>

{
  "avatarUrl": "https://res.cloudinary.com/mg7dsqv2/image/upload/v1234567890/profile.jpg"
}
```

---

## 6. 🟡 Contests — All 5 Endpoints

**Backend actual status:**
✅ All 5 endpoints are implemented in `ContestsController` and wired to `IContestService`. Endpoints available:
- `GET /api/contests` — instructor's contests
- `GET /api/contests/my-contests` — student contest overview
- `GET /api/contests/:id` — single contest detail
- `POST /api/contests` — create contest (Instructor/Admin only)
- `GET /api/contests/:id/results` — leaderboard
- `POST /api/contests/:id/invitations/respond` — accept/decline invitation (bonus, already there)
- `GET /api/contests/students/search` — student candidate search for invite UI

**Frontend status:**
`ContestService` has HTTP calls written for all 5 endpoints with `catchError(() => of(mockData))` fallbacks. Once these endpoints return real data, the frontend picks it up automatically — no code changes needed.

---

### 6.1 GET /api/contests

Returns all contests created by the authenticated instructor.

**Request we send:**
```
GET /api/contests
Authorization: Bearer <jwt>
```

**Response shape we expect:**
```json
{
  "data": [
    {
      "id": "c1",
      "title": "Arrays & Hashing Sprint",
      "description": "Quick contest for hash map problems.",
      "createdByInstructorId": "instructor-uuid",
      "problemIds": ["uuid1", "uuid2"],
      "assignedStudentIds": ["s1", "s2"],
      "studentEmails": ["student1@example.com"],
      "startAt": "2026-09-01T09:00:00Z",
      "endAt": "2026-09-01T11:00:00Z",
      "status": 1,
      "participants": [
        {
          "studentId": "s1",
          "studentName": "Karim Ahmed",
          "studentEmail": "karim@example.com",
          "invitationStatus": 1,
          "respondedAt": "2026-08-20T10:00:00Z",
          "score": 0,
          "problemsSolved": 0,
          "accuracy": 0,
          "rank": 0
        }
      ]
    }
  ],
  "success": true
}
```

**`status` enum:**

| Value | Meaning |
|-------|---------|
| `0` or `"Draft"` | `draft` |
| `1` or `"Upcoming"` | `upcoming` |
| `2` or `"Live"` | `live` |
| `3` or `"Ended"` | `ended` |

**`invitationStatus` enum:**

| Value | Meaning |
|-------|---------|
| `0` or `"pending"` | pending |
| `1` or `"accepted"` | accepted |
| `2` or `"declined"` | declined |

---

### 6.2 GET /api/contests/:id

Returns a single contest by ID. Same shape as a single item from 6.1.

**`404` response:**
```json
{ "success": false, "message": "Contest not found." }
```

---

### 6.3 POST /api/contests

Creates a new contest.

**Request we send:**
```json
POST /api/contests
Authorization: Bearer <jwt>

{
  "title": "Arrays & Hashing Sprint",
  "description": "Quick contest for hash map problems.",
  "problemIds": ["uuid1", "uuid2", "uuid3"],
  "assignedStudentIds": ["s1", "s2", "s3"],
  "startAt": "2026-09-01T09:00:00Z",
  "endAt": "2026-09-01T11:00:00Z"
}
```

**Response `201`:** The created contest in the same shape as GET /api/contests/:id

---

### 6.4 GET /api/contests/:id/results

Returns all student results for a contest, sorted by rank ascending.

**Ranking rules (must match our UI sort):**
1. `score` descending
2. `problemsSolved` descending
3. `finishedAt` ascending (earlier finish wins tie)

---

### 6.5 GET /api/contests/my-contests

Returns the authenticated student's contest overview.

---

### 6.6 POST /api/contests/:id/invitations/respond

Already implemented. Called when a student accepts or declines a contest invitation.

**Request we send:**
```json
POST /api/contests/:id/invitations/respond
Authorization: Bearer <jwt>

{
  "accept": true
}
```

---

## 7. 🟡 AI Submission Feedback

**Endpoint:** `GET /api/submissions/:id/feedback`

**Backend actual status:**
⚠️ The endpoint exists in `SubmissionsController` and calls `GetFeedbackAsync`. However, the current `FeedbackResponse` DTO shape does **not match** what the frontend expects. The backend currently returns:

```json
[
  {
    "id": "uuid",
    "feedbackType": "quality",
    "message": "Good variable naming",
    "confidence": 0.95,
    "createdAt": "2026-08-21T..."
  }
]
```

**What the frontend expects** is a richer envelope with `overallScore`, `summary`, and `feedbackItems` with `title`, `description`, `lineStart`, `lineEnd`, and `severity`. The `FeedbackResponse` DTO needs to be updated to match this contract, or the endpoint needs to return a wrapper object.

**Needed response shape:**
```json
{
  "data": {
    "submissionId": "uuid-string",
    "overallScore": 72,
    "summary": "Your solution works but has room for efficiency improvements.",
    "feedbackItems": [
      {
        "id": "f1",
        "type": "quality",
        "title": "Good variable naming",
        "description": "Variable names are clear and follow conventions.",
        "lineStart": null,
        "lineEnd": null,
        "severity": "low"
      },
      {
        "id": "f2",
        "type": "optimization",
        "title": "Nested loop detected",
        "description": "The nested loop results in O(n²) time. Consider a hash map.",
        "lineStart": 3,
        "lineEnd": 6,
        "severity": "high"
      }
    ]
  }
}
```

**`type` enum:** `quality` | `optimization` | `anomaly`

**`severity` enum:** `low` | `medium` | `high`

**`overallScore`:** integer 0–100. Drives an animated score counter in the UI.

**`lineStart` / `lineEnd`:** line numbers in the student's code. Send `null` for general (not line-specific) feedback.

---

## 8. 🟡 PUT /api/auth/profile

**Endpoint:** `PUT /api/auth/profile`

**Backend actual status:**
✅ **Fully done.** The endpoint exists in `AuthController`, calls `UpdateProfileAsync`, which patches `FullName`, `Bio`, `Organization`, and optionally `AvatarUrl` on the `User` entity. Returns `200 OK`. No work remaining.

**Request we send:**
```json
PUT /api/auth/profile
Authorization: Bearer <jwt>

{
  "fullName": "Jane Doe",
  "headline": "Computer Science Student at Cairo University",
  "bio": "Passionate about algorithms and competitive programming.",
  "organization": "Cairo University",
  "social": {
    "linkedin": "https://linkedin.com/in/janedoe",
    "github": "https://github.com/janedoe",
    "twitter": "https://twitter.com/janedoe"
  }
}
```

**Note:** The current `UpdateProfileDto` accepts `fullName`, `bio`, `organization`, `avatarUrl`. The frontend also sends `headline` and `social` links — confirm whether these need to be added to the DTO and stored in new DB columns, or silently ignored.

---

## 9. 🟡 acceptedSubmissionsCount in Problem List

**Endpoint:** `GET /api/problems` (small fix needed)

**Backend actual status:**
❌ **One line missing in `ProblemService.MapToSummary()`.** The `AcceptedSubmissionsCount` field exists on the `Problem` entity (DB column present, incremented on accepted submissions), and it IS included in `ProblemDetailResponse`. But `ProblemSummaryResponse` has no such field, and `MapToSummary()` doesn't include it.

**Fix needed — two steps:**
1. Add `AcceptedSubmissionsCount` to `ProblemSummaryResponse.cs`
2. Add the mapping in `MapToSummary()` in `ProblemService.cs`:
```csharp
private static ProblemSummaryResponse MapToSummary(Problem p) => new()
{
    Id                       = p.Id,
    Title                    = p.Title,
    Difficulty               = p.Difficulty,
    Tags                     = p.ProblemTags.Select(pt => pt.ConceptTag.Name).ToList(),
    IsActive                 = p.IsActive,
    AcceptedSubmissionsCount = p.AcceptedSubmissionsCount   // ← add this
};
```

---

## 10. 🟡 Starter Code Per Problem

**Endpoint:** `GET /api/problems/:id` (fix needed)

**Backend actual status:**
⚠️ The `StarterCode` field exists on the `CodeTemplate` entity, and `ICodeWrapperService.GetStarterCode()` exists. However, `ProblemDetailResponse` has no `StarterCode` map/object, and `MapToDetail()` does not include it.

**What we need added to the detail response:**
```json
{
  "data": {
    "id": "uuid",
    "title": "...",
    "starterCode": {
      "python":     "def twoSum(nums: list[int], target: int) -> list[int]:\n    pass",
      "csharp":     "public int[] TwoSum(int[] nums, int target) {\n    \n}",
      "javascript": "var twoSum = function(nums, target) {\n    \n};",
      "java":       "public int[] twoSum(int[] nums, int target) {\n    \n}",
      "cpp":        "vector<int> twoSum(vector<int>& nums, int target) {\n    \n}"
    }
  }
}
```

**Language keys must be exactly:** `python`, `csharp`, `javascript`, `java`, `cpp` (all lowercase).

Once this field exists in the response, the frontend will remove `getHardcodedStarterCode()` and read `raw.starterCode` directly.

---

## 11. 🟢 POST /api/auth/forgot-password

**Endpoint:** `POST /api/auth/forgot-password` (new endpoint)

**Backend actual status:**
❌ Not built. No route, no service method, no `ForgotPassword` anywhere in the codebase.

**Our frontend status:**
The forgot-password form at `/auth/forgot-password` is complete with full UI. The submit handler is mocked — it shows a success message without calling the backend. We will wire it once the endpoint exists.

**Request shape we will send:**
```json
POST /api/auth/forgot-password

{
  "email": "user@example.com"
}
```

**Response `200`:**
```json
{ "message": "If this email is registered, a reset link has been sent." }
```

**Note:** Do not confirm or deny whether the email exists — always return `200` with a generic message (security best practice).

---

## 12. 🟢 GET /api/problems/recommended

**Endpoint:** `GET /api/problems/recommended` (new endpoint)

**Backend actual status:**
❌ Not built. No route registered.

**Our frontend status:**
The home page and student dashboard show "Recommended Problems" cards. Currently `ProblemService.getRecommended()` calls `getAll()` and slices the first 3.

**Request we will send:**
```
GET /api/problems/recommended
Authorization: Bearer <jwt>
```

**Response shape we expect:**
```json
{
  "data": [
    {
      "id": "uuid",
      "title": "Coin Change II",
      "difficulty": 1,
      "tags": ["Dynamic Programming"]
    }
  ]
}
```

3 problems max. Personalized to the student (based on weak topics, unsolved problems). We handle the mapping on our side.

---

## What Requires Zero Frontend Changes When Delivered

| Item | Why zero frontend work |
|------|------------------------|
| Start Judge0 (#1, #2) | HTTP calls already written, correct request body already sent |
| AI Hints (#3) | `HintService.getHint()` already POSTs to the correct endpoint — OpenAI key is configured |
| `avatarUrl` in login response (#4) | Already in the response — frontend already reads it |
| `PUT /api/auth/avatar` (#5) | Already working end-to-end |
| All contest endpoints (#6) | `ContestService` already calls all endpoints with fallback |
| `PUT /api/auth/profile` (#8) | Already working end-to-end |

The items that **will need a small backend change** (no frontend code change):
- AI feedback shape (#7) — update `FeedbackResponse` DTO to match the expected envelope
- `acceptedSubmissionsCount` in list (#9) — add one field to `ProblemSummaryResponse` + one line in `MapToSummary()`
- Starter code per problem (#10) — add `StarterCode` dict to `ProblemDetailResponse` + populate in `MapToDetail()`

The items that **will need a small frontend change** when delivered:
- Starter code per problem (#10) — requires removing `getHardcodedStarterCode()` (~5 lines)
- Forgot password (#11) — needs unmocking the form submit handler (~10 lines)
- Recommended problems (#12) — needs swapping `getAll().slice(0,3)` for the real endpoint call (~5 lines)

---

*Document authored by Frontend team — August 21, 2026*
*Backend status updated August 21, 2026 based on actual codebase review.*
*For questions on any contract detail, check `API_GUIDE.md` and `FRONTEND_INTEGRATION_GUIDE.md` in this folder.*
