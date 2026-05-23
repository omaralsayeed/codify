# CONTRIBUTING.md
# Codify — Git Workflow & Contribution Guide

---

## Branch Strategy

We use a simplified **Git Flow**:

```
main          ← production-ready code only (demo branch)
  └── develop ← integration branch, always working
        └── feature/xxx  ← your work branches
        └── fix/xxx
        └── chore/xxx
```

**Rules:**
- Never push directly to `main` or `develop`
- All work happens in a feature/fix/chore branch
- Merge into `develop` via Pull Request
- `main` is updated from `develop` only before a demo

---

## Daily Workflow

```bash
# 1. Start your day — pull latest
git checkout develop
git pull origin develop

# 2. Create your branch
git checkout -b feature/auth-jwt-middleware

# 3. Do your work, commit often
git add .
git commit -m "feat: add JWT middleware with role extraction"

# 4. Before opening a PR, rebase on latest develop
git fetch origin
git rebase origin/develop

# 5. Push and open PR
git push origin feature/auth-jwt-middleware
```

---

## Pull Request Process

1. Open PR from your branch → `develop`
2. Fill in the PR description:
   - **What:** What does this PR do?
   - **Why:** Why is it needed?
   - **How to test:** Steps to verify it works
3. Assign at least one reviewer
4. Wait for approval — do not merge your own PR
5. After approval, the reviewer merges (squash merge preferred)

---

## Commit Message Format

```
<type>: <short description>
```

Types:
- `feat` — new feature
- `fix` — bug fix
- `refactor` — code change without new behavior
- `test` — adding/fixing tests
- `docs` — documentation only
- `chore` — config, dependencies, tooling

Examples:
```
feat: implement Tutor Agent with RAG retrieval
fix: handle Docker timeout on Python submissions
docs: add API spec for submission endpoints
chore: add Chroma client dependency
```

---

## Code Review Checklist

When reviewing a PR, check:

- [ ] Does it follow naming conventions from [CONVENTIONS.md](./CONVENTIONS.md)?
- [ ] Is there any business logic in a Controller that should be in a Service?
- [ ] Are all async methods properly awaited?
- [ ] Is there a fallback for any AI or external service call?
- [ ] Does it break any existing tests?
- [ ] Is anything hardcoded that should be configurable?
- [ ] Would a new team member understand this code without asking?

---

## What Goes in `.gitignore`

Already configured, but never commit:
- `appsettings.Development.json` (if it contains secrets)
- `.env` files
- `node_modules/`
- `bin/`, `obj/`
- `*.user` files
- Any file with API keys or connection strings

---

## Getting Help

- Ask in the team group before spending more than 30 minutes stuck
- If you're going to be blocked for a full day, flag it — don't wait until standup
- If you discover a design issue (something in ARCHITECTURE.md or AGENTS.md is wrong), raise it as a team — don't silently fix it alone
