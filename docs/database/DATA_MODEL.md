# DATA_MODEL.md
# Codify — Database Schema & Entity Reference

> This is the canonical data model. All EF Core entities must match these definitions. If you need to add a field, update this file and the migration together.

---

## Entity Relationship Overview

```
Users ──────────────────────────────────────────────────┐
  │                                                      │
  ├──< Submissions >──< SubmissionResults               │
  │         │                                           │
  │         └──< FeedbackRecords                        │
  │                                                      │
  └──< HintLogs                                         │
  │                                                      │
  └── PerformanceProfile                                │
                                                        │
Problems ──────────────────────────────────────────────┘
  │
  ├──< ProblemTags >── ConceptTags
  │
  └──< TestCases
```

---

## 1. User

```sql
Table: Users
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK, auto-generated |
| FullName | string(200) | Required |
| Email | string(320) | Required, unique |
| PasswordHash | string | bcrypt hash, never store plaintext |
| Role | enum | `Student` or `Instructor` |
| CreatedAt | DateTime | UTC, set on insert |
| LastLoginAt | DateTime? | UTC, nullable |

**C# Enum:**
```csharp
public enum UserRole
{
    Student,
    Instructor
}
```

**Notes:**
- Email is the login identifier. No username field in MVP.
- Admin role is out of scope for MVP.

---

## 2. Problem

```sql
Table: Problems
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| Title | string(300) | Required |
| Statement | text | Full markdown problem statement |
| Difficulty | enum | `Easy`, `Medium`, `Hard` |
| LanguageSupport | string[] | e.g., `["Python", "CSharp"]` — store as JSON |
| Constraints | text | Problem constraints section |
| CreatedAt | DateTime | UTC |
| IsActive | bool | False = hidden from students |

**C# Enum:**
```csharp
public enum Difficulty
{
    Easy,
    Medium,
    Hard
}
```

**Notes:**
- `Statement` supports markdown. Frontend should render it with a markdown viewer.
- `IsActive = false` is used to hide draft or retired problems.

---

## 3. ConceptTag

```sql
Table: ConceptTags
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| Name | string(100) | e.g., "Dynamic Programming", "Graph Traversal" |
| Description | text | Short explanation used in RAG indexing |

**Seed data (initial tags):**
- Arrays & Hashing
- Two Pointers
- Sliding Window
- Binary Search
- Linked Lists
- Trees
- Graphs
- Dynamic Programming
- Greedy
- Backtracking
- Recursion
- Sorting

---

## 4. ProblemTag (Join Table)

```sql
Table: ProblemTags
```

| Field | Type | Notes |
|-------|------|-------|
| ProblemId | UUID | FK → Problems |
| ConceptTagId | UUID | FK → ConceptTags |

Composite PK: (ProblemId, ConceptTagId)

---

## 5. TestCase

```sql
Table: TestCases
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| ProblemId | UUID | FK → Problems |
| InputData | text | Raw input string passed to the program |
| ExpectedOutput | text | Expected stdout output |
| IsSample | bool | True = shown to student as example |
| VisibilityMode | enum | `Public`, `Hidden` |

**Notes:**
- `IsSample = true` cases are shown on the problem page.
- `VisibilityMode = Hidden` cases are only used for evaluation — student never sees them.

---

## 6. Submission

```sql
Table: Submissions
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| ProblemId | UUID | FK → Problems |
| UserId | UUID | FK → Users |
| Code | text | Raw submitted code |
| Language | enum | `Python`, `CSharp` |
| Status | enum | `Pending`, `Running`, `Accepted`, `WrongAnswer`, `RuntimeError`, `TimeLimitExceeded`, `CompileError` |
| SubmittedAt | DateTime | UTC |
| ExecutionTimeMs | int? | Milliseconds, nullable until evaluated |
| MemoryUsedKb | int? | KB, nullable until evaluated |

**C# Enum:**
```csharp
public enum SubmissionLanguage { Python, CSharp }

public enum SubmissionStatus
{
    Pending,
    Running,
    Accepted,
    WrongAnswer,
    RuntimeError,
    TimeLimitExceeded,
    CompileError
}
```

---

## 7. SubmissionResult

```sql
Table: SubmissionResults
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| SubmissionId | UUID | FK → Submissions, unique (1:1) |
| PassedTestCount | int | |
| FailedTestCount | int | |
| TotalTestCount | int | |
| ErrorMessage | text? | Compiler or runtime error output |
| OutputSummary | text? | First failed test case details |

---

## 8. HintLog

```sql
Table: HintLogs
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| UserId | UUID | FK → Users |
| ProblemId | UUID | FK → Problems |
| HintLevel | int | 1 = gentle nudge, 2 = concept hint, 3 = structural hint |
| RequestText | text? | Optional: what the student typed when asking |
| ResponseText | text | The hint returned by the Tutor Agent |
| CreatedAt | DateTime | UTC |

**Notes:**
- HintLevel increments per user per problem. Max is 3 for MVP.
- At level 3, the agent gives the most detailed hint without writing the code.

---

## 9. PerformanceProfile

```sql
Table: PerformanceProfiles
```

| Field | Type | Notes |
|-------|------|-------|
| UserId | UUID | PK + FK → Users (1:1) |
| WeakTopics | string[] | JSON array of ConceptTag names |
| StrongTopics | string[] | JSON array of ConceptTag names |
| SuccessRate | float | 0.0–1.0, percentage of accepted submissions |
| AverageAttempts | float | Average attempts per solved problem |
| LastUpdatedAt | DateTime | UTC, updated after every submission |

**Notes:**
- Updated by the Analytics Agent after each submission.
- `WeakTopics` = concepts where success rate < 40%.
- `StrongTopics` = concepts where success rate > 75%.

---

## 10. FeedbackRecord

```sql
Table: FeedbackRecords
```

| Field | Type | Notes |
|-------|------|-------|
| Id | UUID | PK |
| SubmissionId | UUID | FK → Submissions |
| FeedbackType | enum | `CodeQuality`, `Optimization`, `IntegrityFlag` |
| Message | text | The AI-generated feedback text |
| CreatedAt | DateTime | UTC |

**C# Enum:**
```csharp
public enum FeedbackType
{
    CodeQuality,
    Optimization,
    IntegrityFlag
}
```

---

## Naming Conventions

- Table names: PascalCase plural (e.g., `SubmissionResults`)
- Column names: PascalCase (e.g., `SubmittedAt`)
- All PKs are UUIDs generated by the application, not the database
- All timestamps are stored as UTC
- Nullable fields are explicitly marked with `?`
