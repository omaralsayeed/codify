# ROADMAP.md
# Codify — Sprint Plan & Milestones

> 12 weeks · 6 sprints · 2 weeks each
> Updated at the start of each sprint.

---

## Sprint Overview

| Sprint | Name | Weeks | Status |
|--------|------|-------|--------|
| Sprint 0 | Setup & Learning | 1–2 | ✅ Complete |
| Sprint 1 | Core Backend & Auth | 3–4 | 🔄 In Progress |
| Sprint 2 | Code Execution & Submission Flow | 5–6 | ⏳ Upcoming |
| Sprint 3 | AI Agents Integration | 7–8 | ⏳ Upcoming |
| Sprint 4 | Analytics & Instructor Dashboard | 9–10 | ⏳ Upcoming |
| Sprint 5 | Polish, Testing & Demo Prep | 11–12 | ⏳ Upcoming |

---

## Sprint 0 — Setup & Learning ✅

**Goal:** Everyone can run the project locally. Foundations agreed on.

- [x] Repo created, branch strategy agreed
- [x] Tech stack finalized (ASP.NET Core, Angular, PostgreSQL)
- [x] Development environment setup documented
- [x] Database schema designed and reviewed
- [x] Architecture documented
- [x] Sprint plan written

---

## Sprint 1 — Core Backend & Auth Integration 🔄

**Goal:** A working API with user auth, problem CRUD, and the data layer running.

**Owner: Khaled + Badry (Backend) · Omar (AI layer setup)**

### Backend Tasks
- [ ] ASP.NET Core project scaffolded (modular monolith structure)
- [ ] EF Core configured with PostgreSQL
- [ ] Database migrations created for: Users, Problems, ConceptTags, ProblemTags, TestCases
- [ ] Auth module: POST /auth/register, POST /auth/login (JWT)
- [ ] Auth middleware: JWT validation, role claim extraction
- [ ] Problem module: GET /problems, GET /problems/{id}, POST /problems (instructor only)
- [ ] ConceptTag seeding (initial 12 tags)
- [ ] Standard API response envelope implemented

### Frontend Tasks (Salah + Owais)
- [ ] Angular project scaffolded with Tailwind CSS
- [ ] HTTP interceptor for JWT injection
- [ ] Auth guard for protected routes
- [ ] Login page + Register page (connected to API)
- [ ] Basic problem list page (fetches from API)

### AI Tasks (Omar)
- [ ] LLM provider selected (OpenAI vs Gemini — decide by Day 3 of this sprint)
- [ ] Vector DB selected (Chroma vs pgvector — decide by Day 5)
- [ ] Spike: send a test prompt to the chosen LLM, validate structured JSON output
- [ ] Chroma/vector DB running locally in Docker

### Sprint 1 Done Criteria
- Student can register, log in, and see a list of problems
- Instructor can log in and create a problem with tags
- LLM and vector DB choices are locked in

---

## Sprint 2 — Code Execution & Submission Flow

**Goal:** Students can submit code and get a real pass/fail result.

**Owner: Badry (execution) · Khaled (submission API) · Salah (code editor UI)**

### Backend Tasks
- [ ] Submission module: POST /submissions, GET /submissions/{id}
- [ ] Execution module: Docker runner scaffolded
- [ ] Python execution working (runs code, captures stdout, compares to expected)
- [ ] C# execution working
- [ ] TestCase module: seed 5 problems with full test case sets
- [ ] Submission status state machine (Pending → Running → Accepted/WrongAnswer/etc.)
- [ ] POST /execution/run (sample test cases, for "Run" button)

### Frontend Tasks
- [ ] Problem detail page with markdown rendering
- [ ] Code editor component (Monaco Editor or CodeMirror)
- [ ] "Run" button → shows sample test result
- [ ] "Submit" button → shows full submission result
- [ ] Submission result panel with pass/fail breakdown

### Sprint 2 Done Criteria
- Student can write Python or C# code in the editor
- Clicking "Run" shows sample test output
- Clicking "Submit" runs all tests and shows Accepted or WrongAnswer

