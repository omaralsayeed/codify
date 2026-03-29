# DECISIONS.md
# Architecture Decision Records (ADRs)

> This file records every significant technology or design choice, why it was made, and what was rejected. Before changing any of these decisions, discuss with the full team.

---

## ADR-001: Modular Monolith over Microservices

**Status:** Accepted  
**Date:** Sprint 0

**Context:** We needed to choose a backend architecture pattern.

**Decision:** Modular monolith — one deployable ASP.NET Core application with internal module separation.

**Reasons:**
- Team has 4 months. Microservices require service discovery, inter-service networking, and distributed tracing — all out of scope for this timeline.
- A modular monolith allows clean boundaries (each module has its own services, repos, DTOs) without the operational burden.
- Modules can be extracted into microservices later if the product grows.

**Rejected:** Microservices. Too much infra overhead for a 5-person, 4-month project.

---

## ADR-002: ASP.NET Core for Backend

**Status:** Accepted  
**Date:** Sprint 0

**Context:** We needed to pick a backend framework. Team knows C#.

**Decision:** ASP.NET Core Web API with C#.

**Reasons:**
- Team is already learning C#. Using it here reduces the learning surface area.
- ASP.NET Core is mature, well-documented, and has excellent EF Core integration.
- Strong JWT auth support out of the box.

**Rejected:** Node.js/Express (team less familiar), Django (Python not primary skill), Spring Boot (Java — no existing knowledge).

---

## ADR-003: PostgreSQL for Primary Database

**Status:** Accepted  
**Date:** Sprint 0

**Context:** We needed a relational database for users, problems, submissions, and analytics data.

**Decision:** PostgreSQL with Entity Framework Core.

**Reasons:**
- PostgreSQL is open source, free to host on managed services, and more powerful than SQL Server for our use case.
- EF Core handles migrations cleanly and reduces raw SQL for routine operations.
- Strong support on hosting platforms (Supabase, Neon, Railway).

**Rejected:** SQL Server (licensing concerns for production), MongoDB (our data is relational — problems, submissions, test cases have clear relationships).

---

## ADR-004: JWT-Based Authentication

**Status:** Accepted  
**Date:** Sprint 0

**Context:** We need authentication for two roles: Student and Instructor.

**Decision:** JWT Bearer tokens, issued on login, verified on every protected request.

**Reasons:**
- Stateless — no server-side session store needed.
- Works well with Angular's HTTP interceptors for automatic token injection.
- Easy to embed role claims (`Student` / `Instructor`) in the token payload.

**Rejected:** Session cookies (requires session store, more complex CORS setup), OAuth (overkill for an MVP with no social login requirement).

---

## ADR-005: Docker Sandbox for Code Execution

**Status:** Accepted (implementation TBD)  
**Date:** Sprint 0

**Context:** We need to run student-submitted code safely.

**Decision:** Docker containers as the execution sandbox. Each submission runs in a throwaway container with resource limits.

**Fallback:** Judge0 (open source online judge API) if Docker proves too complex to implement safely in time.

**Reasons:**
- Docker gives us real isolation — no risk of student code affecting the host machine.
- Resource limits (CPU, memory, network) are easy to configure per container.
- MVP supports Python and C# only, so we only need two base images.

**Risks:** Docker-in-Docker can be tricky depending on hosting environment. This is the highest-risk component and should be validated by end of Sprint 1.

---

## ADR-006: RAG for AI Tutor Agent

**Status:** Accepted  
**Date:** Sprint 0

**Context:** The Tutor Agent needs to generate hints that are grounded in the correct concept context (e.g., explaining graph traversal concepts when the problem is a BFS problem).

**Decision:** Use RAG — store concept explanations and problem metadata in a vector database, retrieve relevant chunks before calling the LLM.

**Reasons:**
- Without RAG, the LLM may hallucinate concept explanations.
- RAG anchors the response to pre-approved educational content.
- Reduces token usage by only sending relevant context.

**Vector DB choice:** Chroma (recommended — easy to self-host, good Python SDK). Final decision pending.

---

## ADR-007: Angular for Frontend

**Status:** Accepted  
**Date:** Sprint 0

**Context:** We need a frontend framework. Team knows basic JS.

**Decision:** Angular with TypeScript + Tailwind CSS.

**Reasons:**
- Structured, opinionated framework — good for a team with limited frontend experience because it enforces consistent patterns.
- Strong CLI for generating components, services, and guards.
- TypeScript aligns with the rest of the stack.

**Rejected:** React (less structure, more decision fatigue for a new team), Vue (smaller ecosystem, less team familiarity).

---

## ADR-008: Supported Languages

**Status:** Accepted  
**Date:** Sprint 0

**Decision:** Python and C# only in the MVP.

**Reasons:**
- Docker images already available for both.
- Team can write and validate test cases in both languages.
- Every additional language adds Docker image management, execution configuration, and test case complexity.

**Future:** JavaScript, Java can be added in a v2 if time permits.

---

## ADR-009: LLM Provider

**Status:** ⚠️ Pending  
**Date:** —

**Options:**
- **OpenAI (GPT-4o):** Better instruction following, more predictable structured outputs, higher cost.
- **Gemini (1.5 Pro):** Google ecosystem, competitive on cost, good context window.

**Decision criteria:** Cost per 1M tokens, structured output reliability (JSON mode), rate limits on free/developer tier.

**Action:** Omar to run a test prompt through both with a sample hint request and code analysis task. Decision by end of Sprint 1.

---

## ADR-010: Vector Database

**Status:** ⚠️ Pending  
**Date:** —

**Options:**
- **Chroma:** Simple, self-hostable, good Python SDK. Best for MVP.
- **Pinecone:** Managed, fast, but paid at scale.
- **pgvector:** PostgreSQL extension — no extra service, but less feature-rich.

**Recommendation:** Chroma for MVP. If self-hosting proves complex, pgvector is the lowest-friction fallback.

**Action:** Omar to prototype a basic embed + retrieve flow with Chroma. Decision by Sprint 2.
