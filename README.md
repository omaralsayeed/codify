# Codify — AI-Powered Programming Education Platform

> **Capstone Project · 12-Week MVP · Team of 5**

Codify is a web-based learning platform that helps students solve algorithmic problems through guided AI hints, code submission analysis, performance tracking, and instructor-facing insights. It demonstrates a practical multi-agent AI architecture.

---

## Quick Navigation

| Document | Purpose |
|----------|---------|
| [PROJECT_CONTEXT.md](./PROJECT_CONTEXT.md) | What we're building and why — read this first |
| [ARCHITECTURE.md](./docs/architecture/ARCHITECTURE.md) | System design, layers, component diagram |
| [DATA_MODEL.md](./docs/database/DATA_MODEL.md) | All entities, fields, and relationships |
| [API_SPEC.md](./docs/api/API_SPEC.md) | All endpoints, request/response shapes |
| [AGENTS.md](./docs/agents/AGENTS.md) | AI agent design, prompts, I/O contracts |
| [DECISIONS.md](./docs/architecture/DECISIONS.md) | Why we chose each technology |
| [CONVENTIONS.md](./CONVENTIONS.md) | Code style, naming, folder structure rules |
| [ROADMAP.md](./docs/sprints/ROADMAP.md) | Sprint plan, milestones, who owns what |
| [CONTRIBUTING.md](./CONTRIBUTING.md) | Git workflow, PR rules, branch naming |
| [ENV_SETUP.md](./ENV_SETUP.md) | How to run the project locally |

---

## Team

| Name | Role | Area |
|------|------|------|
| Omar | AI Lead | Agent design, prompt engineering, RAG pipeline |
| Khaled | Backend | ASP.NET Core API, business logic, database |
| Badry | Backend | API integration, execution engine integration |
| Salah | Frontend | Angular UI, student and instructor views |
| Owais | Frontend | Component library, dashboard, UX flows |

---

## Tech Stack (Summary)

```
Frontend   → Angular + TypeScript + Tailwind CSS
Backend    → ASP.NET Core Web API (C#) — Modular Monolith
Database   → PostgreSQL + Entity Framework Core
Auth       → JWT (Student / Instructor roles)
AI Layer   → LLM provider (OpenAI or Gemini) + RAG pipeline
Vector DB  → Chroma (recommended) — TBD
Execution  → Docker sandbox (Judge0 as fallback)
```

---

## Getting Started

1. Clone the repo
2. Read [PROJECT_CONTEXT.md](./PROJECT_CONTEXT.md)
3. Follow [ENV_SETUP.md](./ENV_SETUP.md) to run locally
4. Check [ROADMAP.md](./docs/sprints/ROADMAP.md) for what's currently in progress
5. Review [CONTRIBUTING.md](./CONTRIBUTING.md) before making your first PR

---

## Current Sprint

> **Sprint 1 — Core Backend & Auth Integration**
> See [ROADMAP.md](./docs/sprints/ROADMAP.md) for task breakdown.