---

## Sprint 3 — AI Agents Integration

**Goal:** All three agents are wired up and returning structured output.

**Owner: Omar (all agents) · Badry (API integration) · Khaled (hint log + feedback storage)**

### AI Tasks
- [ ] RAG pipeline: embed concept descriptions and problem statements into Chroma
- [ ] Tutor Agent: full implementation with RAG retrieval + LLM call + JSON validation
- [ ] POST /ai/hints wired to Tutor Agent
- [ ] Hint level tracking per user per problem
- [ ] Code Checker Agent: implementation + integration after submission evaluation
- [ ] Analytics Agent (tagging mode): auto-tag new problems on creation
- [ ] Analytics Agent (performance mode): update PerformanceProfile after each submission
- [ ] Fallback responses for all three agents

### Backend Tasks
- [ ] HintLog persistence
- [ ] FeedbackRecord persistence
- [ ] PerformanceProfile update logic
- [ ] Rate limiting on /ai/hints (10/hour)

### Frontend Tasks
- [ ] Hint panel in problem detail page
- [ ] "Get Hint" button with loading state
- [ ] Hint display with level indicator
- [ ] AI feedback section in submission result panel

### Sprint 3 Done Criteria
- Student clicks "Get Hint" and gets a real AI-generated, non-solution hint
- After submission, AI code feedback is visible
- Performance profile is updated after each submission

---

## Sprint 4 — Analytics & Instructor Dashboard

**Goal:** Instructors can see real student data. Student progress page is functional.

**Owner: Owais (instructor UI) · Salah (student progress UI) · Khaled (analytics API)**

### Backend Tasks
- [ ] GET /analytics/student/{id} — performance profile endpoint
- [ ] GET /analytics/instructor/overview — cohort summary
- [ ] GET /analytics/topic/{topicId} — per-concept breakdown
- [ ] Integrity flag filtering in instructor view

### Frontend Tasks (Instructor)
- [ ] Instructor dashboard overview (total students, avg success rate, top weak topic)
- [ ] Student list with search and filter
- [ ] Per-student drill-down (weak/strong topics, submission history, hint usage)
- [ ] Topic analytics page
- [ ] Integrity flag indicator

### Frontend Tasks (Student)
- [ ] Student progress page (their own weak/strong topics)
- [ ] Recent activity feed (last 10 submissions)
- [ ] Recommendation section ("Try problems on: Dynamic Programming")

### Sprint 4 Done Criteria
- Instructor can log in and see a dashboard with real student data
- Student can see their own performance profile and weak topics

---

## Sprint 5 — Polish, Testing & Demo Prep

**Goal:** The system is stable, tested, and ready for a live demo.

**Owner: All**

### Testing
- [ ] Unit tests for domain logic (hint level rules, submission status transitions)
- [ ] Integration tests for: Auth flow, Submission flow, Hint flow
- [ ] Edge cases: empty submission, invalid language, AI timeout, Docker timeout
- [ ] Manual QA pass through all student workflows
- [ ] Manual QA pass through all instructor workflows

### Polish
- [ ] Error messages are user-friendly everywhere
- [ ] Loading states on all async operations
- [ ] Empty states (no submissions yet, no hints yet)
- [ ] Mobile-friendly layout check

### Demo Prep
- [ ] 10 seeded problems with test cases and concept tags
- [ ] 3 demo student accounts with submission history
- [ ] 1 demo instructor account with a populated dashboard
- [ ] Demo script written (what to show and in what order)
- [ ] Deployment to staging environment

### Sprint 5 Done Criteria
- Full demo can be run without errors
- All core workflows function end-to-end

---

## Open Decisions (Must Resolve by End of Sprint 1)

| Decision | Owner | Deadline |
|----------|-------|----------|
| LLM provider: OpenAI vs Gemini | Omar | Sprint 1, Day 3 |
| Vector DB: Chroma vs pgvector | Omar | Sprint 1, Day 5 |
| Supported languages confirmed: Python + C# | Team | Sprint 1, Day 1 |
