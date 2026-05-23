# PROJECT_CONTEXT.md
# Codify — Full Project Context

> **This file is the single source of truth for what Codify is, what it does, and what it does NOT do.**
> Every AI assistant, new team member, or tool should read this file first.

---

## 1. What Is Codify?

Codify is an **AI-powered programming education platform** built as a university capstone MVP. It targets CS students and bootcamp learners who want to practice algorithmic problem-solving with intelligent, progressive guidance — not just raw answers.

The system combines:
- A **code submission judge** (runs student code against test cases)
- A **multi-agent AI layer** (tutor hints, code analysis, performance analytics)
- An **instructor dashboard** (progress tracking, integrity signals)
- A **student dashboard**
---

## 2. The Problem We're Solving

Current platforms (LeetCode, HackerRank, etc.) have three key gaps:

| Problem | Impact |
|---------|--------|
| Students jump straight to the solution | No real learning happens |
| No personalized weakness tracking | Student doesn't know what to fix |
| AI-generated code submissions are undetectable | Integrity is compromised |

Codify addresses all three through its AI agent layer.

---

## 3. Who Uses It?

### Primary Users
- **Students** — solve problems, request hints, view their own progress
- **Instructors** — monitor cohort progress, review integrity signals

### Secondary
- Teaching assistants, bootcamp admins

---

## 4. What The System Does (In Scope)

- User registration and login (JWT, role-based)
- Browse, filter, and view programming problems
- Submit code → run against hidden test cases → get pass/fail result
- Request AI-generated step-by-step hints (Tutor Agent)
- Receive code quality and optimization feedback after submission (Code Checker Agent)
- Track performance history and weak concept areas (Analytics Agent)
- Instructor dashboard: per-student progress, topic trends, integrity flags

---

## 5. What The System Does NOT Do (Out of Scope for MVP)

These are **hard boundaries**. Do not add these without a team decision:

- ❌ Large-scale competitive judging (no Codeforces-style contests)
- ❌ Support for many languages — MVP supports **Python and C#** only
- ❌ Mobile application (web only)
- ❌ Training a custom AI model from scratch (we use existing LLM providers)
- ❌ Full recruitment marketplace or job matching
- ❌ Real-time collaboration or pair programming
- ❌ Social features (leaderboards, forums, comments)

---

## 6. The Three AI Agents

This is the core of Codify's differentiation. There are exactly **three agents**, each with a distinct role:

### Agent 1 — AI Tutor Agent
- **Trigger:** Student clicks "Get Hint" on a problem
- **Does:** Returns a progressive hint (not the full solution)
- **Uses:** RAG (retrieves concept context from vector DB)
- **Key rule:** Never reveal the full algorithm directly

### Agent 2 — AI Code Checker Agent
- **Trigger:** Student submits code
- **Does:** Analyzes code quality, logic, optimization, and suspicious patterns
- **Uses:** Direct LLM analysis (no RAG needed)
- **Key rule:** Be constructive, structured output only

### Agent 3 — Analytics / Tagging Agent
- **Trigger:** Background — runs after each submission
- **Does:** Tags problems by concept, updates student weakness profile, generates recommendations
- **Uses:** RAG + student history
- **Key rule:** Recommendations must be explainable

---

## 7. Architecture in One Sentence

> A **modular monolith** ASP.NET Core backend with a dedicated AI service layer, an Angular frontend, a PostgreSQL database, and a Docker-based code execution sandbox.

See [ARCHITECTURE.md](./docs/architecture/ARCHITECTURE.md) for the full diagram.

---

## 8. Technology Choices (Finalized)

| Component | Choice | Status |
|-----------|--------|--------|
| Backend framework | ASP.NET Core Web API | ✅ Final |
| Architecture pattern | Modular Monolith | ✅ Final |
| Database | PostgreSQL + EF Core | ✅ Final |
| Auth | JWT (Bearer tokens) | ✅ Final |
| Frontend | Angular + Tailwind CSS | ✅ Final |
| Code execution | Docker sandbox | ✅ Final |
| Languages supported | Python + C# | ✅ Final |
| LLM provider | OpenAI or Gemini | ⚠️ Pending decision |
| Vector DB | Chroma (recommended) | ⚠️ Pending decision |

---

## 9. Team Responsibilities

| Person | Primary Ownership |
|--------|------------------|
| Omar | AI agents, prompt engineering, RAG pipeline, vector DB integration |
| Khaled | Backend API structure, domain logic, database schema, EF Core |
| Badry | Execution engine integration, submission pipeline, API-to-service wiring |
| Salah | Frontend: student-facing UI (problem list, code editor, hint panel) |
| Owais | Frontend: instructor dashboard, analytics views, component library |

All team members share responsibility for testing and reviewing each other's work.

---

## 10. Key Risks to Keep in Mind

| Risk | Mitigation |
|------|-----------|
| Docker sandbox is complex to build safely | De-risk in Sprint 1; use Judge0 as fallback |
| AI outputs are non-deterministic | Use structured JSON outputs + validation layer |
| Over-scoping under pressure | Re-read Section 5 of this file before adding anything new |
| Team unfamiliarity with frameworks | Keep architecture simple; pair on hard parts |

---

## 11. Data We Store

- User accounts and roles
- Problem statements, constraints, test cases
- Concept tags (DP, Graphs, Recursion, Greedy, etc.)
- Student submissions and execution results
- Hint request logs (per user per problem)
- AI feedback records
- Student performance profiles

We do NOT store raw code from external sources. All problems are either synthetic or sourced from public datasets.

---

## 12. Glossary

| Term | Meaning |
|------|---------|
| Agent | An AI component with a defined role, input schema, and output schema |
| RAG | Retrieval-Augmented Generation — fetching relevant context before calling the LLM |
| Modular Monolith | Single deployable backend with internally separated modules (not microservices) |
| Sandbox | Isolated Docker container that runs student code safely |
| Hint Level | A number (1–3) representing how much guidance has been given so far |
| Performance Profile | Per-student record of strong/weak topics and success rates |
| Execution Engine | The service that runs code against test cases and returns results |
