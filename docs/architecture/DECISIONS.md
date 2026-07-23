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

## ADR-003: SQL Server for Primary Database

**Status:** Accepted  
**Date:** Sprint 0

**Context:** We needed a relational database for users, problems, submissions, and AI-related records.

**Decision:** SQL Server with Entity Framework Core.

**Reasons:**
- The current backend is already configured with `UseSqlServer` and SQL Server-compatible migrations.
- EF Core handles migrations cleanly and keeps the relational model expressive enough for the current schema.
- The team can deploy the same database locally and in Azure without adding another storage technology.

**Rejected:** PostgreSQL as the runtime database for the current implementation, MongoDB because the domain is relational.

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

**Status:** Accepted (implementation pending)  
**Date:** Sprint 0

**Context:** We need to run student-submitted code safely.

**Decision:** Docker containers as the execution sandbox. Each submission runs in a throwaway container with resource limits.

**Fallback:** Judge0 (open source online judge API) if Docker proves too complex to implement safely in time.

**Reasons:**
- Docker gives us real isolation — no risk of student code affecting the host machine.
- Resource limits (CPU, memory, network) are easy to configure per container.
- MVP supports Python and C# only, so we only need two base images.

**Risks:** The sandbox is not implemented yet in the current codebase; the active execution service is still a stub that returns sample-case placeholders.

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

**Status:** Accepted  
**Date:** Sprint 0

**Decision:** OpenAI is the active LLM provider for the tutor hint flow.

**Reasons:**
- The runtime already uses the OpenAI chat client through `OpenAiChatClient`.
- The prompt flow is built around structured JSON output and fallback handling.
- The configured model is environment-driven; the default in code is `gpt-4o`.

**Open Question:** Whether to add a second provider for fallback or cost control later.

---

## ADR-010: Vector Database

**Status:** Pending  
**Date:** —

**Context:** The proposal calls for RAG, but the current runtime does not yet include vector retrieval.

**Options:**
- Chroma
- Pinecone
- pgvector

**Current State:** The implementation stores only the tutor prompt template and OpenAI client plumbing. RAG remains a planned extension, not a shipped feature.
