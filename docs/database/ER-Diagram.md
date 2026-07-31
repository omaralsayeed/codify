# Codify — ER Diagram Reference

```mermaid
erDiagram
  USERS {
    uniqueidentifier Id PK
    string FullName
    string Email
    string PasswordHash
    string Role
    datetime2 CreatedAt
    datetime2 LastLoginAt
    string Username
    string Bio
    string AvatarUrl
    decimal Rating
    int SolvedProblems
    datetime2 UpdatedAt
    boolean IsDeleted
  }

  PROBLEMS {
    uniqueidentifier Id PK
    uniqueidentifier AuthorId FK
    string Title
    string Slug
    string Statement
    string Difficulty
    string LanguageSupportJson
    string Constraints
    int TimeLimitMs
    int MemoryLimitMb
    boolean IsPublic
    boolean IsActive
    int AcceptedSubmissionsCount
    int TotalSubmissionsCount
    datetime2 CreatedAt
    datetime2 UpdatedAt
    boolean IsDeleted
  }

  CONCEPT_TAGS {
    uniqueidentifier Id PK
    string Name
    string Slug
    string Description
    datetime2 CreatedAt
    datetime2 UpdatedAt
    boolean IsDeleted
  }

  PROBLEM_TAGS {
    uniqueidentifier ProblemId PK, FK
    uniqueidentifier ConceptTagId PK, FK
  }

  TEST_CASES {
    uniqueidentifier Id PK
    uniqueidentifier ProblemId FK
    string InputData
    string ExpectedOutput
    boolean IsSample
    string VisibilityMode
    int OrderIndex
    datetime2 CreatedAt
    datetime2 UpdatedAt
    boolean IsDeleted
  }

  SUBMISSIONS {
    uniqueidentifier Id PK
    uniqueidentifier ProblemId FK
    uniqueidentifier UserId FK
    string Code
    string Language
    string Status
    datetime2 SubmittedAt
    int ExecutionTimeMs
    int MemoryUsedKb
    int PassedTestCases
    int TotalTestCases
    decimal Score
    datetime2 UpdatedAt
    boolean IsDeleted
  }

  SUBMISSION_RESULTS {
    uniqueidentifier Id PK
    uniqueidentifier SubmissionId FK
    int PassedTestCount
    int FailedTestCount
    int TotalTestCount
    string ErrorMessage
    string OutputSummary
  }

  HINT_LOGS {
    uniqueidentifier Id PK
    uniqueidentifier UserId FK
    uniqueidentifier ProblemId FK
    int HintLevel
    string RequestText
    string ResponseText
    datetime2 CreatedAt
  }

  PERFORMANCE_PROFILES {
    uniqueidentifier UserId PK, FK
    string WeakTopicsJson
    string StrongTopicsJson
    float SuccessRate
    float AverageAttempts
    datetime2 LastUpdatedAt
  }

  FEEDBACK_RECORDS {
    uniqueidentifier Id PK
    uniqueidentifier SubmissionId FK
    string FeedbackType
    string Message
    datetime2 CreatedAt
  }

  USERS ||--o| PERFORMANCE_PROFILES : has
  USERS ||--o{ SUBMISSIONS : writes
  USERS ||--o{ HINT_LOGS : requests
  USERS ||--o{ PROBLEMS : authors
  PROBLEMS ||--o{ TEST_CASES : contains
  PROBLEMS ||--o{ SUBMISSIONS : receives
  PROBLEMS ||--o{ HINT_LOGS : receives
  PROBLEMS ||--o{ PROBLEM_TAGS : tagged_by
  CONCEPT_TAGS ||--o{ PROBLEM_TAGS : applies_to
  SUBMISSIONS ||--|| SUBMISSION_RESULTS : has
  SUBMISSIONS ||--o{ FEEDBACK_RECORDS : gets
```

## Notes

- `ProblemTags` uses a composite primary key of `(ProblemId, ConceptTagId)`.
- `SubmissionResults.SubmissionId` is unique, so each submission has at most one result row.
- `Problems.AuthorId` is nullable and uses `ON DELETE SET NULL`.
- `HintLogs` is present in the schema even though hint persistence is not yet wired into runtime.
- Soft-delete query filters exist on `Users`, `Problems`, `ConceptTags`, `TestCases`, and `Submissions`.
