# ARCHITECTURE.md
# Codify — System Architecture

---

## 1. Pattern: Modular Monolith

Codify's backend is a **modular monolith** — one deployable ASP.NET Core application with clearly separated internal modules. This was chosen over microservices because:

- The team has 4 months and limited framework experience
- Microservices introduce significant operational overhead
- Modules can be extracted later if the product grows

Each module owns its own domain logic and exposes only what other modules need.

---

## 2. High-Level Component Map

```
┌─────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                          │
│                                                             │
│   Angular SPA (Student UI)    Angular SPA (Instructor UI)   │
└──────────────────────┬──────────────────────────────────────┘
                       │ HTTP / REST
┌──────────────────────▼──────────────────────────────────────┐
│                      API GATEWAY LAYER                       │
│                  (ASP.NET Core Web API)                      │
│                                                             │
│  AuthController  ProblemController  SubmissionController    │
│  HintController  AnalyticsController  ExecutionController   │
└──────────────────────┬──────────────────────────────────────┘
                       │
┌──────────────────────▼──────────────────────────────────────┐
│                      DOMAIN / APPLICATION LAYER              │
│                                                             │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────┐    │
│  │ Auth Module │  │Problem Mod. │  │ Submission Mod.  │    │
│  └─────────────┘  └─────────────┘  └──────────────────┘    │
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────────┐    │
│  │  AI Module  │  │Analytics Mod│  │  Execution Mod.  │    │
│  └──────┬──────┘  └─────────────┘  └────────┬─────────┘    │
└─────────┼───────────────────────────────────┼──────────────┘
          │                                   │
┌─────────▼──────────────┐      ┌─────────────▼──────────────┐
│     AI SERVICE LAYER    │      │   CODE EXECUTION LAYER      │
│                        │      │                             │
│  TutorAgent            │      │  ExecutionService           │
│  CodeCheckerAgent      │      │  DockerRunner               │
│  AnalyticsAgent        │      │  TestCaseEvaluator          │
│  RAGPipeline           │      │                             │
│  VectorDBClient        │      └─────────────────────────────┘
│  LLMClient             │
└────────────────────────┘
          │
┌─────────▼──────────────┐
│  INFRASTRUCTURE LAYER   │
│                        │
│  PostgreSQL (EF Core)  │
│  Chroma Vector DB      │
│  LLM Provider API      │
│  Docker Socket         │
└────────────────────────┘
```

---

## 3. Backend Module Breakdown

### 3.1 Auth Module
**Owns:** User registration, login, JWT issuance, role validation
**Exposes:** `IAuthService` — used by all other modules to verify identity
**Tables:** `Users`

### 3.2 Problem Module
**Owns:** Problem CRUD, concept tag management, problem filtering
**Exposes:** `IProblemService` — used by Submission and AI modules
**Tables:** `Problems`, `ConceptTags`, `ProblemTags`, `TestCases`

### 3.3 Submission Module
**Owns:** Submission intake, result storage, attempt tracking
**Exposes:** `ISubmissionService` — used by Execution and AI modules
**Tables:** `Submissions`, `SubmissionResults`

### 3.4 AI Module
**Owns:** Agent orchestration, prompt templates, RAG pipeline, LLM client
**Exposes:** `ITutorAgent`, `ICodeCheckerAgent`, `IAnalyticsAgent`
**Tables:** `HintLogs`, `FeedbackRecords`
**External:** LLM API, Vector DB

### 3.5 Analytics Module
**Owns:** Performance profile updates, weakness detection, recommendations
**Exposes:** `IAnalyticsService` — used by Instructor dashboard endpoints
**Tables:** `PerformanceProfiles`

### 3.6 Execution Module
**Owns:** Docker container lifecycle, test case running, output comparison
**Exposes:** `IExecutionService` — called by Submission module
**External:** Docker daemon

---

## 4. Data Flow — Student Submits Code

```
1. Student clicks "Submit" in Angular UI
2. POST /api/submissions → SubmissionController
3. SubmissionService saves submission to DB (status: Pending)
4. ExecutionService picks up the submission
5. Docker container spins up with student code
6. Test cases run, output captured
7. Results saved to SubmissionResults table
8. CodeCheckerAgent analyzes code asynchronously
9. FeedbackRecord saved
10. AnalyticsAgent updates PerformanceProfile
11. GET /api/submissions/{id} returns full result to frontend
```

---

## 5. Data Flow — Student Requests a Hint

```
1. Student clicks "Get Hint" in Angular UI
2. POST /api/ai/hints → HintController
3. Backend assembles context:
   - Problem statement
   - Current hint level for this student+problem
   - Previous attempt summary
4. TutorAgent is called with this context
5. RAG Pipeline queries Vector DB for relevant concept explanations
6. LLM generates a constrained hint
7. HintLog saved to DB
8. Hint returned to frontend, hint level incremented
```

---

## 6. Frontend Structure

```
src/
├── app/
│   ├── auth/               # Login, Register pages
│   ├── student/
│   │   ├── problem-list/   # Browse problems
│   │   ├── problem-detail/ # Problem view + code editor + hint panel
│   │   ├── submission/     # Submission result view
│   │   └── progress/       # Student's own analytics
│   ├── instructor/
│   │   ├── dashboard/      # Overview stats
│   │   ├── students/       # Per-student drill-down
│   │   └── topics/         # Topic-level analytics
│   ├── shared/
│   │   ├── components/     # Reusable UI components
│   │   ├── services/       # HTTP service wrappers
│   │   └── models/         # TypeScript interfaces matching API response shapes
│   └── core/
│       ├── guards/         # Auth route guards
│       └── interceptors/   # JWT injection, error handling
```

---

## 7. Backend Folder Structure

```
src/
├── Codify.API/             # Controllers, middleware, startup
├── Codify.Application/     # Use cases, service interfaces, DTOs
├── Codify.Domain/          # Entities, domain rules, enums
├── Codify.Infrastructure/  # EF Core, LLM clients, Docker client, Vector DB
└── Codify.ExecutionEngine/ # Sandbox logic, Docker runner, evaluator

tests/
├── Codify.UnitTests/
└── Codify.IntegrationTests/
```

---

## 8. Security Considerations

- JWT tokens signed with a secret key, expiry set to 1 hour
- Role claims in token: `Student` or `Instructor`
- Execution sandbox: no network access, 5-second timeout, 128MB memory cap
- AI endpoints: rate-limited to prevent abuse
- All user code executed in a throwaway Docker container, never on the host

---

## 9. Deployment (MVP)

```
Frontend  → Static hosting (Vercel, Netlify, or Azure Static Web Apps)
Backend   → Single Docker container (Railway, Render, or Azure App Service)
Database  → PostgreSQL (managed — Supabase, Neon, or Railway Postgres)
Vector DB → Chroma running as a sidecar container
Execution → Docker-in-Docker or sibling container approach
```

All environments: `development`, `staging`, `demo`.
