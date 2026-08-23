# Codify

An AI-powered programming education platform for CS students and bootcamp learners. Students solve algorithmic problems and receive progressive AI hints, code quality feedback, and a personal performance profile that tracks their weak and strong topics over time. Instructors get a cohort dashboard with per-student analytics and integrity signals.

---

## Table of Contents

- [What It Does](#what-it-does)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Design Patterns](#design-patterns)
- [Project Structure](#project-structure)
- [Domain Model](#domain-model)
- [Database Schema](#database-schema)
- [API Reference](#api-reference)
- [AI Agents](#ai-agents)
- [Frontend Pages](#frontend-pages)
- [Rate Limiting](#rate-limiting)
- [Error Handling](#error-handling)
- [Seeded Data](#seeded-data)
- [Local Setup](#local-setup)
- [Team Ownership](#team-ownership)

---

## What It Does

| Problem with existing platforms | How Codify addresses it |
|---|---|
| Students jump straight to solutions | Progressive 3-level hint system — each level nudges, never reveals |
| No personalised weakness tracking | Performance profile rebuilt after every submission, updated on every hint |
| AI-generated submissions go undetectable | Code Checker Agent flags suspicious patterns as `IntegrityFlag` feedback |

**In scope for the MVP:**
- User registration and login with role-based access (Student / Instructor)
- Browse, filter, and view programming problems
- Submit Python or C# code → run against hidden test cases → pass/fail result
- Request AI-generated step-by-step hints (up to 3 per problem)
- Receive AI code quality and optimisation feedback after submission
- Student progress page (weak/strong topics, submission history, hint usage)
- Instructor dashboard (cohort overview, per-student drill-down)

**Out of scope for the MVP:** contests, mobile app, social features, custom LLM training, real-time collaboration.

---

## Technology Stack

| Layer | Choice | Notes |
|---|---|---|
| Backend framework | ASP.NET Core 10 Web API (C#) | Modular monolith |
| ORM | Entity Framework Core 10 | Code-first migrations |
| Database | SQL Server | Global query filters for soft deletes |
| Auth | JWT Bearer tokens | BCrypt password hashing |
| Code execution | Judge0 API | Sandboxed Python and C# evaluation |
| LLM | OpenAI (GPT-4o) | Structured JSON output, fallback on failure |
| Frontend | Angular + Tailwind CSS | SPA, TypeScript |
| API documentation | Swagger / OpenAPI | Available at `/swagger` in non-production |

---

## Architecture

Codify is a **modular monolith**: one deployable ASP.NET Core binary with clear internal layer separation. The Frontend is a separate Angular SPA.

```
┌─────────────────────────────────────────────────┐
│              Angular SPA (port 4200)            │
│   Auth · Problems · Editor · Hints · Dashboard  │
└────────────────────┬────────────────────────────┘
                     │ HTTP / REST / JSON
                     ▼
┌─────────────────────────────────────────────────┐
│          ASP.NET Core Web API (port 5001)        │
│                                                 │
│  ┌──────────────────────────────────────────┐   │
│  │  Presentation Layer (Codify.API)         │   │
│  │  Controllers · Middleware · Program.cs   │   │
│  └──────────────────┬───────────────────────┘   │
│                     │                           │
│  ┌──────────────────▼───────────────────────┐   │
│  │  Application Layer (Codify.Application)  │   │
│  │  Services · DTOs · Interfaces · Agents   │   │
│  └──────────────────┬───────────────────────┘   │
│                     │                           │
│  ┌──────────────────▼───────────────────────┐   │
│  │  Domain Layer (Codify.Domain)            │   │
│  │  Entities · Enums · Exceptions           │   │
│  └──────────────────┬───────────────────────┘   │
│                     │                           │
│  ┌──────────────────▼───────────────────────┐   │
│  │  Infrastructure (Codify.Infrastructure)  │   │
│  │  EF Core · Repositories · JwtService     │   │
│  │  OpenAiChatClient · TutorAgent           │   │
│  │  Judge0Client · BackgroundJobs           │   │
│  └──────────┬──────────────────┬────────────┘   │
└─────────────┼──────────────────┼────────────────┘
              │                  │
              ▼                  ▼
        SQL Server           OpenAI API
                          Judge0 (code execution)
```

### Request flow — submission

```
POST /api/submissions
    → SubmissionsController
    → SubmissionService.CreateAsync
        → validate problem exists, load test cases
        → Submission.Create (Pending)
        → ExecutionService.EvaluateAsync per test case (Judge0)
        → Submission.MarkAsAccepted / MarkAsFailed
        → SubmissionResult.Create
        → Problem.IncrementSubmissionCounters
        → User.IncrementSolvedProblems (first accepted only)
        → PerformanceService.UpdateAfterSubmissionAsync
    ← SubmissionDetailResponse
```

### Request flow — AI hint

```
POST /api/hints
    → HintsController
    → AiHintService.GetHintAsync
        → validate problem exists
        → HintRepository.GetCurrentHintLevelAsync → enforce max 3
        → TutorAgent.GenerateHintAsync
            → PromptLoader.LoadAsync (tutor-agent-system.txt)
            → PromptTemplate.Render (injects problem context)
            → OpenAiChatClient.CompleteAsync → OpenAI API
            → JSON parse → fallback if invalid
        → HintLog.Create → persist
        → PerformanceService.IncrementHintCountAsync
    ← HintResponse
```

---

## Design Patterns

### Repository Pattern
Every aggregate root has a dedicated repository interface in `Codify.Application/Interfaces` and an EF Core implementation in `Codify.Infrastructure/Repositories`. Controllers never touch `DbContext` directly.

```
IUserRepository → UserRepository(CodifyDbContext)
IProblemRepository → ProblemRepository(CodifyDbContext)
ISubmissionRepository → SubmissionRepository(CodifyDbContext)
IHintRepository → HintRepository(CodifyDbContext)
IPerformanceRepository → PerformanceRepository(CodifyDbContext)
IConceptTagRepository → ConceptTagRepository(CodifyDbContext)
IFeedbackRepository → FeedbackRepository(CodifyDbContext)
```

### Service Layer
All business logic lives in services. Controllers are thin — they extract the user identity, call one service method, and return the result through `ApiResponse.Ok()`.

```csharp
// Controller — no logic, no EF
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateSubmissionRequest request)
{
    var userId = User.GetUserId();
    var result = await submissionService.CreateAsync(request, userId);
    return StatusCode(202, ApiResponse.Ok(result));
}
```

### Factory Method on Entities
Domain entities use private constructors and static `Create` factory methods to guarantee invariants:

```csharp
var submission = Submission.Create(request.ProblemId, userId, request.Code, request.Language);
```

### Soft Deletes via Global Query Filters
`Users`, `Problems`, `ConceptTags`, `TestCases`, and `Submissions` all have an `IsDeleted` flag. EF Core global query filters automatically exclude soft-deleted records from all queries without any callsite changes.

### Standard Response Envelope
Every API response is wrapped:

```json
{ "success": true, "data": { ... } }
{ "success": false, "errorCode": "NOT_FOUND", "message": "Problem X not found." }
```

### Fallback Pattern on AI Calls
Every LLM call is wrapped in try/catch. If the API call fails or returns unparseable JSON, the agent returns a pre-defined safe fallback response rather than propagating an error to the student.

### Prompt Template System
Prompt text is stored in `.txt` files under `Codify.Infrastructure/AI/Prompts/`. `PromptLoader` reads the file at call time (relative to `AppContext.BaseDirectory`). `PromptTemplate.Render` replaces `{{key}}` placeholders with runtime values so prompt logic stays out of C# code.

### Background Job Queue (Submission Evaluation)
Submission evaluation is dispatched to a `Channel<T>`-based background queue (`SubmissionEvaluationQueue`). `SubmissionEvaluationBackgroundService` (a hosted service) drains the queue and calls the Judge0 evaluation pipeline asynchronously, keeping the HTTP response fast.

### Split Query Loading
The `UserRepository.GetWithAnalyticsDataAsync` and `GetInstructorWithProblemsAndSubmissionsAsync` methods use `AsSplitQuery()` to avoid the cartesian product that would result from EF Core joining multiple `Include` collections in a single SQL query.

---

## Project Structure

```
codify/
├── .github/
│   └── ISSUE_TEMPLATE.md
├── backend/
│   └── src/
│       ├── Codify.API/                         # Presentation layer
│       │   ├── Controllers/
│       │   │   ├── AuthController.cs           # /api/auth
│       │   │   ├── ProblemsController.cs       # /api/problems
│       │   │   ├── SubmissionsController.cs    # /api/submissions
│       │   │   ├── ExecutionController.cs      # /api/execution
│       │   │   ├── HintsController.cs          # /api/hints  ← rate limited
│       │   │   ├── AnalyticsController.cs      # /api/analytics  ← rate limited
│       │   │   ├── TagsController.cs           # /api/tags
│       │   │   └── AiController.cs             # placeholder for future AI routes
│       │   ├── Common/
│       │   │   └── ApiResponse.cs              # universal response envelope
│       │   ├── Extensions/
│       │   │   └── ClaimsPrincipalExtensions.cs  # User.GetUserId() helper
│       │   ├── Middleware/
│       │   │   └── ExceptionMiddleware.cs      # global exception → ApiResponse.Fail
│       │   ├── Properties/launchSettings.json
│       │   ├── appsettings.json
│       │   ├── appsettings.Development.json    # gitignored — local secrets
│       │   └── Program.cs                      # composition root
│       │
│       ├── Codify.Application/                 # Use cases — no EF, no HTTP
│       │   ├── Agents/
│       │   │   ├── ITutorAgent.cs
│       │   │   └── TutorAgentInput.cs
│       │   ├── DTOs/
│       │   │   ├── Auth/                       # LoginRequest/Response, RegisterRequest/Response, UserProfileResponse
│       │   │   ├── Problems/                   # CreateProblemRequest, ProblemDetailResponse, ProblemFilterRequest, ProblemSummaryResponse, UpdateProblemRequest
│       │   │   ├── Submissions/                # CreateSubmissionRequest, SubmissionDetailResponse, SubmissionSummaryResponse
│       │   │   ├── AI/                         # HintRequest, HintResponse, HintHistoryResponse
│       │   │   ├── Analytics/                  # StudentAnalyticsResponse, InstructorAnalyticsResponse
│       │   │   ├── Execution/                  # RunCodeRequest, RunCodeResponse, QuickRunRequest
│       │   │   ├── Tags/                       # ConceptTagResponse, CreateConceptTagRequest, UpdateConceptTagRequest
│       │   │   ├── Feedback/                   # FeedbackDetail
│       │   │   ├── TestCases/                  # TestCase DTOs
│       │   │   └── PagedResult.cs
│       │   ├── Interfaces/
│       │   │   ├── IAuthService.cs
│       │   │   ├── IProblemService.cs
│       │   │   ├── ISubmissionService.cs
│       │   │   ├── IExecutionService.cs
│       │   │   ├── IConceptTagService.cs
│       │   │   ├── IAiHintService.cs
│       │   │   ├── IAnalyticsService.cs
│       │   │   ├── IPerformanceService.cs
│       │   │   ├── IJwtService.cs
│       │   │   ├── ILLMClient.cs
│       │   │   ├── IUserRepository.cs
│       │   │   ├── IProblemRepository.cs
│       │   │   ├── ISubmissionRepository.cs
│       │   │   ├── IHintRepository.cs
│       │   │   ├── IPerformanceRepository.cs
│       │   │   ├── IConceptTagRepository.cs
│       │   │   ├── IFeedbackRepository.cs
│       │   │   └── ITestCaseRepository.cs
│       │   └── Services/
│       │       ├── AuthService.cs
│       │       ├── ProblemService.cs
│       │       ├── SubmissionService.cs
│       │       ├── ExecutionService.cs
│       │       ├── ConceptTagService.cs
│       │       ├── AiHintService.cs
│       │       ├── AnalyticsService.cs
│       │       └── PerformanceService.cs
│       │
│       ├── Codify.Domain/                      # Pure C# — no dependencies
│       │   ├── Entities/
│       │   │   ├── User.cs
│       │   │   ├── Problem.cs
│       │   │   ├── ConceptTag.cs
│       │   │   ├── ProblemTag.cs
│       │   │   ├── TestCase.cs
│       │   │   ├── Submission.cs
│       │   │   ├── SubmissionResult.cs
│       │   │   ├── HintLog.cs
│       │   │   ├── PerformanceProfile.cs
│       │   │   └── FeedbackRecord.cs
│       │   ├── Enums/
│       │   │   ├── UserRole.cs                 # Student | Instructor
│       │   │   ├── Difficulty.cs               # Easy | Medium | Hard
│       │   │   ├── SubmissionLanguage.cs       # Python | CSharp
│       │   │   ├── SubmissionStatus.cs         # Pending | Running | Accepted | WrongAnswer | RuntimeError | TimeLimitExceeded | CompileError | MemoryLimitExceeded
│       │   │   ├── FeedbackType.cs             # CodeQuality | Optimization | IntegrityFlag
│       │   │   └── TestCaseVisibility.cs       # Public | Hidden
│       │   └── Exceptions/
│       │       ├── NotFoundException.cs
│       │       ├── ForbiddenException.cs
│       │       └── ValidationException.cs
│       │
│       ├── Codify.Infrastructure/              # EF Core, external services
│       │   ├── AI/
│       │   │   ├── Prompts/
│       │   │   │   └── tutor-agent-system.txt  # Prompt template for Tutor Agent
│       │   │   ├── IPromptLoader.cs
│       │   │   ├── PromptLoader.cs
│       │   │   ├── PromptTemplate.cs
│       │   │   ├── TutorAgent.cs
│       │   │   ├── OpenAiChatClient.cs
│       │   │   └── OpenAiOptions.cs
│       │   ├── Auth/
│       │   │   └── JwtService.cs
│       │   ├── BackgroundJobs/
│       │   │   ├── ISubmissionEvaluationQueue.cs
│       │   │   ├── SubmissionEvaluationQueue.cs
│       │   │   └── SubmissionEvaluationBackgroundService.cs
│       │   ├── Judge0/
│       │   │   ├── Judge0Client.cs
│       │   │   ├── Judge0Options.cs
│       │   │   └── Judge0LanguageMap.cs
│       │   ├── Persistence/
│       │   │   ├── CodifyDbContext.cs
│       │   │   ├── Configurations/             # IEntityTypeConfiguration per entity
│       │   │   ├── Migrations/                 # EF Core migration history
│       │   │   └── Seed/
│       │   │       ├── ConceptTagSeed.cs       # 12 concept tags
│       │   │       └── ProblemSeed.cs          # 5 seeded problems with test cases
│       │   ├── Repositories/
│       │   │   ├── UserRepository.cs
│       │   │   ├── ProblemRepository.cs
│       │   │   ├── SubmissionRepository.cs
│       │   │   ├── HintRepository.cs
│       │   │   ├── PerformanceRepository.cs
│       │   │   ├── ConceptTagRepository.cs
│       │   │   ├── FeedbackRepository.cs
│       │   │   └── TestCaseRepository.cs
│       │   └── DependencyInjection.cs          # AddInfrastructure() extension
│       │
│       └── Codify.ExecutionEngine/             # Reserved — placeholder for future Docker sandbox
│
├── frontend/
│   └── src/
│       ├── app/
│       │   ├── auth/                           # Login, Register pages
│       │   ├── student/
│       │   │   ├── problem-list/               # Browse + filter problems
│       │   │   ├── problem-detail/             # Editor + hints + submit
│       │   │   ├── submission-result/          # Pass/fail breakdown + AI feedback
│       │   │   └── progress/                   # Personal analytics dashboard
│       │   ├── instructor/
│       │   │   ├── dashboard/                  # Cohort overview metrics
│       │   │   ├── students/                   # Student list + drill-down
│       │   │   └── topics/                     # Topic analytics
│       │   ├── shared/
│       │   │   ├── components/                 # LoadingSpinner, ErrorMessage, Badge
│       │   │   ├── services/                   # HTTP service wrappers
│       │   │   └── models/                     # TypeScript interfaces
│       │   └── core/
│       │       ├── guards/                     # AuthGuard, RoleGuard
│       │       └── interceptors/               # JwtInterceptor, ErrorInterceptor
│       ├── environments/
│       │   ├── environment.ts                  # Production
│       │   └── environment.development.ts      # Local dev
│       └── assets/
├── docs/
│   ├── api/API_SPEC.md
│   ├── architecture/ARCHITECTURE.md
│   ├── architecture/DECISIONS.md
│   ├── database/DATA_MODEL.md
│   ├── database/ER-Diagram.md
│   └── sprints/ROADMAP.md
├── .gitignore
├── CONVENTIONS.md
├── ENV_SETUP.md
├── PROJECT_CONTEXT.md
└── README.md
```

---

## Domain Model

### Entities and their responsibilities

**User**
Represents both students and instructors. Tracks `SolvedProblems` count, `Rating`, optional profile fields (`Username`, `Bio`, `AvatarUrl`), and soft-delete. Business methods: `RecordLogin()`, `IncrementSolvedProblems()`, `UpdateProfile()`, `SoftDelete()`.

**Problem**
A programming challenge with a markdown `Statement`, `Constraints`, execution limits (`TimeLimitMs`, `MemoryLimitMb`), and submission counters. Auto-generates a URL `Slug` from the title. Business methods: `Update()`, `IncrementSubmissionCounters(bool accepted)`, `Deactivate()`, `SoftDelete()`.

**ConceptTag**
A topic label (e.g. "Dynamic Programming", "Graphs"). Has a `Slug`. Many-to-many relationship with Problems through `ProblemTag`.

**ProblemTag**
Join entity linking a `Problem` to a `ConceptTag`. Composite PK `(ProblemId, ConceptTagId)`.

**TestCase**
An input/output pair for a problem. `IsSample = true` means it's shown to students. `VisibilityMode` is either `Public` or `Hidden`. Ordered by `OrderIndex`.

**Submission**
Student code submission. State machine: `Pending → Running → Accepted | WrongAnswer | RuntimeError | TimeLimitExceeded | CompileError | MemoryLimitExceeded`. Tracks `PassedTestCases`, `TotalTestCases`, `Score` (percentage), and execution metrics. Business methods: `MarkAsRunning()`, `MarkAsAccepted()`, `MarkAsFailed()`.

**SubmissionResult**
One-to-one with Submission. Holds the aggregate pass/fail counts and the first failing output summary for display.

**HintLog**
Persists each AI hint a student received. Records `HintLevel` (1–3), the `ResponseText` from the AI, and optionally the student's code (`RequestText`) at the time of request.

**PerformanceProfile**
One-to-one with User. Holds `WeakTopicsJson`, `StrongTopicsJson` (JSON arrays of tag names), `SuccessRate`, `AverageAttempts`, and `TotalHintsUsed`. Rebuilt in full after every submission (`Update()`), incremented cheaply after every hint (`IncrementHintCount()`).

**FeedbackRecord**
AI-generated feedback attached to a submission. Type is one of `CodeQuality`, `Optimization`, or `IntegrityFlag`.

---

## Database Schema

All entities use `Guid` PKs generated in application code. All string enums are stored as `nvarchar` (not integers). Soft-delete is implemented with an `IsDeleted` bit column and an EF Core global query filter on each affected entity.

```
Users
  Id (PK) · FullName · Email (unique) · PasswordHash · Role
  Username? · Bio? · AvatarUrl? · Rating · SolvedProblems
  CreatedAt · LastLoginAt? · UpdatedAt · IsDeleted

Problems
  Id (PK) · AuthorId (FK → Users, SET NULL) · Title · Slug (unique)
  Statement · Difficulty · Constraints · LanguageSupportJson
  TimeLimitMs · MemoryLimitMb · IsPublic · IsActive
  AcceptedSubmissionsCount · TotalSubmissionsCount
  CreatedAt · UpdatedAt · IsDeleted

ConceptTags
  Id (PK) · Name (unique) · Slug (unique) · Description
  CreatedAt · UpdatedAt · IsDeleted

ProblemTags
  ProblemId (PK, FK → Problems) · ConceptTagId (PK, FK → ConceptTags)

TestCases
  Id (PK) · ProblemId (FK) · InputData · ExpectedOutput
  IsSample · VisibilityMode · OrderIndex
  CreatedAt · UpdatedAt · IsDeleted

Submissions
  Id (PK) · ProblemId (FK) · UserId (FK) · Code · Language
  Status · SubmittedAt · ExecutionTimeMs? · MemoryUsedKb?
  PassedTestCases · TotalTestCases · Score?
  UpdatedAt · IsDeleted

SubmissionResults
  Id (PK) · SubmissionId (FK, unique) · PassedTestCount
  FailedTestCount · TotalTestCount · ErrorMessage? · OutputSummary?

HintLogs
  Id (PK) · UserId (FK) · ProblemId (FK) · HintLevel (1–3)
  RequestText? · ResponseText · CreatedAt

PerformanceProfiles
  UserId (PK, FK → Users) · WeakTopicsJson · StrongTopicsJson
  SuccessRate · AverageAttempts · TotalHintsUsed · LastUpdatedAt

FeedbackRecords
  Id (PK) · SubmissionId (FK) · FeedbackType · Message · CreatedAt
```

**Relationships**
- Users `1:1` PerformanceProfiles
- Users `1:N` Submissions, HintLogs, AuthoredProblems
- Problems `1:N` TestCases, Submissions, HintLogs
- Problems `N:M` ConceptTags via ProblemTags
- Submissions `1:1` SubmissionResults
- Submissions `1:N` FeedbackRecords

**EF Core Migrations**
```
20260401213428_InitialCreate
20260424163742_AlignWithErDiagram
20260811194109_AddTotalHintsUsedToPerformanceProfile
```

---

## API Reference

Base URL: `https://localhost:5001/api`

All responses use the envelope `{ "success": bool, "data": any, "errorCode"?: string, "message"?: string }`.

Protected endpoints require `Authorization: Bearer <token>`.

---

### Auth  `/api/auth`

| Method | Path | Auth | Role | Description |
|---|---|---|---|---|
| POST | `/auth/register` | No | — | Register a new user |
| POST | `/auth/login` | No | — | Login and receive JWT |
| POST | `/auth/logout` | Yes | Any | Stateless logout (client discards token) |
| GET | `/auth/me` | Yes | Any | Return current user profile |

**POST /auth/register** — `201 Created`
```json
// Request
{ "fullName": "Ahmed Hassan", "email": "ahmed@example.com", "password": "min8chars", "role": "Student" }
// Response data
{ "userId": "uuid", "email": "ahmed@example.com", "role": "Student" }
```

**POST /auth/login** — `200 OK`
```json
// Response data
{ "token": "eyJ...", "expiresAt": "2026-08-12T00:00:00Z", "user": { "userId": "uuid", "fullName": "Ahmed Hassan", "role": "Student" } }
```

---

### Problems  `/api/problems`

All routes require authentication.

| Method | Path | Auth | Role | Description |
|---|---|---|---|---|
| GET | `/problems` | Yes | Any | Paged list with filters |
| GET | `/problems/{id}` | Yes | Any | Full problem detail + sample cases |
| POST | `/problems` | Yes | Instructor | Create a problem with test cases |
| PUT | `/problems/{id}` | Yes | Instructor | Update a problem |
| DELETE | `/problems/{id}` | Yes | Instructor | Soft-delete a problem |
| GET | `/problems/{id}/submissions` | Yes | Any | Submissions for a problem (students see own only) |

**GET /problems** — query params: `difficulty`, `tag`, `search`, `page` (default 1), `pageSize` (default 20)
```json
// Response data
{ "items": [ { "id": "uuid", "title": "Two Sum", "difficulty": "Easy", "tags": ["Arrays & Hashing"], "isActive": true } ], "totalCount": 5, "page": 1, "pageSize": 20 }
```

**GET /problems/{id}**
```json
// Response data
{
  "id": "uuid", "title": "Two Sum", "slug": "two-sum",
  "statement": "Given an array of integers...",
  "difficulty": "Easy", "constraints": "2 ≤ nums.length ≤ 10⁴",
  "languageSupport": ["Python", "CSharp"],
  "tags": ["Arrays & Hashing"],
  "sampleTestCases": [ { "input": "[2,7,11,15]\n9", "expectedOutput": "[0,1]" } ],
  "timeLimitMs": 1000, "memoryLimitMb": 128,
  "acceptedSubmissionsCount": 0, "totalSubmissionsCount": 0
}
```

---

### Submissions  `/api/submissions`

| Method | Path | Auth | Role | Rate Limit |
|---|---|---|---|---|
| POST | `/submissions` | Yes | Student | 30/hour |
| GET | `/submissions/{id}` | Yes | Owner or Instructor | — |

**POST /submissions** — `202 Accepted`
```json
// Request
{ "problemId": "uuid", "code": "def two_sum(nums, target): ...", "language": "Python" }
// Response data
{ "submissionId": "uuid", "status": "Pending", "passedTestCases": 5, "totalTestCases": 5, "score": 100, "result": { "passedTestCount": 5, "failedTestCount": 0, ... }, "aiFeedback": [] }
```

**Submission statuses:** `Pending` → `Running` → `Accepted | WrongAnswer | RuntimeError | TimeLimitExceeded | CompileError | MemoryLimitExceeded`

---

### Execution  `/api/execution`

| Method | Path | Auth | Role | Rate Limit | Description |
|---|---|---|---|---|---|
| POST | `/execution/run` | Yes | Student | 60/hour | Run against sample test cases only ("Run" button) |
| POST | `/execution/quick-run` | No | — | — | Raw stdout/stderr (dev tool) |
| POST | `/execution/run-with-tests` | No | — | — | Run against custom test set (dev tool) |

---

### AI Hints  `/api/hints`

| Method | Path | Auth | Role | Rate Limit |
|---|---|---|---|---|
| POST | `/hints` | Yes | Student | 10/hour |
| GET | `/hints/history` | Yes | Student | — |

**POST /hints** — request a progressive AI hint. Server determines the next level (1, 2, or 3) based on persisted history. Max 3 hints per problem per user.
```json
// Request
{ "problemId": "uuid", "studentCode": "def solution(): ...", "hintLevel": 1, "attemptCount": 2, "lastSubmissionStatus": "WrongAnswer" }
// Response data
{ "hintText": "Think about what data structure gives O(1) lookups.", "hintLevel": 1, "followUpQuestion": "What would you store as key and value?", "hasMoreHints": true }
```

**GET /hints/history?problemId={uuid}** — returns all hints the student has received for a problem.
```json
// Response data
{ "problemId": "uuid", "totalHintsUsed": 2, "canRequestMore": true, "hints": [ { "hintLevel": 1, "hintText": "...", "createdAt": "..." } ] }
```

---

### Analytics  `/api/analytics`

| Method | Path | Auth | Role | Rate Limit | Description |
|---|---|---|---|---|---|
| GET | `/analytics/students/{id}` | Yes | Owner or Instructor | 60/hour | Full student performance breakdown |
| GET | `/analytics/overview` | Yes | Instructor | 60/hour | Instructor cohort dashboard |

**GET /analytics/students/{id}**
```json
// Response data
{
  "userId": "uuid", "fullName": "Ahmed Hassan", "email": "ahmed@example.com",
  "totalSolvedProblems": 3, "easySolved": 2, "mediumSolved": 1, "hardSolved": 0,
  "totalSubmissions": 10, "acceptedSubmissions": 5, "wrongAnswers": 3,
  "runtimeErrors": 1, "compileErrors": 1, "timeLimitExceeded": 0,
  "successRatePercent": 50.0, "averageExecutionTimeMs": 42.5, "averageAttemptsPerProblem": 3.3,
  "languageBreakdown": [ { "language": "Python", "submissions": 8 }, { "language": "CSharp", "submissions": 2 } ],
  "weakTopics": ["Graphs", "Dynamic Programming"],
  "strongTopics": ["Arrays & Hashing", "Binary Search"],
  "totalHintsUsed": 4,
  "lastSubmissionAt": "2026-08-11T20:00:00Z", "memberSince": "2026-04-01T00:00:00Z"
}
```

**GET /analytics/overview** (Instructor)
```json
// Response data
{
  "instructorId": "uuid", "fullName": "Dr. Smith",
  "totalProblemsAuthored": 5, "totalStudentsReached": 12,
  "totalSubmissionsReceived": 48, "overallAcceptRatePercent": 54.2,
  "students": [ { "studentId": "uuid", "fullName": "...", "totalSubmissions": 6, "acceptedSubmissions": 3, "successRatePercent": 50.0, "problemsSolved": 2, "lastActivityAt": "..." } ]
}
```

---

### Tags  `/api/tags`

| Method | Path | Auth | Role | Description |
|---|---|---|---|---|
| GET | `/tags` | No | — | All concept tags |
| GET | `/tags/{id}` | No | — | Single tag |
| POST | `/tags` | Yes | Instructor | Create tag |
| PUT | `/tags/{id}` | Yes | Instructor | Update tag |
| DELETE | `/tags/{id}` | Yes | Instructor | Soft-delete tag |
| GET | `/tags/problems/{problemId}` | No | — | Tags for a problem |
| POST | `/tags/problems/{problemId}/{tagId}` | Yes | Instructor | Add tag to problem |
| DELETE | `/tags/problems/{problemId}/{tagId}` | Yes | Instructor | Remove tag from problem |

---

## AI Agents

### Agent 1 — Tutor Agent (`TutorAgent`)

**Trigger:** Student clicks "Get Hint"
**File:** `Codify.Infrastructure/AI/TutorAgent.cs`
**Prompt:** `Codify.Infrastructure/AI/Prompts/tutor-agent-system.txt`

The agent receives the problem title, statement, concept tags, hint level (1–3), the student's current code, the student's previous hints, and their attempt count. It renders these into a prompt template and calls OpenAI. The response is expected as structured JSON matching `HintResponse`. If the call fails or the JSON is invalid, a generic safe fallback hint is returned.

**Key rules:**
- Never reveals the complete algorithm
- Each level gives progressively more specific guidance
- Hint level is server-enforced — the client cannot request level 3 without having received level 2

### Agent 2 — Code Checker Agent (`CodeCheckerAgent`)

**Trigger:** After a submission is evaluated
**Interface:** `ICodeCheckerAgent` in `Codify.Application`
**Feedback types:** `CodeQuality`, `Optimization`, `IntegrityFlag`

Analyses the submitted code for quality issues, potential optimisations, and suspicious patterns (AI-generated code signals). Persists results as `FeedbackRecord` rows tied to the submission.

### Agent 3 — Analytics Agent (Tagging + Performance)

Not implemented as a standalone agent. The tagging and performance update logic is baked directly into `PerformanceService`:

- **After every submission:** `UpdateAfterSubmissionAsync` recalculates weak/strong topics, success rate, and average attempts across the student's full submission history.
- **After every hint:** `IncrementHintCountAsync` does a lightweight upsert to keep `TotalHintsUsed` in sync without a full recalculation.

**Weak topic threshold:** < 40% acceptance rate on a tag
**Strong topic threshold:** > 75% acceptance rate on a tag

---

## Frontend Pages

### Student Pages

**Login / Register** (`/auth/login`, `/auth/register`)
Standard auth forms. On success, JWT is stored and the `JwtInterceptor` attaches it to all subsequent requests. `RoleGuard` directs students and instructors to their respective home routes.

**Problem List** (`/problems`)
Fetches `GET /api/problems` with filter controls for difficulty, concept tag, and search. Paginated. Each row shows title, difficulty badge, and tags.

**Problem Detail** (`/problems/:id`)
The core student experience. Contains:
- Markdown-rendered problem statement
- Constraints and sample test cases
- Monaco / CodeMirror code editor with language selector (Python / C#)
- "Run" button → calls `POST /api/execution/run` → shows sample test results inline
- "Submit" button → calls `POST /api/submissions` → shows full result panel
- Hint panel (collapsible) — shows hints in order, "Get Hint" button, loading state, disabled after level 3

**Submission Result** (`/submissions/:id`)
Pass/fail breakdown per test case, status badge (colour-coded), execution time and memory, expandable AI feedback section.

**Progress / Dashboard** (`/student/progress`)
Personal analytics: solved count split by difficulty, success rate, average attempts, weak and strong topics, total hints used, recent submission activity, and recommended topics to practise.

### Instructor Pages

**Instructor Dashboard** (`/instructor/dashboard`)
Calls `GET /api/analytics/overview`. Shows total problems authored, students reached, overall accept rate, and a sortable per-student summary table.

**Student Detail** (`/instructor/students/:id`)
Calls `GET /api/analytics/students/:id`. Full student performance breakdown — submission stats, language breakdown, weak/strong topic lists, hint usage.

**Topic Analytics** (`/instructor/topics`)
Per-concept-tag breakdown of student performance across the instructor's problem set.

---

## Rate Limiting

All policies use a **sliding window** algorithm partitioned by user ID (falls back to IP address for anonymous requests). Responses exceed the limit return `429 Too Many Requests`.

| Policy name | Applies to | Limit |
|---|---|---|
| `submissions` | POST /submissions | 30 requests / hour |
| `execution` | POST /execution/run | 60 requests / hour |
| `ai-hints` | POST /hints | 10 requests / hour |
| `analytics` | GET /analytics/* | 60 requests / hour |

`GET /hints/history` is intentionally not rate-limited — it is a read-only query with no AI or write cost.

---

## Error Handling

Exceptions are caught globally by `ExceptionMiddleware` and mapped to the standard response envelope.

| Exception type | HTTP status | Error code |
|---|---|---|
| `NotFoundException` | 404 Not Found | `NOT_FOUND` |
| `ForbiddenException` | 403 Forbidden | `FORBIDDEN` |
| `ValidationException` | 400 Bad Request | `VALIDATION_ERROR` |
| Any other exception | 500 Internal Server Error | `INTERNAL_ERROR` |

Model validation failures (e.g. missing required fields) return `400 Bad Request` automatically via ASP.NET Core's `[ApiController]` attribute before the request reaches the service layer.

---

## Seeded Data

The application seeds the following on startup if the tables are empty:

**12 Concept Tags**
Arrays & Hashing · Two Pointers · Sliding Window · Binary Search · Linked Lists · Trees · Graphs · Dynamic Programming · Greedy · Backtracking · Recursion · Sorting

**5 Problems** (target is 10 for the demo)

| # | Title | Difficulty | Tags |
|---|---|---|---|
| 1 | Two Sum | Easy | Arrays & Hashing |
| 2 | Valid Parentheses | Easy | Linked Lists |
| 3 | Binary Search | Easy | Binary Search |
| 4 | Maximum Subarray | Medium | Dynamic Programming, Greedy |
| 5 | Climbing Stairs | Easy | Dynamic Programming, Recursion |

Each problem has 2 public sample test cases and 3 hidden test cases.

---

## Local Setup

### Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0+ |
| Node.js | 18+ |
| Angular CLI | 17+ |
| Docker Desktop | Latest |
| SQL Server | Local or via Docker |

### 1 — Clone

```bash
git clone https://github.com/your-org/codify.git
cd codify
```

### 2 — Backend configuration

Create `backend/src/Codify.API/appsettings.Development.json` (this file is gitignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=codify_dev;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "Jwt": {
    "Secret": "your-secret-key-minimum-32-characters-long",
    "Issuer": "codify-api",
    "Audience": "codify-client",
    "ExpiryHours": 24
  },
  "OpenAI": {
    "ApiKey": "sk-...",
    "Model": "gpt-4o"
  },
  "Judge0": {
    "BaseUrl": "https://judge0-ce.p.rapidapi.com",
    "ApiKey": "your-rapidapi-key",
    "ApiHost": "judge0-ce.p.rapidapi.com"
  }
}
```

### 3 — Run the backend

```bash
cd backend/src/Codify.API
dotnet run
```

Migrations are applied automatically on startup. Seed data is inserted if the tables are empty.

- API: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### 4 — Run the frontend

```bash
cd frontend
npm install
ng serve
```

App: `http://localhost:4200`

### 5 — Trust the dev certificate (first time only)

```bash
dotnet dev-certs https --trust
```

---

## Team Ownership

| Member | Owns |
|---|---|
| Omar | AI agents, prompt engineering, RAG pipeline, vector DB integration |
| Khaled | Backend API structure, domain logic, database schema, EF Core, analytics |
| Badry | Execution engine, submission pipeline, Judge0 integration |
| Salah | Frontend: student-facing UI (problem list, editor, hint panel) |
| Owais | Frontend: instructor dashboard, analytics views, component library |
